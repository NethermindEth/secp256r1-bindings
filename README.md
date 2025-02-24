# secp256r1-bindings

[![Test](https://github.com/nethermindeth/secp256r1-bindings/actions/workflows/test.yml/badge.svg)](https://github.com/nethermindeth/secp256r1-bindings/actions/workflows/test.yml)
[![Nethermind.Crypto.SecP256r1](https://img.shields.io/nuget/v/Nethermind.Crypto.SecP256r1)](https://www.nuget.org/packages/Nethermind.Crypto.SecP256r1)

C# bindings for the SecP256r1 (P-256) signature verification using Go [crypto/ecdsa](https://pkg.go.dev/crypto/ecdsa) package.

### Build

- Build the Go code in `src/go-ecdsa` in `c-shared` mode as follows:

  ```bash
  go build -ldflags="-s -w" -buildmode=c-shared src/go-ecdsa/main.go
  ```
- Put the built library into the respective subdirectory in `src/Nethermind.Crypto.SecP256r1/runtimes`, replacing the existing stub file
- Build the project as follows:

  ```bash
  dotnet build src/Nethermind.Crypto.SecP256r1
  ```
