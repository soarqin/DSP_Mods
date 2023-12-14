using System;
using System.Threading;

namespace CompressSave.Wrapper;

public class ByteSpan
{
    public byte[] Buffer { get; }
    //public int Start;
    public int Length;
    public readonly int Capacity;
    public int IdleCapacity => Capacity - Length;
    public int Position;

    public ByteSpan(byte[] buffer)
    {
        Buffer = buffer;
        Capacity = Buffer.Length;
    }
    public void Clear()
    {
        Length = 0;
        Position = 0;
    }
    public int Write(byte[] src, int offset, int count)
    {
        int writeLen = Math.Min(Capacity - Length, count);
        Array.Copy(src, offset, Buffer, Length, writeLen);
        Length += writeLen;
        return writeLen;
    }

    public int Read(byte[] dst, int offset, int count)
    {
        count = Math.Min(Length - Position, count);
        Array.Copy(Buffer, Position, dst, offset, count);
        Position += count;
        return count;
    }

    public static implicit operator byte[](ByteSpan bs) => bs.Buffer;
}

public struct ReadOnlySpan(byte[] buffer, int length)
{
    private readonly byte[] _buffer = buffer;
    private int _position = 0;

    public int Read(byte[] dst, int offset, int count)
    {
        count = Math.Min(length - _position, count);
        Array.Copy(_buffer, _position, dst, offset, count);
        _position += count;
        return count;
    }

    public static implicit operator byte[](ReadOnlySpan s) => s._buffer;
}

public class DoubleBuffer(byte[] readingBuffer, byte[] writingBuffer, Action onReadBufferReadyAction)
{
    public const int Mb = 1024 * 1024;

    public ByteSpan WriteBuffer = new(writingBuffer);
    private ByteSpan _readBuffer;
    private ByteSpan _midBuffer = new(readingBuffer);

    private readonly Semaphore _readEnd = new Semaphore(1, 1);
    private readonly Semaphore _writeEnd = new Semaphore(0, 1);

    public ByteSpan ReadBegin()
    {
        _writeEnd.WaitOne();
        return _readBuffer;
    }

    public void ReadEnd()
    {
        _readBuffer.Clear();
        _midBuffer = _readBuffer;
        _readBuffer = null;
        _readEnd.Release();
    }
    /// <summary>
    /// swap current write buffer to read and wait a new write buffer
    /// </summary>
    /// <returns> write buffer </returns>
    public ByteSpan SwapBuffer(bool triggerEvent = true)
    {
        var write = SwapBegin();
        SwapEnd();
        onReadBufferReadyAction?.Invoke();
        return write;
    }

    public void WaitReadEnd()
    {
        _readEnd.WaitOne();
        _readEnd.Release();
    }

    private ByteSpan SwapBegin()
    {
        _readEnd.WaitOne();
        _readBuffer = WriteBuffer;
        WriteBuffer = _midBuffer;
        _midBuffer = null;
        return WriteBuffer;
    }

    private void SwapEnd()
    {
        _writeEnd.Release();
    }
}