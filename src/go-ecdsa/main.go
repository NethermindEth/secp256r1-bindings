package main

import (
  "C"
  "crypto/ecdsa"
  "crypto/elliptic"
  "math/big"
  "unsafe"
)

//export VerifyBytes
func VerifyBytes(data *C.uchar, length C.int) C.int {
  bytes := C.GoBytes(unsafe.Pointer(data), length) // copy to Go-managed memory immediately

  if len(bytes) != 160 {
      return C.int(0)
  }

  hash := bytes[0:32]
  r := new(big.Int).SetBytes(bytes[32:64])
  s := new(big.Int).SetBytes(bytes[64:96])
  x := new(big.Int).SetBytes(bytes[96:128])
  y := new(big.Int).SetBytes(bytes[128:160])

  pubKey := ecdsa.PublicKey{Curve: elliptic.P256(), X: x, Y: y}

  verified := ecdsa.Verify(&pubKey, hash, r, s)
  if verified {
      return C.int(1)
  }
  return C.int(0)
}

func main() {}
