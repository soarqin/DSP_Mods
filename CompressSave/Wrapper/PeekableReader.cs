using System.IO;

namespace CompressSave.Wrapper;

internal class PeekableReader(DecompressionStream input) : BinaryReader(input)
{
    public override int PeekChar()
    {            
        return input.PeekByte();
    }
}