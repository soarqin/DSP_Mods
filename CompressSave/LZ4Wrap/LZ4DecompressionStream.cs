using System;
using System.IO;

namespace CompressSave.LZ4Wrap;

class LZ4DecompressionStream : Stream
{
    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => inStream.Length;

    public override long Position 
    {   get => readPos; 
        set
        {
            if (value < readPos)
                ResetStream();
            else
                value -= readPos;
            byte[] tmpBuffer = new byte[1024];
            while (value > 0)
            {
                value -= Read(tmpBuffer, 0, (int)(value < 1024 ? value : 1024));
            }
        } 
    }

    public Stream inStream;

    IntPtr dctx = IntPtr.Zero;

    readonly ByteSpan srcBuffer;
    readonly ByteSpan dcmpBuffer;
    private bool decompressFinish = false;
    readonly long startPos = 0;
    long readPos = 0; //sum of readlen

    public LZ4DecompressionStream(Stream inStream,int extraBufferSize = 512*1024)
    {
        this.inStream = inStream;
        startPos = inStream.Position;
        srcBuffer = new ByteSpan(new byte[extraBufferSize]);
        int len = Fill();
        long expect = LZ4API.DecompressBegin(ref dctx, srcBuffer.Buffer, ref len, out var blockSize);
        srcBuffer.Position += len;
        if (expect < 0) throw new Exception(expect.ToString());
        dcmpBuffer = new ByteSpan(new byte[blockSize]);
    }

    public void ResetStream()
    {
        inStream.Seek(startPos, SeekOrigin.Begin);
        decompressFinish = false;
        srcBuffer.Clear();
        dcmpBuffer.Clear();
        LZ4API.ResetDecompresssCTX(dctx);
        readPos = 0;
    }

    public int Fill()
    {
        int suplus = srcBuffer.Length - srcBuffer.Position;
        if (srcBuffer.Length> 0 && srcBuffer.Position >= suplus)
        {
            Array.Copy(srcBuffer, srcBuffer.Position, srcBuffer, 0, suplus);
            srcBuffer.Length -= srcBuffer.Position;
            srcBuffer.Position = 0;
        }
        if (srcBuffer.IdleCapacity > 0)
        {
            var readlen = inStream.Read(srcBuffer, srcBuffer.Length, srcBuffer.IdleCapacity);
            srcBuffer.Length += readlen;
        }
        return srcBuffer.Length - srcBuffer.Position;
    }

    public override void Flush()
    {
            
    }

    protected override void Dispose(bool disposing)
    {
        LZ4API.DecompressEnd(dctx);
        dctx = IntPtr.Zero;
        base.Dispose(disposing);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int readlen = 0;
        while (count > (readlen += dcmpBuffer.Read(buffer, offset + readlen, count - readlen)) && !decompressFinish)
        {
            var buffSize = Fill();
            if (buffSize <= 0) return readlen;

            var rt = LZ4API.DecompressUpdateEx(dctx, dcmpBuffer, 0, dcmpBuffer.Capacity, srcBuffer, srcBuffer.Position,buffSize, null);
            if (rt.Expect < 0) throw new Exception(rt.Expect.ToString());
            if (rt.Expect == 0) decompressFinish = true;

            srcBuffer.Position += (int)rt.ReadLen;
            dcmpBuffer.Position = 0;
            dcmpBuffer.Length = (int)rt.WriteLen;
        }
        readPos += readlen;
        return readlen;
    }

    public int PeekByte()
    {
        if (dcmpBuffer.Length <= dcmpBuffer.Position)
        {
            var buffSize = Fill();
            if (buffSize <= 0) return -1;

            var rt = LZ4API.DecompressUpdateEx(dctx, dcmpBuffer, 0, dcmpBuffer.Capacity, srcBuffer, srcBuffer.Position, buffSize, null);
            if (rt.Expect < 0) throw new Exception(rt.Expect.ToString());
            if (rt.Expect == 0) decompressFinish = true;

            srcBuffer.Position += (int)rt.ReadLen;
            dcmpBuffer.Position = 0;
            dcmpBuffer.Length = (int)rt.WriteLen;
        }
        return dcmpBuffer.Buffer[dcmpBuffer.Position];
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}