using System;
using System.Threading;

namespace CompressSave.Wrapper;

public class ByteSpan
{
    public byte[] Buffer { get; private set; }
    //public int Start;
    public int Length;
    public int Capacity;
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

public struct ReadOnlySpan
{
    public readonly int Length;
    public readonly byte[] Buffer;
    public int Position;

    public ReadOnlySpan(byte[] buffer, int length)
    {
        Buffer = buffer;
        Length = length;
        Position = 0;
    }

    public int Read(byte[] dst, int offset, int count)
    {
        count = Math.Min(Length - Position, count);
        Array.Copy(Buffer, Position, dst, offset, count);
        Position += count;
        return count;
    }

    public static implicit operator byte[](ReadOnlySpan s) => s.Buffer;
}

public class DoubleBuffer
{
    public const int MB = 1024 * 1024;

    public ByteSpan writeBuffer;
    public ByteSpan readBuffer;
    private ByteSpan midBuffer;
    private Action onReadBufferReady;

    Semaphore readEnd = new Semaphore(1, 1);
    Semaphore writeEnd = new Semaphore(0, 1);

    public DoubleBuffer(byte[] readBuffer, byte[] writeBuffer, Action onReadBufferReady)
    {
        this.onReadBufferReady = onReadBufferReady;
        this.midBuffer = new ByteSpan(readBuffer);
        this.writeBuffer = new ByteSpan(writeBuffer);
    }

    public ByteSpan ReadBegin()
    {
        writeEnd.WaitOne();
        return readBuffer;
    }

    public void ReadEnd()
    {
        readBuffer.Clear();
        midBuffer = readBuffer;
        readBuffer = null;
        readEnd.Release();
    }
    /// <summary>
    /// swap current write buffer to read and wait a new write buffer
    /// </summary>
    /// <returns> write buffer </returns>
    public ByteSpan SwapBuffer(bool triggerEvent = true)
    {
        var write = SwapBegin();
        SwapEnd();
        onReadBufferReady?.Invoke();
        return write;
    }

    public void WaitReadEnd()
    {
        readEnd.WaitOne();
        readEnd.Release();
    }

    public ByteSpan SwapBegin()
    {
        readEnd.WaitOne();
        readBuffer = writeBuffer;
        writeBuffer = midBuffer;
        midBuffer = null;
        return writeBuffer;
    }

    public void SwapEnd()
    {
        writeEnd.Release();
    }
}