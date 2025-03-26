# secp256r1-bindings

[![Test](https://github.com/nethermindeth/secp256r1-bindings/actions/workflows/test.yml/badge.svg)](https://github.com/nethermindeth/secp256r1-bindings/actions/workflows/test.yml)
[![Nethermind.Crypto.SecP256r1](https://img.shields.io/nuget/v/Nethermind.Crypto.SecP256r1)](https://www.nuget.org/packages/Nethermind.Crypto.SecP256r1)

C# bindings for the SecP256r1 (P-256) signature verification using [BoringSSL](https://github.com/google/boringssl) crypto module.

### Build

- Checkout the repository, including nested BoringSSL submodule.

- Copy `CMakeLists.txt` and all `export.*` files from the `src` folder to `src/boringssl`, replacing existing files.

- Build the BoringSSL library in Release mode per [documentation](https://boringssl.googlesource.com/boringssl/+/HEAD/BUILDING.md):

  ```bash
  cmake -GNinja -B build -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=1
  ninja -C build crypto
  ```

- Put the built `crypto`/`libcrypto` library from `src/boringssl/build` into the respective subdirectory in `src/Nethermind.Crypto.SecP256r1/runtimes`, renaming it and replacing the existing `secp256r1`/`libsecp256r1` stub file

- Build the .NET project as follows:

  ```bash
  dotnet build src/Nethermind.Crypto.SecP256r1
  ```
