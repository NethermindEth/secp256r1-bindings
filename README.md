# secp256r1-bindings

[![Test](https://github.com/nethermindeth/secp256r1-bindings/actions/workflows/test-publish.yml/badge.svg)](https://github.com/nethermindeth/secp256r1-bindings/actions/workflows/test-publish.yml)
[![Nethermind.Crypto.SecP256r1](https://img.shields.io/nuget/v/Nethermind.Crypto.SecP256r1)](https://www.nuget.org/packages/Nethermind.Crypto.SecP256r1)

C# bindings for the [BoringSSL](https://github.com/google/boringssl) ECDSA signature verification.

## Build

The prebuilt BoringSSL binaries in `src/Nethermind.Crypto.SecP256r1/runtimes` are stored in Git LFS and updated by running the [Build BoringSSL](./.github/workflows/build-boringssl.yml) workflow, which builds the pinned BoringSSL submodule for all platforms. To build them manually:

- Checkout the repository, including nested BoringSSL submodule.

- Copy `CMakeLists.txt` and all `export.*` files from the `src` folder to `src/boringssl`, replacing existing files.

- Build the BoringSSL library in Release mode per [docs](https://github.com/google/boringssl/blob/main/BUILDING.md):

  ```bash
  cmake -GNinja -B build -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=1
  ninja -C build crypto
  ```

  For `win-arm64`, configure from an ARM64 developer environment with `-DCMAKE_C_COMPILER=clang-cl -DCMAKE_CXX_COMPILER=clang-cl`, as MSVC cannot assemble BoringSSL's AArch64 assembly, which is essential for performance.

- Put the built `crypto`/`libcrypto` library from `src/boringssl/build` into the respective subdirectory in `src/Nethermind.Crypto.SecP256r1/runtimes`, renaming it and replacing the existing `secp256r1`/`libsecp256r1` stub file

- Build the .NET project as follows:

  ```bash
  dotnet build src/Nethermind.Crypto.SecP256r1
  ```

## License

This project is licensed under the [MIT](./LICENSE) license and contains dynamically linked BoringSSL binaries licensed under the [Apache-2.0](https://github.com/google/boringssl/blob/main/LICENSE) license.
