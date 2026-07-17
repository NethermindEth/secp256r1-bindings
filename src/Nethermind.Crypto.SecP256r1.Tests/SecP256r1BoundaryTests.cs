// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace Nethermind.Crypto.Tests;

// EIP-7951 boundary vectors: valid signatures whose r or s component equals
// 2^i or N - 2^i for each i in [0, 255], where N is the secp256r1 group order.
public class SecP256r1BoundaryTests
{
    [TestCaseSource(nameof(SBoundaryInputs))]
    [TestCaseSource(nameof(RBoundaryInputs))]
    public void Verifies_boundary_signature(byte[] input)
    {
        Assert.That(SecP256r1.VerifySignature(input), Is.True);
    }

    [TestCaseSource(nameof(OutOfRangeInputs))]
    public void Rejects_out_of_range_component(byte[] input)
    {
        Assert.That(SecP256r1.VerifySignature(input), Is.False);
    }

    // For fixed d, k with Q = d*G and r = (k*G).x, choosing e = s*k - r*d makes (r, s)
    // a valid signature of e for any target s, since the verifier computes
    // (e*s^-1)*G + (r*s^-1)*Q = ((e + r*d)*s^-1)*G = k*G.
    public static IEnumerable<TestCaseData> SBoundaryInputs()
    {
        BigInteger d = DeriveScalar("eip7951/s/d"), k = DeriveScalar("eip7951/s/k");
        P256.Point q = P256.MultiplyG(d);
        BigInteger r = P256.MultiplyG(k).X % P256.N;

        foreach (var (s, name) in BoundaryScalars())
        {
            BigInteger e = P256.Mod(s * k - r * d, P256.N);
            yield return new TestCaseData(EncodeInput(e, r, s, q)).SetName($"s = {name}");
        }
    }

    // For a target r that is the x-coordinate of some point R and fixed s and e, the key
    // Q = r^-1 * (s*R - e*G) makes (r, s) a valid signature of e, since the verifier
    // computes (e*s^-1)*G + (r*s^-1)*Q = R. Targets that are not an x-coordinate
    // on the curve (about half) have no such point and are skipped.
    public static IEnumerable<TestCaseData> RBoundaryInputs()
    {
        BigInteger s = DeriveScalar("eip7951/r/s"), e = DeriveScalar("eip7951/r/e");
        P256.Point eg = P256.MultiplyG(e);

        foreach (var (r, name) in BoundaryScalars())
        {
            if (!P256.TryLiftX(r, out P256.Point rp))
                continue;

            P256.Point q = P256.Multiply(P256.Subtract(P256.Multiply(rp, s), eg), P256.ModInverse(r, P256.N));
            yield return new TestCaseData(EncodeInput(e, r, s, q)).SetName($"r = {name}");
        }
    }

    // r and s outside [1, N - 1] must be rejected even alongside an otherwise valid signature.
    public static IEnumerable<TestCaseData> OutOfRangeInputs()
    {
        BigInteger d = DeriveScalar("eip7951/range/d"), k = DeriveScalar("eip7951/range/k"), e = DeriveScalar("eip7951/range/e");
        P256.Point q = P256.MultiplyG(d);
        BigInteger r = P256.MultiplyG(k).X % P256.N;
        BigInteger s = P256.Mod((e + r * d) * P256.ModInverse(k, P256.N), P256.N);

        yield return new TestCaseData(EncodeInput(e, BigInteger.Zero, s, q)).SetName("r = 0");
        yield return new TestCaseData(EncodeInput(e, P256.N, s, q)).SetName("r = N");
        yield return new TestCaseData(EncodeInput(e, r, BigInteger.Zero, q)).SetName("s = 0");
        yield return new TestCaseData(EncodeInput(e, r, P256.N, q)).SetName("s = N");
    }

    private static IEnumerable<(BigInteger Value, string Name)> BoundaryScalars()
    {
        for (var i = 0; i < 256; i++)
        {
            yield return (BigInteger.One << i, $"2^{i}");
            yield return (P256.N - (BigInteger.One << i), $"N - 2^{i}");
        }
    }

    private static BigInteger DeriveScalar(string label)
    {
        var value = new BigInteger(SHA256.HashData(Encoding.ASCII.GetBytes(label)), isUnsigned: true, isBigEndian: true) % P256.N;
        return value.IsZero ? BigInteger.One : value;
    }

    private static byte[] EncodeInput(BigInteger e, BigInteger r, BigInteger s, P256.Point q) =>
        [.. Encode(e), .. Encode(r), .. Encode(s), .. Encode(q.X), .. Encode(q.Y)];

    private static byte[] Encode(BigInteger value)
    {
        var raw = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        var bytes = new byte[32];
        raw.CopyTo(bytes, 32 - raw.Length);
        return bytes;
    }
}

