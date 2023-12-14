using System;
using System.IO;
using System.Threading;

namespace CompressSave.Wrapper;

public class CompressionStream : Stream
{
    private readonly WrapperDefines _wrapper;

    public const int Mb = 1024 * 1024;
    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _totalWrite;

    // only use for game statistics
    public override long Position
    {
        get => BufferWriter.WriteSum;
        set => throw new NotImplementedException();
    }

    public readonly Stream OutStream;

    private long _totalWrite;
    private readonly bool _useMultiThread;
    private DoubleBuffer _doubleBuffer;

    private byte[] _outBuffer;

    private IntPtr _cctx;
    private long _lastError;
    private bool _stopWorker = true;

    public bool HasError()
    {
        return _lastError != 0;
    }

    private void HandleError(long errorCode)
    {
        if (errorCode < 0)
        {
            _wrapper.CompressContextFree(_cctx);
            _cctx = IntPtr.Zero;
            _lastError = errorCode;
            throw new Exception(errorCode.ToString());
        }
    }

    public struct CompressBuffer
    {
        public byte[] ReadBuffer;
        public byte[] WriteBuffer;
        public byte[] OutBuffer;
    }

    public static CompressBuffer CreateBuffer(int outBufferSize, int exBufferSize = 4 * Mb)
    {
        try
        {
            return new CompressBuffer
            {
                OutBuffer = new byte[outBufferSize],
                ReadBuffer = new byte[exBufferSize],
                WriteBuffer = new byte[exBufferSize],
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
        _wrapper = wrap;
        OutStream = outputStream;
        InitBuffer(compressBuffer.ReadBuffer, compressBuffer.WriteBuffer, compressBuffer.OutBuffer);
        var writeSize = _wrapper.CompressBegin(out _cctx, compressionLevel, _outBuffer, _outBuffer.Length);
        HandleError(writeSize);
        outputStream.Write(_outBuffer, 0, (int)writeSize);
        _useMultiThread = multiThread;
        if (!multiThread) return;
        _stopWorker = false;
        var compressThread = new Thread(CompressAsync);
        compressThread.Start();
    }

    private void InitBuffer(byte[] readBuffer, byte[] writeBuffer, byte[] outputBuffer)
    {
        _doubleBuffer = new DoubleBuffer(readBuffer ?? new byte[4 * Mb], writeBuffer ?? new byte[4 * Mb], Compress);
        _outBuffer = outputBuffer ?? new byte[_wrapper.CompressBufferBound(writeBuffer?.Length ?? 4 * Mb)];
        BufferWriter = new BufferWriter(_doubleBuffer,this);
    }

    public override void Flush()
    {
        _doubleBuffer.SwapBuffer();
        if(_useMultiThread)
        {
            _doubleBuffer.WaitReadEnd();
        }
        lock (_outBuffer)
        {
            OutStream.Flush();
        }
    }

    private void Compress()
    {
        if (!_useMultiThread)
        {
            Compress_Internal();
        }
    }

    private void Compress_Internal()
    {
        var consumeBuffer = _doubleBuffer.ReadBegin();
        if (consumeBuffer.Length > 0)
        {
            lock (_outBuffer)
            {
                long writeSize;
                try
                {
                    writeSize = _wrapper.CompressUpdateEx(_cctx, _outBuffer, 0, consumeBuffer.Buffer, 0, consumeBuffer.Length);
                    HandleError(writeSize);
                }
                finally
                {
                    _doubleBuffer.ReadEnd();
                }
                OutStream.Write(_outBuffer, 0, (int)writeSize);
                _totalWrite += writeSize;
            }
        }
        else
        {
            _doubleBuffer.ReadEnd();
        }
    }

    private void CompressAsync()
    {
        while(!_stopWorker)
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

    private void FreeContext()
    {
        _wrapper.CompressContextFree(_cctx);
        _cctx = IntPtr.Zero;
    }

    private bool _closed;
    public override void Close()
    {
        if (_closed) return;
        BufferWriter.Close();
        _closed = true;
        //Console.WriteLine($"FLUSH");

        Flush();

        // try stop the worker
        _stopWorker = true;
        _doubleBuffer.SwapBuffer();

        var size = _wrapper.CompressEnd(_cctx, _outBuffer, _outBuffer.Length);
        //Debug.Log($"End");
        OutStream.Write(_outBuffer, 0, (int)size);
        base.Close();
    }

    protected override void Dispose(bool disposing)
    {
        FreeContext();
        base.Dispose(disposing);
    }

}