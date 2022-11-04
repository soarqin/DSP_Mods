using System;
using System.Collections.Generic;
using System.IO;
using MonoMod.Utils;

namespace CompressSave.LZ4Wrap;

public struct DecompressStatus
{
    public long WriteLen;
    public long ReadLen;
    public long Expect;
}

public static class LZ4API
{
    public static readonly bool Avaliable;

    static LZ4API()
    {
        Avaliable = true;
        string assemblyPath = System.Reflection.Assembly.GetAssembly(typeof(LZ4API)).Location;
        string root = string.Empty;
        try
        {
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                root = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
            }

            var map = new Dictionary<string, List<DynDllMapping>>
            {
                {
                    "LZ4.dll", new List<DynDllMapping>
                    {
                        "LZ4.dll",
                        "X64/LZ4.dll",
                        "BepInEx/scripts/x64/LZ4.dll",
                        Path.Combine(root, "X64/LZ4.dll"),
                        Path.Combine(root, "LZ4.dll")
                    }
                },
            };
            typeof(LZ4API).ResolveDynDllImports(map);
        }
        catch (Exception e)
        {
            Avaliable = false;
            Console.WriteLine($"Error: {e}");
        }
    }

    public delegate long CalCompressOutBufferSizeFunc(long inBufferSize);

    [DynDllImport(libraryName: "LZ4.dll")] public static CalCompressOutBufferSizeFunc CalCompressOutBufferSize;

    [DynDllImport(libraryName: "LZ4.dll")] public static CompressBeginFunc CompressBegin;

    public delegate long CompressBeginFunc(out IntPtr ctx, byte[] outBuff, long outCapacity, byte[] dictBuffer = null,
        long dictSize = 0);

    [DynDllImport(libraryName: "LZ4.dll")] private static CompressUpdateFunc CompressUpdate = null;

    private unsafe delegate long CompressUpdateFunc(IntPtr ctx, byte* dstBuffer, long dstCapacity, byte* srcBuffer,
        long srcSize);

    public static unsafe long CompressUpdateEx(IntPtr ctx, byte[] dstBuffer, long dstOffset, byte[] srcBuffer,
        long srcOffset, long srcLen)
    {
        fixed (byte* pdst = dstBuffer, psrc = srcBuffer)
        {
            return CompressUpdate(ctx, pdst + dstOffset, dstBuffer.Length - dstOffset, psrc + srcOffset,
                srcLen - srcOffset);
        }
    }

    [DynDllImport(libraryName: "LZ4.dll")] public static FreeCompressContextFunc FreeCompressContext;

    public delegate void FreeCompressContextFunc(IntPtr ctx);

    [DynDllImport(libraryName: "LZ4.dll")] public static CompressEndFunc CompressEnd;

    public delegate long CompressEndFunc(IntPtr ctx, byte[] dstBuffer, long dstCapacity);

    [DynDllImport(libraryName: "LZ4.dll")] public static DecompressEndFunc DecompressEnd;

    public delegate long DecompressEndFunc(IntPtr dctx);

    [DynDllImport(libraryName: "LZ4.dll")] private static DecompressUpdateFunc DecompressUpdate = null;

    private unsafe delegate long DecompressUpdateFunc(IntPtr dctx, byte* dstBuffer, ref long dstCapacity, byte* srcBuffer,
        ref long srcSize, byte* dict, long dictSize);

    public static unsafe DecompressStatus DecompressUpdateEx(IntPtr dctx, byte[] dstBuffer, int dstOffset, int dstCount,
        byte[] srcBuffer, long srcOffset, long count, byte[] dict)
    {
        long dstLen = Math.Min(dstCount, dstBuffer.Length - dstOffset);
        long errCode;
        fixed (byte* pdst = dstBuffer, psrc = srcBuffer, pdict = dict)
        {
            errCode = DecompressUpdate(dctx, pdst + dstOffset, ref dstLen, psrc + srcOffset, ref count, pdict,
                dict?.Length ?? 0);
        }

        return new DecompressStatus
        {
            Expect = errCode,
            ReadLen = count,
            WriteLen = dstLen,
        };
    }

    [DynDllImport(libraryName: "LZ4.dll")] public static DecompressBeginFunc DecompressBegin;

    public delegate long DecompressBeginFunc(ref IntPtr pdctx, byte[] inBuffer, ref int inBufferSize, out int blockSize);

    public delegate void ResetDecompresssCtxFunc(IntPtr dctx);

    [DynDllImport(libraryName: "LZ4.dll")] public static ResetDecompresssCtxFunc ResetDecompresssCTX;
}