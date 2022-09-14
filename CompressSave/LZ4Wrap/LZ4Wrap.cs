using System;
using System.Collections.Generic;
using System.IO;
using MonoMod.Utils;

namespace CompressSave.LZ4Wrap;

public struct DecompressStatus
{
    public long writeLen;
    public long readLen;
    public long expect;
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
                root = Path.GetDirectoryName(assemblyPath);
            }
            var map = new Dictionary<string, List<DynDllMapping>>
            {
                { "LZ4.dll" ,new List<DynDllMapping>{
                    "LZ4.dll",
                    "X64/LZ4.dll",
                    "BepInEx/scripts/x64/LZ4.dll",
                    Path.Combine(root,"X64/LZ4.dll"),
                    Path.Combine(root,"LZ4.dll")
                } },
            };
            typeof(LZ4API).ResolveDynDllImports(map);
        }
        catch (Exception e)
        {
            Avaliable = false;
            Console.WriteLine($"Error: {e}");
            return;
        }
    }

    public delegate long _CalCompressOutBufferSize(long inBufferSize);

    [DynDllImport(libraryName: "LZ4.dll")]
    public static _CalCompressOutBufferSize CalCompressOutBufferSize;

    [DynDllImport(libraryName: "LZ4.dll")]
    public static _CompressBegin CompressBegin;
    public delegate long _CompressBegin(out IntPtr ctx, byte[] outBuff, long outCapacity, byte[] dictBuffer = null, long dictSize = 0);

    [DynDllImport(libraryName: "LZ4.dll")]
    public static _CompressUpdate CompressUpdate;
    public unsafe delegate long _CompressUpdate(IntPtr ctx, byte* dstBuffer, long dstCapacity, byte* srcBuffer, long srcSize);

    public unsafe static long CompressUpdateEx(IntPtr ctx, byte[] dstBuffer, long dstOffset, byte[] srcBuffer, long srcOffset, long srcLen)
    {
        fixed (byte* pdst = dstBuffer, psrc = srcBuffer)
        {
            return CompressUpdate(ctx, pdst + dstOffset, dstBuffer.Length - dstOffset, psrc + srcOffset, srcLen - srcOffset);
        }
    }

    [DynDllImport(libraryName: "LZ4.dll")]
    public static _FreeCompressContext FreeCompressContext;
    public delegate void _FreeCompressContext(IntPtr ctx);

    [DynDllImport(libraryName: "LZ4.dll")]
    public static _CompressEnd CompressEnd;
    public delegate long _CompressEnd(IntPtr ctx, byte[] dstBuffer, long dstCapacity);

    [DynDllImport(libraryName: "LZ4.dll")]
    public static _DecompressEnd DecompressEnd;
    public delegate long _DecompressEnd(IntPtr dctx);

    [DynDllImport(libraryName: "LZ4.dll")]
    unsafe static _DecompressUpdate DecompressUpdate = null;
    public unsafe delegate long _DecompressUpdate(IntPtr dctx, byte* dstBuffer, ref long dstCapacity, byte* srcBuffer, ref long srcSize, byte* dict, long dictSize);
    public unsafe static DecompressStatus DecompressUpdateEx(IntPtr dctx, byte[] dstBuffer, int dstOffset, int dstCount, byte[] srcBuffer, long srcOffset, long count, byte[] dict)
    {
        long dstLen = Math.Min(dstCount, dstBuffer.Length - dstOffset);
        long errCode = 0;
        fixed (byte* pdst = dstBuffer, psrc = srcBuffer, pdict = dict)
        {
            errCode = DecompressUpdate(dctx, pdst + dstOffset, ref dstLen, psrc + srcOffset, ref count, pdict, dict == null ? 0 : dict.Length);
        }
        return new DecompressStatus
        {
            expect = errCode,
            readLen = count,
            writeLen = dstLen,
        };
    }

    [DynDllImport(libraryName: "LZ4.dll")]
    public static _DecompressBegin DecompressBegin;
    public delegate long _DecompressBegin(ref IntPtr pdctx, byte[] inBuffer, ref int inBufferSize, out int blockSize);

    public delegate void _ResetDecompresssCTX(IntPtr dctx);

    [DynDllImport(libraryName: "LZ4.dll")]
    public static _ResetDecompresssCTX ResetDecompresssCTX;
}