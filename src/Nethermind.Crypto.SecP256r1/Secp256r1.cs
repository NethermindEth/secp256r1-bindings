// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Nethermind.Crypto;

public static partial class Secp256r1
{
    private const string LibraryName = "secp256r1";

    static Secp256r1() => SetLibraryFallbackResolver();

    [LibraryImport(LibraryName, SetLastError = true)]
    private static unsafe partial byte VerifyBytes(byte* data, int length);

    /// <summary>
    /// Checks that provided input represent correct secp256r1 signature.
    /// </summary>
    /// <param name="input">Input data as a direct concatenation of the following byte arrays: <br/>
    /// <c>hash</c> - Message hash, 32 bytes; <br/>
    /// <c>r</c> - From ECDSA (r,s) signature pair, 32 bytes; <br/>
    /// <c>s</c> - From ECDSA (r,s) signature pair, 32 bytes; <br/>
    /// <c>x</c> - x coordinate of the public key point, 32 bytes; <br/>
    /// <c>y</c> - y coordinate of the public key point, 32 bytes.
    /// </param>
    /// <returns>
    /// <c>true</c> if input is formed correctly and the signature is valid,
    /// <c>false</c> otherwise.
    /// </returns>
    public static unsafe bool VerifySignature(in ReadOnlyMemory<byte> input)
    {
        using MemoryHandle pin = input.Pin();
        return VerifyBytes((byte*) pin.Pointer, input.Length) != 0;
    }

    private static void SetLibraryFallbackResolver()
    {
        Assembly assembly = typeof(Secp256r1).Assembly;

        AssemblyLoadContext.GetLoadContext(assembly)!.ResolvingUnmanagedDll += (context, name) =>
        {
            if (context != assembly || !LibraryName.Equals(name, StringComparison.Ordinal))
                return nint.Zero;

            string platform;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                name = $"lib{name}.so";
                platform = "linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                name = $"lib{name}.dylib";
                platform = "osx";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                name = $"{name}.dll";
                platform = "win";
            }
            else
                throw new PlatformNotSupportedException();

            var arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

            return NativeLibrary.Load($"runtimes/{platform}-{arch}/native/{name}", context, DllImportSearchPath.AssemblyDirectory);
        };
    }
}
