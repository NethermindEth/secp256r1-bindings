# secp256r1-bindings

C# bindings for the SecP256r1 (P-256) signature verification using Go [crypto/ecdsa](https://pkg.go.dev/crypto/ecdsa) package.

### Build

- Build Go code in c-shared mode: `go build -ldflags="-s -w" -buildmode=c-shared src/go-ecdsa/main.go`
- Put obtained library to the subfolder corresponding to the current OS in the /runtimes folder, replacing existing file.
- Build .NET code: `dotnet build src/Nethermind.Crypto.SecP256r1`
