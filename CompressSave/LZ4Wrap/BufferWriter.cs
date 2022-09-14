using System;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;

namespace CompressSave.LZ4Wrap;

public unsafe class BufferWriter : BinaryWriter
{
    ByteSpan currentBuffer => doubleBuffer.writeBuffer;

    DoubleBuffer doubleBuffer;

    private Encoding _encoding;

    private Encoder encoder;

    byte[] Buffer => currentBuffer.Buffer;

    long SuplusCapacity => endPos - curPos;

    long swapedBytes = 0;

    public long WriteSum => swapedBytes + curPos - startPos;

    public override Stream BaseStream => _baseStream;

    public override void Write(char[] chars, int index, int count)
    {
        if (chars == null)
        {
            throw new ArgumentNullException("chars");
        }
        byte[] bytes = _encoding.GetBytes(chars, index, count);
        Write(bytes);
    }

    byte* curPos;
    byte* endPos;
    byte* startPos;
    private Stream _baseStream;

    public BufferWriter(DoubleBuffer doubleBuffer, LZ4CompressionStream outStream)
        : this(doubleBuffer, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), outStream)
    {

    }

    BufferWriter(DoubleBuffer buffer , UTF8Encoding encoding, LZ4CompressionStream outStream) : base(Stream.Null, encoding)
    {
        _baseStream = outStream;
        swapedBytes = 0;
        doubleBuffer = buffer;
        RefreshStatus();
        _encoding = encoding;
        encoder = _encoding.GetEncoder();
    }

    void SwapBuffer()
    {
        currentBuffer.Position = 0;

        currentBuffer.Length = (int)(curPos - startPos);
        swapedBytes += currentBuffer.Length;
        doubleBuffer.SwapBuffer();
        RefreshStatus();
    }

    void RefreshStatus()
    {
        startPos = (byte*)Unsafe.AsPointer(ref Buffer[0]);
        curPos = startPos;
        endPos = (byte*)Unsafe.AsPointer(ref Buffer[Buffer.Length - 1]) + 1;
    }

    void CheckCapacityAndSwap(int requiredCapacity)
    {
        if (SuplusCapacity < requiredCapacity)
        {
            SwapBuffer();
        }
    }

    public override void Write(byte value)
    {
        CheckCapacityAndSwap(1);
        *(curPos++) = value;
    }

    public override void Write(bool value) => Write((byte)(value ? 1 : 0));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SwapBuffer();
        }
        base.Dispose(disposing);
    }

    public override void Close()
    {
        Dispose(disposing: true);
    }

    public override void Flush()
    {
        SwapBuffer();
    }

    public override long Seek(int offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void Write(sbyte value) => Write((byte)value);

    public override void Write(byte[] _buffer) => Write(_buffer, 0, _buffer.Length);


    public override void Write(byte[] _buffer, int index, int count)
    {
        if (_buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }
        fixed (byte* start = _buffer)
        {
            byte* srcPos = start + index;
            while (SuplusCapacity <= count)
            {
                int dstSuplus = (int)SuplusCapacity;
                //Array.Copy(_buffer, index + writed, Buffer, Position, SuplusCapacity);
                Unsafe.CopyBlock(curPos, srcPos, (uint)dstSuplus);
                count -= dstSuplus;
                srcPos += dstSuplus;
                curPos = endPos;
                SwapBuffer();
            }
            Unsafe.CopyBlock(curPos, srcPos, (uint)count);
            curPos += count;
        }
    }

    public unsafe override void Write(char ch)
    {
        if (char.IsSurrogate(ch))
        {
            throw new ArgumentException("Arg_SurrogatesNotAllowedAsSingleChar");
        }

        CheckCapacityAndSwap(4);

        curPos += encoder.GetBytes(&ch, 1, curPos, (int)SuplusCapacity, flush: true);
    }

    //slow
    public override void Write(char[] chars)
    {
        if (chars == null)
        {
            throw new ArgumentNullException("chars");
        }
        byte[] bytes = _encoding.GetBytes(chars, 0, chars.Length);
        Write(bytes);
    }

    public unsafe override void Write(double value)
    {
        CheckCapacityAndSwap(8);
        ulong num = (ulong)(*(long*)(&value));
        *(curPos++) = (byte)num;
        *(curPos++) = (byte)(num >> 8);
        *(curPos++) = (byte)(num >> 16);
        *(curPos++) = (byte)(num >> 24);
        *(curPos++) = (byte)(num >> 32);
        *(curPos++) = (byte)(num >> 40);
        *(curPos++) = (byte)(num >> 48);
        *(curPos++) = (byte)(num >> 56);
    }

    //slow
    public override void Write(decimal d)
    {
        CheckCapacityAndSwap(16);
        int[] bits = decimal.GetBits(d);

        Write(bits[0]);
        Write(bits[1]);
        Write(bits[2]);
        Write(bits[3]);
    }


    public override void Write(short value)
    {
        CheckCapacityAndSwap(2);
        *(curPos++) = (byte)value;
        *(curPos++) = (byte)(value >> 8);
    }

    public override void Write(ushort value)
    {
        CheckCapacityAndSwap(2);
        *(curPos++) = (byte)value;
        *(curPos++) = (byte)(value >> 8);
    }


    public override void Write(int value)
    {
        if (SuplusCapacity < 4)
        {
            SwapBuffer();
        }
        *(curPos++) = (byte)value;
        *(curPos++) = (byte)(value >> 8);
        *(curPos++) = (byte)(value >> 16);
        *(curPos++) = (byte)(value >> 24);
    }

    public override void Write(uint value)
    {
        CheckCapacityAndSwap(4);
        *(curPos++) = (byte)value;
        *(curPos++) = (byte)(value >> 8);
        *(curPos++) = (byte)(value >> 16);
        *(curPos++) = (byte)(value >> 24);
    }


    public override void Write(long value)
    {
        CheckCapacityAndSwap(8);
        *(curPos++) = (byte)value;
        *(curPos++) = (byte)(value >> 8);
        *(curPos++) = (byte)(value >> 16);
        *(curPos++) = (byte)(value >> 24);
        *(curPos++) = (byte)(value >> 32);
        *(curPos++) = (byte)(value >> 40);
        *(curPos++) = (byte)(value >> 48);
        *(curPos++) = (byte)(value >> 56);
    }

    public override void Write(ulong value)
    {
        CheckCapacityAndSwap(8);
        *(curPos++) = (byte)value;
        *(curPos++) = (byte)(value >> 8);
        *(curPos++) = (byte)(value >> 16);
        *(curPos++) = (byte)(value >> 24);
        *(curPos++) = (byte)(value >> 32);
        *(curPos++) = (byte)(value >> 40);
        *(curPos++) = (byte)(value >> 48);
        *(curPos++) = (byte)(value >> 56);
    }

    public unsafe override void Write(float value)
    {
        if (SuplusCapacity < 4)
        {
            SwapBuffer();
        }
        uint num = *(uint*)(&value);
        *(curPos++) = (byte)num;
        *(curPos++) = (byte)(num >> 8);
        *(curPos++) = (byte)(num >> 16);
        *(curPos++) = (byte)(num >> 24);
    }


    //slow
    public unsafe override void Write(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }
        int byteCount = _encoding.GetByteCount(value);
        Write7BitEncodedInt(byteCount);
        {
            var dstSuplus = (int)SuplusCapacity;
            if (byteCount <= dstSuplus)
            {
                fixed (char* start = value)
                {
                    int Wcount = _encoding.GetBytes(start, value.Length, curPos, dstSuplus);
                    curPos += Wcount;
                    //Console.WriteLine($"Using quick write!");
                    return;
                }
            }
        }

        int charIndex = 0;
        bool completed;
        fixed (char* chars = value)
        {
            do
            {
                encoder.Convert(chars + charIndex, value.Length - charIndex,
                    curPos, (int)SuplusCapacity, true,
                    out int charsConsumed, out int bytesWritten, out completed);
                charIndex += charsConsumed;
                curPos += bytesWritten;
                //Console.WriteLine($"charsConsumed{charsConsumed} charIndex{charIndex} bytesWritten{bytesWritten} position{position} suplusCapacity{suplusCapacity}");

                if (SuplusCapacity <= 0)
                    SwapBuffer();
            } while (!completed);
        }
        encoder.Reset(); //flush
    }



    protected new void Write7BitEncodedInt(int value)
    {
        uint num;
        for (num = (uint)value; num >= 128; num >>= 7)
        {
            Write((byte)(num | 0x80));
        }
        Write((byte)num);
    }
}