using System;

namespace CompressSave.Wrapper;

public struct DecompressStatus
{
    public long WriteLen;
    public long ReadLen;
    public long Expect;
}

public class WrapperDefines
{
    public delegate long CompressBufferBoundFunc(long inBufferSize);
    public delegate long CompressBeginFunc(out IntPtr ctx, int compressionLevel, byte[] outBuff, long outCapacity, byte[] dictBuffer = null,
        long dictSize = 0);
    public delegate long CompressEndFunc(IntPtr ctx, byte[] dstBuffer, long dstCapacity);
    public delegate void CompressContextFreeFunc(IntPtr ctx);
    public delegate long DecompressBeginFunc(ref IntPtr pdctx, byte[] inBuffer, ref int inBufferSize, out int blockSize, byte[] dict = null, long dictSize = 0);
    public delegate long DecompressEndFunc(IntPtr dctx);
    public delegate void DecompressContextResetFunc(IntPtr dctx);
    protected unsafe delegate long CompressUpdateFunc(IntPtr ctx, byte* dstBuffer, long dstCapacity, byte* srcBuffer,
        long srcSize);
    protected unsafe delegate long DecompressUpdateFunc(IntPtr dctx, byte* dstBuffer, ref long dstCapacity, byte* srcBuffer,
        ref long srcSize);
    
    public CompressBufferBoundFunc CompressBufferBound;
    public CompressBeginFunc CompressBegin;
    public CompressEndFunc CompressEnd;
    public CompressContextFreeFunc CompressContextFree;
    public DecompressBeginFunc DecompressBegin;
    public DecompressEndFunc DecompressEnd;
    public DecompressContextResetFunc DecompressContextReset;
    protected CompressUpdateFunc CompressUpdate;
    protected DecompressUpdateFunc DecompressUpdate;

    public unsafe long CompressUpdateEx(IntPtr ctx, byte[] dstBuffer, long dstOffset, byte[] srcBuffer,
        long srcOffset, long srcLen)
    {
        fixed (byte* pdst = dstBuffer, psrc = srcBuffer)
        {
            return CompressUpdate(ctx, pdst + dstOffset, dstBuffer.Length - dstOffset, psrc + srcOffset,
                srcLen - srcOffset);
        }
    }

    public unsafe DecompressStatus DecompressUpdateEx(IntPtr dctx, byte[] dstBuffer, int dstOffset, int dstCount,
        byte[] srcBuffer, long srcOffset, long count)
    {
        long dstLen = Math.Min(dstCount, dstBuffer.Length - dstOffset);
        long errCode;
        fixed (byte* pdst = dstBuffer, psrc = srcBuffer)
        {
            errCode = DecompressUpdate(dctx, pdst + dstOffset, ref dstLen, psrc + srcOffset, ref count);
        }

        return new DecompressStatus
        {
            Expect = errCode,
            ReadLen = count,
            WriteLen = dstLen,
        };
    }
}
