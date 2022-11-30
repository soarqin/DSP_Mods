using System;
using System.IO;
using BepInEx.Logging;
using CompressSave.Wrapper;

namespace CompressSave;

public static class SaveUtil
{
    public static ManualLogSource logger;

    public static readonly Version VerifiedVersion = new Version
    {
        Major = 0,
        Minor = 9,
        Release = 27,
    };

    private static string UnzipToFile(DecompressionStream lzStream, string fullPath)
    {
        lzStream.ResetStream();
        string dir = Path.GetDirectoryName(fullPath);
        string filename = "[Recovery]-" + Path.GetFileNameWithoutExtension(fullPath);
        fullPath = Path.Combine(dir, filename + GameSave.saveExt);
        int i = 0;
        while(File.Exists(fullPath))
        {
            fullPath = Path.Combine(dir, $"{filename}[{i++}]{GameSave.saveExt}"); 
        }
        var buffer = new byte[1024 * 1024];
        using (var fs = new FileStream(fullPath, FileMode.Create))
        using (var br = new BinaryWriter(fs))
        {
            for (int read = lzStream.Read(buffer, 0, buffer.Length); read > 0; read = lzStream.Read(buffer, 0, buffer.Length))
            {
                fs.Write(buffer, 0, read);
            }
            fs.Seek(6L, SeekOrigin.Begin);
            br.Write(fs.Length);

        }
        return Path.GetFileNameWithoutExtension(fullPath);
    }

    public static bool DecompressSave(string saveName, out string newSaveName)
    {
        newSaveName = string.Empty;
        string path = GameConfig.gameSaveFolder + saveName + GameSave.saveExt;
        try
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var compressType = SaveGetCompressType(fileStream);
                switch (compressType)
                {
                    case CompressionType.LZ4:
                    case CompressionType.Zstd:
                        using (var lzstream = new DecompressionStream(compressType == CompressionType.LZ4 ? PatchSave.LZ4Wrapper : PatchSave.ZstdWrapper, fileStream))
                        {
                            newSaveName = UnzipToFile(lzstream, path);
                        }
                        return true;
                    case CompressionType.None:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e);
            return false;
        }
    }
    public static CompressionType SaveGetCompressType(FileStream fs)
    {
        for (int i = 0; i < 3; i++)
        {
            if (0xCC != fs.ReadByte())
                return CompressionType.None;
        }

        return fs.ReadByte() switch
        {
            0xCC => CompressionType.LZ4,
            0xCD => CompressionType.Zstd,
            _ => CompressionType.None
        };
    }

    internal static CompressionType SaveGetCompressType(string saveName)
    {
        if (string.IsNullOrEmpty(saveName)) return CompressionType.None;
        try
        {
            using (FileStream fileStream = new FileStream(GetFullSavePath(saveName), FileMode.Open))
                return SaveGetCompressType(fileStream);
        }
        catch (Exception e)
        {
            logger.LogWarning(e);
            return CompressionType.None;
        }
    }

    private static string GetFullSavePath(string saveName) => GameConfig.gameSaveFolder + saveName + GameSave.saveExt;
}