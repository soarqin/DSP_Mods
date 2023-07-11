using System;
using System.IO;
using System.Threading;

namespace CompressSave.Wrapper;

public class CompressionStream : Stream
{
    public WrapperDefines wrapper;

    public const int MB = 1024 * 1024;
    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => totalWrite;

    // only use for game statistics
    public override long Position { get => BufferWriter.WriteSum; set => new NotImplementedException(); }

    public readonly Stream outStream;

    long totalWrite = 0;
    bool useMultiThread;
    DoubleBuffer doubleBuffer;

    private byte[] outBuffer;

    IntPtr cctx;
    long lastError = 0;
    bool stopWorker = true;
    Thread compressThread;

    public bool HasError()
    {
        return lastError != 0;
    }

    public void HandleError(long errorCode)
    {
        if (errorCode < 0)
        {
            wrapper.CompressContextFree(cctx);
            cctx = IntPtr.Zero;
            lastError = errorCode;
            throw new Exception(errorCode.ToString());
        }
    }

    public struct CompressBuffer
    {
        public byte[] readBuffer;
        public byte[] writeBuffer;
        public byte[] outBuffer;
    }

    public static CompressBuffer CreateBuffer(int outBufferSize, int exBufferSize = 4 * MB)
    {
        try
        {
            return new CompressBuffer
            {
                outBuffer = new byte[outBufferSize],
                readBuffer = new byte[exBufferSize],
                writeBuffer = new byte[exBufferSize],
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        return new CompressBuffer();
    }
       
    public BufferWriter BufferWriter { get; private set; }

    public CompressionStream(WrapperDefines wrap, int compressionLevel, Stream outputStream, CompressBuffer compressBuffer, bool multiThread)
    {
        wrapper = wrap;
        outStream = outputStream;
        InitBuffer(compressBuffer.readBuffer, compressBuffer.writeBuffer, compressBuffer.outBuffer);
        long writeSize = wrapper.CompressBegin(out cctx, compressionLevel, outBuffer, outBuffer.Length);
        HandleError(writeSize);
        outputStream.Write(outBuffer, 0, (int)writeSize);
        useMultiThread = multiThread;
        if(multiThread)
        {
            stopWorker = false;
            compressThread = new Thread(() => CompressAsync());
            compressThread.Start();
        }
    }

    void InitBuffer(byte[] readBuffer, byte[] writeBuffer, byte[] outputBuffer)
    {
        doubleBuffer = new DoubleBuffer(readBuffer ?? new byte[4 * MB], writeBuffer ?? new byte[4 * MB], Compress);
        outBuffer = outputBuffer ?? new byte[wrapper.CompressBufferBound(writeBuffer?.Length ?? 4 * MB)];
        BufferWriter = new BufferWriter(doubleBuffer,this);
    }

    public override void Flush()
    {
        doubleBuffer.SwapBuffer();
        if(useMultiThread)
        {
            doubleBuffer.WaitReadEnd();
        }
        lock (outBuffer)
        {
            outStream.Flush();
        }
    }

    void Compress()
    {
        if (!useMultiThread)
        {
            Compress_Internal();
        }
    }

    void Compress_Internal()
    {
        var consumeBuffer = doubleBuffer.ReadBegin();
        if (consumeBuffer.Length > 0)
        {
            lock (outBuffer)
            {
                long writeSize = 0;
                try
                {
                    writeSize = wrapper.CompressUpdateEx(cctx, outBuffer, 0, consumeBuffer.Buffer, 0, consumeBuffer.Length);
                    HandleError(writeSize);
                }
                finally
                {
                    doubleBuffer.ReadEnd();
                }
                outStream.Write(outBuffer, 0, (int)writeSize);
                totalWrite += writeSize;
            }
        }
        else
        {
            doubleBuffer.ReadEnd();
        }
    }

    void CompressAsync()
    {
        while(!stopWorker)
        {
            Compress_Internal();
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
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
        BufferWriter.Write(buffer, offset, count);
        //var writeBuffer = doubleBuffer.writeBuffer;
        //int writeSize = writeBuffer.Write(buffer, offset, count);
        //while (count - writeSize > 0)
        //{
        //    SwapBuffer(ref writeBuffer);
        //    offset += writeSize;
        //    count -= writeSize;
        //    writeSize = writeBuffer.Write(buffer, offset, count);
        //}
        //inputSum += count;
    }

    protected void FreeContext()
    {
        wrapper.CompressContextFree(cctx);
        cctx = IntPtr.Zero;
    }

    bool closed = false;
    public override void Close()
    {
        if(!closed)
        {
            BufferWriter.Close();
            closed = true;
            //Console.WriteLine($"FLUSH");

            Flush();

            // try stop the worker
            stopWorker = true;
            doubleBuffer.SwapBuffer();

            long size = wrapper.CompressEnd(cctx, outBuffer, outBuffer.Length);
            //Debug.Log($"End");
            outStream.Write(outBuffer, 0, (int)size);
            base.Close();
        }
    }

    protected override void Dispose(bool disposing)
    {
        FreeContext();
        base.Dispose(disposing);
    }

}