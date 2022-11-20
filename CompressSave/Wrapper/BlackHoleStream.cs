using System;
using System.IO;

namespace CompressSave.Wrapper;

class BlackHoleStream : Stream
{
    private long length;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => length;

    public override long Position { get; set; }

    public BlackHoleStream()
    {

    }

    public override void Flush()
    {
        ;
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
        length = value;
    }
    public byte[] testBuffer = new byte[1024 * 1024];

    public override void Write(byte[] buffer, int offset, int count)
    {
        Array.Copy(buffer, offset, testBuffer, 0, Math.Min(count, testBuffer.Length));
    }
}