// Minimal secp256r1 (NIST P-256) affine/Jacobian arithmetic for test vector generation.
internal static class P256
{
    public static readonly BigInteger P = ParseHex("FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFF");
    public static readonly BigInteger N = ParseHex("FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551");
    private static readonly BigInteger B = ParseHex("5AC635D8AA3A93E7B3EBBD55769886BC651D06B0CC53B0F63BCE3C3E27D2604B");
    private static readonly Point G = new(
        ParseHex("6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296"),
        ParseHex("4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5"));

    public readonly record struct Point(BigInteger X, BigInteger Y);

    private readonly record struct JacobianPoint(BigInteger X, BigInteger Y, BigInteger Z)
    {
        public static readonly JacobianPoint Infinity = new(BigInteger.One, BigInteger.One, BigInteger.Zero);
        public bool IsInfinity => Z.IsZero;
    }

    public static BigInteger Mod(BigInteger value, BigInteger modulus) => ((value % modulus) + modulus) % modulus;

    public static BigInteger ModInverse(BigInteger value, BigInteger primeModulus) =>
        BigInteger.ModPow(Mod(value, primeModulus), primeModulus - 2, primeModulus);

    public static Point MultiplyG(BigInteger scalar) => Multiply(G, scalar);

    public static Point Multiply(Point point, BigInteger scalar)
    {
        scalar = Mod(scalar, N);
        JacobianPoint result = JacobianPoint.Infinity, addend = new(point.X, point.Y, BigInteger.One);

        for (var i = (int)scalar.GetBitLength() - 1; i >= 0; i--)
        {
            result = Double(result);
            if (!((scalar >> i) & 1).IsZero)
                result = Add(result, addend);
        }

        return ToAffine(result);
    }

    public static Point Subtract(Point left, Point right) =>
        ToAffine(Add(new(left.X, left.Y, BigInteger.One), new(right.X, Mod(-right.Y, P), BigInteger.One)));

    /// <summary>Finds a curve point with the given x-coordinate, if one exists.</summary>
    public static bool TryLiftX(BigInteger x, out Point point)
    {
        BigInteger y2 = Mod(x * x % P * x - 3 * x + B, P);
        BigInteger y = BigInteger.ModPow(y2, (P + 1) / 4, P);
        point = new(x, y);
        return y * y % P == y2;
    }

    private static JacobianPoint Double(JacobianPoint p)
    {
        if (p.IsInfinity || p.Y.IsZero)
            return JacobianPoint.Infinity;

        BigInteger z2 = p.Z * p.Z % P;
        BigInteger y2 = p.Y * p.Y % P;
        BigInteger m = Mod(3 * (p.X - z2) * (p.X + z2), P); // a = -3
        BigInteger s = 4 * p.X % P * y2 % P;
        BigInteger x = Mod(m * m - 2 * s, P);
        BigInteger y = Mod(m * (s - x) - 8 * y2 % P * y2, P);
        BigInteger z = 2 * p.Y * p.Z % P;
        return new(x, y, z);
    }

    private static JacobianPoint Add(JacobianPoint p, JacobianPoint q)
    {
        if (p.IsInfinity)
            return q;
        if (q.IsInfinity)
            return p;

        BigInteger z1z1 = p.Z * p.Z % P, z2z2 = q.Z * q.Z % P;
        BigInteger u1 = p.X * z2z2 % P, u2 = q.X * z1z1 % P;
        BigInteger s1 = p.Y * z2z2 % P * q.Z % P, s2 = q.Y * z1z1 % P * p.Z % P;

        if (u1 == u2)
            return s1 == s2 ? Double(p) : JacobianPoint.Infinity;

        BigInteger h = Mod(u2 - u1, P), r = Mod(s2 - s1, P);
        BigInteger h2 = h * h % P, h3 = h2 * h % P, u1h2 = u1 * h2 % P;
        BigInteger x = Mod(r * r - h3 - 2 * u1h2, P);
        BigInteger y = Mod(r * (u1h2 - x) - s1 * h3 % P, P);
        BigInteger z = h * p.Z % P * q.Z % P;
        return new(x, y, z);
    }

    private static Point ToAffine(JacobianPoint p)
    {
        if (p.IsInfinity)
            throw new InvalidOperationException("Unexpected point at infinity.");

        BigInteger zInv = ModInverse(p.Z, P), zInv2 = zInv * zInv % P;
        return new(p.X * zInv2 % P, p.Y * zInv2 % P * zInv % P);
    }

    private static BigInteger ParseHex(string hex) => BigInteger.Parse($"0{hex}", NumberStyles.HexNumber);
}
