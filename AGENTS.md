# AGENTS instructions

C# bindings for the BoringSSL ECDSA signature verification. See [global.json](./global.json) and [src](./src/) directory for the project requirements and configuration.

## Project structure

- [src](./src/): The main codebase. The P/Invoke declarations mirror BoringSSL's C API ([ec_key.h](https://github.com/google/boringssl/blob/main/include/openssl/ec_key.h), [ec.h](https://github.com/google/boringssl/blob/main/include/openssl/ec.h), [bn.h](https://github.com/google/boringssl/blob/main/include/openssl/bn.h), [ecdsa.h](https://github.com/google/boringssl/blob/main/include/openssl/ecdsa.h)).
- [src/boringssl](./src/boringssl/): The pinned BoringSSL submodule. [CMakeLists.txt](./src/CMakeLists.txt) and the `exports.*` files in [src](./src/) replace BoringSSL's own when building, so keep them in sync with the submodule version.
- [build-boringssl.yml](./.github/workflows/build-boringssl.yml): Builds BoringSSL from the pinned submodule and opens a pull request with the resulting binaries.
- [test-publish.yml](./.github/workflows/test-publish.yml): Runs the tests and optionally publishes on NuGet.

## Coding guidelines

- Follow [.editorconfig](./.editorconfig).
- Do not assume; measure, research, ask if unsure.
- Keep comments short and to the point.
- Add tests for new code and bug fixes.
- Use conventional commits; keep scoped and imperative.
- Keep the native binaries under `src/Nethermind.Crypto.SecP256r1/runtimes/` in sync with a single BoringSSL version; they are Git LFS objects updated only by [build-boringssl.yml](./.github/workflows/build-boringssl.yml), so do not edit or rebuild them locally.
- Keep the P/Invoke signatures in sync with the BoringSSL headers of the shipped binaries, and export every function they use in all `exports.*` files.
- Prefer the latest versions of GitHub Actions and runners.
- Update [THIRD-PARTY-NOTICES](./THIRD-PARTY-NOTICES) when introducing a dependency if needed.
- Keep [AGENTS.md](./AGENTS.md) in sync with the ongoing development.
