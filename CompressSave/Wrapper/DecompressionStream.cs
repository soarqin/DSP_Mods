using System;
using System.IO;

namespace CompressSave.Wrapper;

public class DecompressionStream : Stream
{
    private readonly WrapperDefines _wrapper;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _inStream.Length;

    public override long Position
    {
        get => _readPos;
        set
        {
            if (value < _readPos)
                ResetStream();
            else
                value -= _readPos;
            var tmpBuffer = new byte[1024];
            while (value > 0)
            {
                value -= Read(tmpBuffer, 0, (int)(value < 1024 ? value : 1024));
            }
        }
    }

    private readonly Stream _inStream;

    private IntPtr _dctx = IntPtr.Zero;

    private readonly ByteSpan _srcBuffer;
    private readonly ByteSpan _dcmpBuffer;
    private bool _decompressFinish;
    private readonly long _startPos;
    private long _readPos; //sum of readlen

    public DecompressionStream(WrapperDefines wrap, Stream inputStream, int extraBufferSize = 512 * 1024)
    {
        _wrapper = wrap;
        _inStream = inputStream;
        _startPos = inputStream.Position;
        _srcBuffer = new ByteSpan(new byte[extraBufferSize]);
        var len = Fill();
        var expect = _wrapper.DecompressBegin(ref _dctx, _srcBuffer.Buffer, ref len, out var blockSize);
        _srcBuffer.Position += len;
        if (expect < 0) throw new Exception(expect.ToString());
        _dcmpBuffer = new ByteSpan(new byte[blockSize]);
    }

    public void ResetStream()
    {
        _inStream.Seek(_startPos, SeekOrigin.Begin);
        _decompressFinish = false;
        _srcBuffer.Clear();
        _dcmpBuffer.Clear();
        _wrapper.DecompressContextReset(_dctx);
        _readPos = 0;
    }

    private int Fill()
    {
        var suplus = _srcBuffer.Length - _srcBuffer.Position;
        if (_srcBuffer.Length > 0 && _srcBuffer.Position >= suplus)
        {
            Array.Copy(_srcBuffer, _srcBuffer.Position, _srcBuffer, 0, suplus);
            _srcBuffer.Length -= _srcBuffer.Position;
            _srcBuffer.Position = 0;
        }

        if (_srcBuffer.IdleCapacity > 0)
        {
            var readlen = _inStream.Read(_srcBuffer, _srcBuffer.Length, _srcBuffer.IdleCapacity);
            _srcBuffer.Length += readlen;
        }

        return _srcBuffer.Length - _srcBuffer.Position;
    }

    public override void Flush()
    {
    }

    protected override void Dispose(bool disposing)
    {
        _wrapper.DecompressEnd(_dctx);
        _dctx = IntPtr.Zero;
        base.Dispose(disposing);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var readlen = 0;
        while (count > (readlen += _dcmpBuffer.Read(buffer, offset + readlen, count - readlen)) && !_decompressFinish)
        {
            var buffSize = Fill();
            if (buffSize <= 0) return readlen;

            var rt = _wrapper.DecompressUpdateEx(_dctx, _dcmpBuffer, 0, _dcmpBuffer.Capacity, _srcBuffer, _srcBuffer.Position,
                buffSize);
            if (rt.Expect < 0) throw new Exception(rt.Expect.ToString());
            if (rt.Expect == 0) _decompressFinish = true;

            _srcBuffer.Position += (int)rt.ReadLen;
            _dcmpBuffer.Position = 0;
            _dcmpBuffer.Length = (int)rt.WriteLen;
        }

        _readPos += readlen;
        return readlen;
    }

    public int PeekByte()
    {
        if (_dcmpBuffer.Length <= _dcmpBuffer.Position)
        {
            var buffSize = Fill();
            if (buffSize <= 0) return -1;

            var rt = _wrapper.DecompressUpdateEx(_dctx, _dcmpBuffer, 0, _dcmpBuffer.Capacity, _srcBuffer, _srcBuffer.Position,
                buffSize);
            if (rt.Expect < 0) throw new Exception(rt.Expect.ToString());
            if (rt.Expect == 0) _decompressFinish = true;

            _srcBuffer.Position += (int)rt.ReadLen;
            _dcmpBuffer.Position = 0;
            _dcmpBuffer.Length = (int)rt.WriteLen;
        }

        return _dcmpBuffer.Buffer[_dcmpBuffer.Position];
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