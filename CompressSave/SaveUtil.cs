using System;
using System.IO;
using BepInEx.Logging;
using CompressSave.Wrapper;

namespace CompressSave;

public static class SaveUtil
{
    public static ManualLogSource Logger;

    public static readonly Version VerifiedVersion = new()
    {
        Major = 0,
        Minor = 10,
        Release = 28,
    };

    private static string UnzipToFile(DecompressionStream lzStream, string fullPath)
    {
        lzStream.ResetStream();
        var dir = Path.GetDirectoryName(fullPath);
        var filename = "[Recovery]-" + Path.GetFileNameWithoutExtension(fullPath);
        fullPath = filename + GameSave.saveExt;
        if (dir != null) fullPath = Path.Combine(dir, fullPath);
        var i = 0;
        while(File.Exists(fullPath))
        {
            fullPath = $"{filename}[{i++}]{GameSave.saveExt}"; 
            if (dir != null) fullPath = Path.Combine(dir, fullPath);
        }
        var buffer = new byte[1024 * 1024];
        using (var fs = new FileStream(fullPath, FileMode.Create))
        using (var br = new BinaryWriter(fs))
        {
            for (var read = lzStream.Read(buffer, 0, buffer.Length); read > 0; read = lzStream.Read(buffer, 0, buffer.Length))
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
        var path = GameConfig.gameSaveFolder + saveName + GameSave.saveExt;
        try
        {
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
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
        catch (Exception e)
        {
            Logger.LogError(e);
            return false;
        }
    }
    public static CompressionType SaveGetCompressType(FileStream fs)
    {
        for (var i = 0; i < 3; i++)
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
            using var fileStream = new FileStream(GetFullSavePath(saveName), FileMode.Open);
            return SaveGetCompressType(fileStream);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e);
            return CompressionType.None;
        }
    }

    private static string GetFullSavePath(string saveName) => GameConfig.gameSaveFolder + saveName + GameSave.saveExt;
}