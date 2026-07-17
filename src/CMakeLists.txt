# Copyright 2014 The BoringSSL Authors
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     https://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# This is modified from the original BoringSSL script to:
# - build crypto module only;
# - exclude tests, validation, and most of the configuration options;
# - do not export all the OPENSSL_EXPORT methods, only ones specified in exports.* files.

cmake_minimum_required(VERSION 3.22)

# Defer enabling C and CXX languages.
project(BoringSSL NONE)

# Don't install BoringSSL to system directories by default; it has no stable
# ABI. Instead, default to an "install" directory under the source.
if(CMAKE_INSTALL_PREFIX_INITIALIZED_TO_DEFAULT)
  set(CMAKE_INSTALL_PREFIX ${CMAKE_SOURCE_DIR}/install CACHE PATH "" FORCE)
endif()

include(cmake/go.cmake)
include(gen/sources.cmake)

enable_language(C)
enable_language(CXX)

include(GNUInstallDirs)

string(TOLOWER "${CMAKE_BUILD_TYPE}" CMAKE_BUILD_TYPE_LOWER)
if(CMAKE_BUILD_TYPE_LOWER STREQUAL "relwithassert" OR
   NOT CMAKE_BUILD_TYPE_LOWER MATCHES "rel")
  add_definitions(-DBORINGSSL_DISPATCH_TEST)
  # CMake automatically connects include_directories to the NASM
  # command-line, but not add_definitions.
  set(CMAKE_ASM_NASM_FLAGS "${CMAKE_ASM_NASM_FLAGS} -DBORINGSSL_DISPATCH_TEST")
endif()

# Add a RelWithAsserts build configuration. It is the same as Release, except it
# does not define NDEBUG, so asserts run.
foreach(VAR CMAKE_C_FLAGS CMAKE_CXX_FLAGS CMAKE_ASM_FLAGS)
  string(REGEX REPLACE "(^| )[/-]DNDEBUG( |$)" " " "${VAR}_RELWITHASSERTS"
         "${${VAR}_RELEASE}")
endforeach()

# Add the generated files to NASM's includes. With a final slash for NASM 2.
set(CMAKE_ASM_NASM_FLAGS "${CMAKE_ASM_NASM_FLAGS} -I${CMAKE_CURRENT_SOURCE_DIR}/gen/")

