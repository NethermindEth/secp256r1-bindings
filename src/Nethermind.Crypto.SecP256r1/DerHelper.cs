// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Nethermind.Crypto;

internal static class DerHelper
{
    // Max possible size for DER-encoded array of the P256 signature:
    // 2 for SEQUENCE header and length
    // for each of 2 elements (R, S):
    // - 2 for INTEGER header and length
    // - 1 for the leading zero byte if number starts with "negative" byte
    // - 32 for the element itself
    public const int P256SignatureMaxSize = 2 + 2 * (2 + 1 + 32);

    // Encodes signature (r,s) in DER format
    // AsnWriter allocates too much
    public static ReadOnlySpan<byte> EncodeP256Signature(ReadOnlySpan<byte> r, ReadOnlySpan<byte> s, Span<byte> buffer)
    {
        buffer[0] = 0x30; // SEQUENCE OF

        var index = 2;
        index += EncodeUnsignedInteger(r, buffer[index..]);
        index += EncodeUnsignedInteger(s, buffer[index..]);

        buffer[1] = (byte)(index - 2); // SEQUENCE OF length

        return buffer[..index];
    }

    private static int EncodeUnsignedInteger(ReadOnlySpan<byte> value, Span<byte> buffer)
    {
        // Skip zeroes
        var valIndex = BitConverter.IsLittleEndian
            ? BitOperations.TrailingZeroCount(Unsafe.ReadUnaligned<ulong>(in value[0])) / 8
            : BitOperations.LeadingZeroCount(Unsafe.ReadUnaligned<ulong>(in value[0])) / 8;
        value = value[Math.Min(valIndex, value.Length - 1)..];

        buffer[0] = 0x02; // INTEGER;
        buffer[1] = (byte)value.Length; // INTEGER length
        var buffIndex = 2;

        // Add leading zero if number is negative
        if ((value[0] & 0x80) != 0)
        {
            buffer[1]++;
            buffer[buffIndex++] = 0;
        }

        value.CopyTo(buffer[buffIndex..]);
        return buffIndex + value.Length;
    }
}
