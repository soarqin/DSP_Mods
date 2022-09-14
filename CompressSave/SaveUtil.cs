using System;
using System.IO;
using BepInEx.Logging;
using CompressSave.LZ4Wrap;

namespace CompressSave;

class SaveUtil
{
    public static ManualLogSource logger;
        

    public static readonly Version VerifiedVersion = new Version
    {
        Major = 0,
        Minor = 9,
        Release = 26,
    };

    public static string UnzipToFile(LZ4DecompressionStream lzStream, string fullPath)
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
                if (!IsCompressedSave(fileStream)) return false;
                using (var lzstream = new LZ4DecompressionStream(fileStream))
                {
                    newSaveName = UnzipToFile(lzstream, path);
                }
            }
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e);
            return false;
        }
    }
    public static bool IsCompressedSave(FileStream fs)
    {
        for (int i = 0; i < 4; i++)
        {
            if (0xCC != fs.ReadByte())
                return false;
        }
        return true;
    }

    internal static bool IsCompressedSave(string saveName)
    {
        if (string.IsNullOrEmpty(saveName)) return false;
        try
        {
            using (FileStream fileStream = new FileStream(GetFullSavePath(saveName), FileMode.Open))
                return IsCompressedSave(fileStream);
        }
        catch (Exception e)
        {
            logger.LogWarning(e);
            return false;
        }
    }

    public static string GetFullSavePath(string saveName) => GameConfig.gameSaveFolder + saveName + GameSave.saveExt;

    public static bool VerifyVersion(int majorVersion, int minorVersion, int releaseVersion)
    {
        return new Version(majorVersion, minorVersion, releaseVersion) == VerifiedVersion;
    }
}