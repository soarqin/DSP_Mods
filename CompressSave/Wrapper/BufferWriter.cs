using System;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;

namespace CompressSave.Wrapper;

public unsafe class BufferWriter : BinaryWriter
{
    private ByteSpan CurrentBuffer => _doubleBuffer.WriteBuffer;

    private readonly DoubleBuffer _doubleBuffer;

    private readonly Encoding _encoding;

    private readonly int _maxBytesPerChar;

    private byte[] Buffer => CurrentBuffer.Buffer;

    private long SuplusCapacity => _endPos - _curPos;

    private long _swapedBytes;

    public long WriteSum => _swapedBytes + _curPos - _startPos;

    public override Stream BaseStream => _baseStream;

    public override void Write(char[] chars, int index, int count)
    {
        if (chars == null)
        {
            throw new ArgumentNullException(nameof(chars));
        }
        byte[] bytes = _encoding.GetBytes(chars, index, count);
        Write(bytes);
    }

    private byte* _curPos;
    private byte* _endPos;
    private byte* _startPos;
    private readonly Stream _baseStream;

    public BufferWriter(DoubleBuffer doubleBuffer, CompressionStream outStream)
        : this(doubleBuffer, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), outStream)
    {

    }

    private BufferWriter(DoubleBuffer buffer , UTF8Encoding encoding, CompressionStream outStream) : base(Stream.Null, encoding)
    {
        _baseStream = outStream;
        _swapedBytes = 0;
        _doubleBuffer = buffer;
        RefreshStatus();
        _encoding = encoding;
        _maxBytesPerChar = _encoding.GetMaxByteCount(1);
    }

    private void SwapBuffer()
    {
        CurrentBuffer.Position = 0;

        CurrentBuffer.Length = (int)(_curPos - _startPos);
        _swapedBytes += CurrentBuffer.Length;
        _doubleBuffer.SwapBuffer();
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        _startPos = (byte*)Unsafe.AsPointer(ref Buffer[0]);
        _curPos = _startPos;
        _endPos = (byte*)Unsafe.AsPointer(ref Buffer[Buffer.Length - 1]) + 1;
    }

    private void CheckCapacityAndSwap(int requiredCapacity)
    {
        if (SuplusCapacity < requiredCapacity)
        {
            SwapBuffer();
        }
    }

    public override void Write(byte value)
    {
        CheckCapacityAndSwap(1);
        *(_curPos++) = value;
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

    public override void Write(byte[] buffer) => Write(buffer, 0, buffer.Length);


    public override void Write(byte[] buffer, int index, int count)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }
        fixed (byte* start = buffer)
        {
            byte* srcPos = start + index;
            while (SuplusCapacity <= count)
            {
                int dstSuplus = (int)SuplusCapacity;
                //Array.Copy(_buffer, index + writed, Buffer, Position, SuplusCapacity);
                Unsafe.CopyBlock(_curPos, srcPos, (uint)dstSuplus);
                count -= dstSuplus;
                srcPos += dstSuplus;
                _curPos = _endPos;
                SwapBuffer();
            }
            Unsafe.CopyBlock(_curPos, srcPos, (uint)count);
            _curPos += count;
        }
    }

    public override void Write(char ch)
    {
        if (char.IsSurrogate(ch))
        {
            throw new ArgumentException("Arg_SurrogatesNotAllowedAsSingleChar");
        }

        CheckCapacityAndSwap(_maxBytesPerChar);

        _curPos += _encoding.GetBytes(&ch, 1, _curPos, (int)SuplusCapacity);
    }

    //slow
    public override void Write(char[] chars)
    {
        if (chars == null)
        {
            throw new ArgumentNullException(nameof(chars));
        }
        byte[] bytes = _encoding.GetBytes(chars, 0, chars.Length);
        Write(bytes);
    }

    public override void Write(double value)
    {
        CheckCapacityAndSwap(8);
        ulong num = (ulong)(*(long*)(&value));
        *(_curPos++) = (byte)num;
        *(_curPos++) = (byte)(num >> 8);
        *(_curPos++) = (byte)(num >> 16);
        *(_curPos++) = (byte)(num >> 24);
        *(_curPos++) = (byte)(num >> 32);
        *(_curPos++) = (byte)(num >> 40);
        *(_curPos++) = (byte)(num >> 48);
        *(_curPos++) = (byte)(num >> 56);
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
        *(_curPos++) = (byte)value;
        *(_curPos++) = (byte)(value >> 8);
    }

    public override void Write(ushort value)
    {
        CheckCapacityAndSwap(2);
        *(_curPos++) = (byte)value;
        *(_curPos++) = (byte)(value >> 8);
    }


    public override void Write(int value)
    {
        if (SuplusCapacity < 4)
        {
            SwapBuffer();
        }
        *(_curPos++) = (byte)value;
        *(_curPos++) = (byte)(value >> 8);
        *(_curPos++) = (byte)(value >> 16);
        *(_curPos++) = (byte)(value >> 24);
    }

    public override void Write(uint value)
    {
        CheckCapacityAndSwap(4);
        *(_curPos++) = (byte)value;
        *(_curPos++) = (byte)(value >> 8);
        *(_curPos++) = (byte)(value >> 16);
        *(_curPos++) = (byte)(value >> 24);
    }


    public override void Write(long value)
    {
        CheckCapacityAndSwap(8);
        *(_curPos++) = (byte)value;
        *(_curPos++) = (byte)(value >> 8);
        *(_curPos++) = (byte)(value >> 16);
        *(_curPos++) = (byte)(value >> 24);
        *(_curPos++) = (byte)(value >> 32);
        *(_curPos++) = (byte)(value >> 40);
        *(_curPos++) = (byte)(value >> 48);
        *(_curPos++) = (byte)(value >> 56);
    }

    public override void Write(ulong value)
    {
        CheckCapacityAndSwap(8);
        *(_curPos++) = (byte)value;
        *(_curPos++) = (byte)(value >> 8);
        *(_curPos++) = (byte)(value >> 16);
        *(_curPos++) = (byte)(value >> 24);
        *(_curPos++) = (byte)(value >> 32);
        *(_curPos++) = (byte)(value >> 40);
        *(_curPos++) = (byte)(value >> 48);
        *(_curPos++) = (byte)(value >> 56);
    }

    public override void Write(float value)
    {
        CheckCapacityAndSwap(4);
        uint num = *(uint*)(&value);
        *(_curPos++) = (byte)num;
        *(_curPos++) = (byte)(num >> 8);
        *(_curPos++) = (byte)(num >> 16);
        *(_curPos++) = (byte)(num >> 24);
    }


    // Just use same mechanisum from `Write(char[] chars, int index, int count)`
    public override void Write(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }
        byte[] bytes = _encoding.GetBytes(value);
        Write7BitEncodedInt(bytes.Length);
        Write(bytes);
    }


    private new void Write7BitEncodedInt(int value)
    {
        uint num;
        for (num = (uint)value; num >= 128; num >>= 7)
        {
            Write((byte)(num | 0x80));
        }
        Write((byte)num);
    }
}