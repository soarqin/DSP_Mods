using System.IO;

namespace CompressSave.Wrapper;

class PeekableReader : BinaryReader
{
    DecompressionStream decompressionStream;
    public PeekableReader(DecompressionStream input) : base (input)
    {
        decompressionStream = input;
    }

    public override int PeekChar()
    {            
        return decompressionStream.PeekByte();
    }
}