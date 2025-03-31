// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Nethermind.Crypto;

internal static partial class BoringSsl
{
    private const string LibraryName = "secp256r1";
    static BoringSsl() => SetLibraryFallbackResolver();

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

    [LibraryImport(LibraryName, EntryPoint = "EC_KEY_new_by_curve_name")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint EC_KEY_new_by_curve_name(int nid);

    [LibraryImport(LibraryName, EntryPoint = "EC_KEY_set_public_key")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int EC_KEY_set_public_key(nint key, nint point);

    [LibraryImport(LibraryName, EntryPoint = "EC_KEY_free")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void EC_KEY_free(nint key);

    [LibraryImport(LibraryName, EntryPoint = "EC_KEY_get0_group")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint EC_KEY_get0_group(nint key);

    [LibraryImport(LibraryName, EntryPoint = "EC_POINT_new")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint EC_POINT_new(nint group);

    [LibraryImport(LibraryName, EntryPoint = "EC_POINT_set_affine_coordinates_GFp")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int EC_POINT_set_affine_coordinates_GFp(nint group, nint point, nint x, nint y, nint ctx);

    [LibraryImport(LibraryName, EntryPoint = "EC_POINT_free")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void EC_POINT_free(nint point);

    [LibraryImport(LibraryName, EntryPoint = "BN_bin2bn")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe partial nint BN_bin2bn(byte* bin, int len, nint ret);

    [LibraryImport(LibraryName, EntryPoint = "BN_free")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void BN_free(nint bn);

    [LibraryImport(LibraryName, EntryPoint = "ecdsa_verify_fixed")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe partial int ECDSA_verify_fixed(byte* digest, nint digest_len, byte* sig, nint sig_len, nint key);

    public const int NID_X9_62_prime256v1 = 415;
}
