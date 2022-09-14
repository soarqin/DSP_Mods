using System.IO;

namespace CompressSave.LZ4Wrap;

class PeekableReader : BinaryReader
{
    LZ4DecompressionStream lzstream;
    public PeekableReader(LZ4DecompressionStream input) : base (input)
    {
        lzstream = input;
    }

    public override int PeekChar()
    {            
        return lzstream.PeekByte();
    }
}