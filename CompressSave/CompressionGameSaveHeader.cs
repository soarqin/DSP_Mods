namespace CompressSave;

public enum CompressionType
{
    None = 0,
    LZ4 = 1,
    Zstd = 2,
}

internal class CompressionGameSaveHeader: GameSaveHeader
{
    public CompressionType CompressionType = CompressionType.None;
}