if(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  set(CLANG 1)
endif()

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

# Set some project-wide compiler flags.
if(CMAKE_COMPILER_IS_GNUCXX OR CLANG)
  set(C_CXX_FLAGS "-fno-strict-aliasing")
  if(NOT MSVC)
    if(CMAKE_SYSTEM_NAME STREQUAL "Emscripten")
      # emscripten's emcc/clang does not accept the "-ggdb" flag.
      set(C_CXX_FLAGS "${C_CXX_FLAGS} -g")
    else()
      set(C_CXX_FLAGS "${C_CXX_FLAGS} -ggdb")
    endif()
    set(C_CXX_FLAGS "${C_CXX_FLAGS} -fno-common -fvisibility=hidden")
  endif()
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${C_CXX_FLAGS}")
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${C_CXX_FLAGS}")
elseif(MSVC)
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -utf-8")
  # Without /Zc:__cplusplus, MSVC does not define the right value for
  # __cplusplus. See https://devblogs.microsoft.com/cppblog/msvc-now-correctly-reports-__cplusplus/
  # If this becomes too problematic for downstream code, we can look at
  # _MSVC_LANG.
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -utf-8 -Zc:__cplusplus")
endif()

if(WIN32)
  add_definitions(-D_HAS_EXCEPTIONS=0)
  add_definitions(-DWIN32_LEAN_AND_MEAN)
  add_definitions(-DNOMINMAX)
  # Allow use of fopen.
  add_definitions(-D_CRT_SECURE_NO_WARNINGS)
endif()

# pthread_rwlock_t on Linux requires a feature flag. We limit this to Linux
# because, on Apple platforms, it instead disables APIs we use. See compat(5)
# and sys/cdefs.h. Reportedly, FreeBSD also breaks when this is set. See
# https://crbug.com/boringssl/471.
if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -D_XOPEN_SOURCE=700")
endif()

if(BUILD_SHARED_LIBS)
  add_definitions(-DBORINGSSL_SHARED_LIBRARY)
  # Enable position-independent code globally. This is needed because
  # some library targets are OBJECT libraries.
  set(CMAKE_POSITION_INDEPENDENT_CODE TRUE)
endif()

if(OPENSSL_SMALL)
  add_definitions(-DOPENSSL_SMALL)
endif()

if(OPENSSL_NO_ASM)
  add_definitions(-DOPENSSL_NO_ASM)
endif()

if(NOT OPENSSL_NO_ASM)
  # On x86 and x86_64 Windows, we use the NASM output.
  if(WIN32 AND CMAKE_SYSTEM_PROCESSOR MATCHES "AMD64|x86_64|amd64|x86|i[3-6]86")
    enable_language(ASM_NASM)
    set(OPENSSL_NASM TRUE)
    set(CMAKE_ASM_NASM_FLAGS "${CMAKE_ASM_NASM_FLAGS} -gcv8")
  else()
    enable_language(ASM)
    set(OPENSSL_ASM TRUE)
    if(NOT WIN32)
      set(CMAKE_ASM_FLAGS "${CMAKE_ASM_FLAGS} -Wa,--noexecstack")
    endif()
    # Clang's integrated assembler does not support debug symbols.
    if(NOT CMAKE_ASM_COMPILER_ID MATCHES "Clang")
      set(CMAKE_ASM_FLAGS "${CMAKE_ASM_FLAGS} -Wa,-g")
    endif()
  endif()
endif()

if(WIN32)
  set(CMAKE_SHARED_LINKER_FLAGS "/DEF:../exports.def")
elseif(CMAKE_C_COMPILER_ID STREQUAL "AppleClang")
  set(CMAKE_SHARED_LINKER_FLAGS "-Wl,-exported_symbols_list,../exports.exp")
else()
  set(CMAKE_SHARED_LINKER_FLAGS "-Wl,--version-script=../exports.lds")
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
  set(CMAKE_C_FLAGS_RELEASE "${CMAKE_C_FLAGS_RELEASE} -s")
  set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -s")
endif()

# Enable a suite of warnings. Note this uses the list-based add_compile_options,
# instead of CMAKE_*_FLAGS (string-based).
if(CMAKE_COMPILER_IS_GNUCXX OR CLANG)
  set(C_CXX_WARNINGS -Werror -Wformat=2 -Wmissing-field-initializers -Wshadow
      -Wsign-compare -Wtype-limits -Wvla -Wwrite-strings -Wimplicit-fallthrough)
  set(C_WARNINGS -Wold-style-definition -Wstrict-prototypes)
  set(CXX_WARNING -Wnon-virtual-dtor)

  # clang-cl sets both CLANG and MSVC. It implements clang's diagnostics, but
  # some options behave as in MSVC.
  if(MSVC)
    # clang-cl sets different default warnings than clang. It also treats -Wall
    # as -Weverything, to match MSVC. Instead -W3 is the alias for -Wall.
    # See http://llvm.org/viewvc/llvm-project?view=revision&revision=319116
    list(APPEND C_CXX_WARNINGS -W3 -Wno-unused-parameter)
  else()
    list(APPEND C_CXX_WARNINGS -Wall)
  endif()

  if(CLANG)
    list(APPEND C_CXX_WARNINGS -Wnewline-eof -Wextra-semi -fcolor-diagnostics
         -Wheader-hygiene)
  else()
    list(APPEND C_CXX_WARNINGS -Wformat-signedness)
    # GCC (at least 4.8.4) has a bug where it'll find unreachable free() calls
    # and declare that the code is trying to free a stack pointer.
    list(APPEND C_CXX_WARNINGS -Wno-free-nonheap-object)
  endif()

  # In GCC, -Wmissing-declarations is the C++ spelling of -Wmissing-prototypes
  # and using the wrong one is an error. In Clang, -Wmissing-prototypes is the
  # spelling for both and -Wmissing-declarations is some other warning.
  #
  # https://gcc.gnu.org/onlinedocs/gcc-7.1.0/gcc/Warning-Options.html#Warning-Options
  # https://clang.llvm.org/docs/DiagnosticsReference.html#wmissing-prototypes
  # https://clang.llvm.org/docs/DiagnosticsReference.html#wmissing-declarations
  if(CLANG)
    list(APPEND C_CXX_WARNINGS -Wmissing-prototypes)
  else()
    list(APPEND C_WARNINGS -Wmissing-prototypes)
    list(APPEND CXX_WARNINGS -Wmissing-declarations)
  endif()

  # -Wstring-concatenation was added in Clang 12.0.0, which corresponds to
  # AppleClang 13.0.0 per the table in
  # https://en.wikipedia.org/wiki/Xcode#Toolchain_versions
  if((CMAKE_C_COMPILER_ID STREQUAL "Clang" AND
      CMAKE_C_COMPILER_VERSION VERSION_GREATER_EQUAL "12.0.0") OR
     (CMAKE_C_COMPILER_ID STREQUAL "AppleClang" AND
      CMAKE_C_COMPILER_VERSION VERSION_GREATER_EQUAL "13.0.0"))
    list(APPEND C_CXX_WARNINGS -Wstring-concatenation)
  endif()

  # Clang 12's -Wframe-larger-than reportedly does not work in clang-cl. See
  # https://crbug.com/42290587. Clang 13 includes the following fix, which may
  # be related. Speculatively gate on Clang 13. That corresponds to
  # AppleClang 13.1.6.
  # https://github.com/llvm/llvm-project/commit/6aaf4fa2885600b0e31042071ad06f78218ab0f2
  #
  # Only enable this warning in release builds.
  if(CMAKE_BUILD_TYPE MATCHES "[rR][eE][lL]" AND
     ((CMAKE_C_COMPILER_ID STREQUAL "Clang" AND
       CMAKE_C_COMPILER_VERSION VERSION_GREATER_EQUAL "13.0.0") OR
      (CMAKE_C_COMPILER_ID STREQUAL "AppleClang" AND
       CMAKE_C_COMPILER_VERSION VERSION_GREATER_EQUAL "13.1.6")))
    list(APPEND C_CXX_WARNINGS -Wframe-larger-than=25344)
  endif()

  # -Wctad-maybe-unsupported was added in Clang 10, which is AppleClang 12.0.0.
  if((CMAKE_C_COMPILER_ID STREQUAL "Clang" AND
      CMAKE_C_COMPILER_VERSION VERSION_GREATER_EQUAL "10.0.0") OR
     (CMAKE_C_COMPILER_ID STREQUAL "AppleClang" AND
      CMAKE_C_COMPILER_VERSION VERSION_GREATER_EQUAL "12.0.0"))
    list(APPEND C_CXX_WARNINGS -Wctad-maybe-unsupported)
  endif()

  add_compile_options(
    "$<$<COMPILE_LANGUAGE:C,CXX>:${C_CXX_WARNINGS}>"
    "$<$<COMPILE_LANGUAGE:C>:${C_WARNINGS}>"
    "$<$<COMPILE_LANGUAGE:CXX>:${CXX_WARNINGS}>"
  )
elseif(MSVC)
  set(MSVC_DISABLED_WARNINGS_LIST
      "-wd4100" # 'exarg' : unreferenced formal parameter
      "-wd4127" # conditional expression is constant
      "-wd4244" # 'function' : conversion from 'int' to 'uint8_t',
                # possible loss of data
      "-wd4267" # conversion from 'size_t' to 'int', possible loss of data
      "-wd4702" # unreachable code; MSVC's warning is too aggressive. See
                # https://crbug.com/385161043
      "-wd4706" # assignment within conditional expression
      )
  add_compile_options(
    "$<$<COMPILE_LANGUAGE:C,CXX>:-W4;-WX;${MSVC_DISABLED_WARNINGS_LIST}>")
endif()

if(OPENSSL_ASM)
  set(CRYPTO_SOURCES_ASM_USED ${CRYPTO_SOURCES_ASM})
  set(BCM_SOURCES_ASM_USED ${BCM_SOURCES_ASM})
elseif(OPENSSL_NASM)
  set(CRYPTO_SOURCES_ASM_USED ${CRYPTO_SOURCES_NASM})
  set(BCM_SOURCES_ASM_USED ${BCM_SOURCES_NASM})
endif()

set(NO_CXX_RUNTIME_FLAGS)
if(NOT MSVC)
  # Without -fno-exceptions, use of std::unique_ptr emits a call to
  # std::terminate.
  set(NO_CXX_RUNTIME_FLAGS -fno-exceptions -fno-rtti)
endif()

add_library(fipsmodule OBJECT ${BCM_SOURCES} ${BCM_SOURCES_ASM_USED})
# The C++ code in libcrypto shouldn't depend on libstdc++.
target_compile_options(fipsmodule PRIVATE $<$<COMPILE_LANGUAGE:CXX>:${NO_CXX_RUNTIME_FLAGS}>)
target_include_directories(fipsmodule PRIVATE ${PROJECT_SOURCE_DIR}/include)
target_compile_definitions(fipsmodule PRIVATE -DBORINGSSL_IMPLEMENTATION)
set(CRYPTO_FIPS_OBJECTS $<TARGET_OBJECTS:fipsmodule>)

add_library(crypto ${CRYPTO_SOURCES} ${CRYPTO_FIPS_OBJECTS} ${CRYPTO_SOURCES_ASM_USED})
# The C++ code in libcrypto shouldn't depend on libstdc++.
target_compile_options(crypto PRIVATE $<$<COMPILE_LANGUAGE:CXX>:${NO_CXX_RUNTIME_FLAGS}>)
target_include_directories(crypto PUBLIC
  $<BUILD_INTERFACE:${PROJECT_SOURCE_DIR}/include>
  $<INSTALL_INTERFACE:include>
)
target_compile_definitions(crypto PRIVATE -DBORINGSSL_IMPLEMENTATION)
set_property(TARGET crypto PROPERTY EXPORT_NAME Crypto)

if(WIN32)
  target_link_libraries(crypto ws2_32)
endif()

# CMAKE_SYSTEM_NAME is "Generic" for embedded OSes:
# https://cmake.org/cmake/help/book/mastering-cmake/chapter/Cross%20Compiling%20With%20CMake.html#toolchain-files
#
# For now we assume embedded OSes do not have threads. Additionally, the Threads
# package does not work with Android, but Android does not require any extra
# parameters to link pthreads.
if(NOT CMAKE_SYSTEM_NAME MATCHES "^(Generic|Android)$")
  find_package(Threads REQUIRED)
  target_link_libraries(crypto Threads::Threads)
endif()
