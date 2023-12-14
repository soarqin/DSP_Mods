using System;
using System.IO;

namespace CompressSave.Wrapper;

class BlackHoleStream : Stream
{
    private long _length;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _length;

    public override long Position { get; set; }

    public override void Flush()
    {
        
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return count;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        _length = value;
    }

    private readonly byte[] _testBuffer = new byte[1024 * 1024];

    public override void Write(byte[] buffer, int offset, int count)
    {
        Array.Copy(buffer, offset, _testBuffer, 0, Math.Min(count, _testBuffer.Length));
    }
}