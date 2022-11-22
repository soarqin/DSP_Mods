using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using CompressSave.Wrapper;

namespace CompressSave;

public enum CompressionType
{
    None = 0,
    LZ4 = 1,
    Zstd = 2,
}

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class CompressSave : BaseUnityPlugin
{
    string StringFromCompresstionType(CompressionType type)
    {
        switch (type)
        {
            case CompressionType.LZ4: return "lz4";
            case CompressionType.Zstd: return "zstd";
            case CompressionType.None: default: return "none";
        }
    }

    CompressionType CompressionTypeFromString(string str)
    {
        switch (str)
        {
            case "lz4": return CompressionType.LZ4;
            case "zstd": return CompressionType.Zstd;
            default: return CompressionType.None;
        }
    }

    public void Awake()
    {
        SaveUtil.logger = Logger;
        if (LZ4API.Avaliable && ZstdAPI.Avaliable)
        {
            PatchSave.CompressionTypeForSaves = CompressionTypeFromString(
                Config.Bind("Compression", "Type", StringFromCompresstionType(PatchSave.CompressionTypeForSaves),
                    new ConfigDescription("Set default compression type.",
                        new AcceptableValueList<string>("lz4", "zstd", "none"), new {}))
                .Value);
            PatchSave.CompressionLevelForSaves = Config.Bind("Compression", "Level", PatchSave.CompressionLevelForSaves,
                    "Set default compression level.\n0 for default level.\n3 ~ 12 for lz4, -5 ~ 22 for zstd.\nSmaller level leads to faster speed and less compression ratio.")
                .Value;
            PatchSave.CreateCompressBuffer();
            if (GameConfig.gameVersion != SaveUtil.VerifiedVersion)
            {
                SaveUtil.logger.LogWarning($"Save version mismatch. Expect:{SaveUtil.VerifiedVersion}, Current:{GameConfig.gameVersion}. MOD may not work as expected.");
            }
            Harmony.CreateAndPatchAll(typeof(PatchSave));
            if (PatchSave.EnableCompress && PatchSave.CompressionTypeForSaves != CompressionType.None)
                Harmony.CreateAndPatchAll(typeof(PatchUISaveGame));
            Harmony.CreateAndPatchAll(typeof(PatchUILoadGame));
        }
        else
            SaveUtil.logger.LogWarning("LZ4.dll is not avaliable.");
    }

    public void OnDestroy()
    {
        PatchUISaveGame.OnDestroy();
        PatchUILoadGame.OnDestroy();
        Harmony.UnpatchAll();
    }
}

class PatchSave
{
    public static WrapperDefines lz4Wrapper = new LZ4API(), zstdWrapper = new ZstdAPI();
    const long MB = 1024 * 1024;
    private static CompressionStream.CompressBuffer compressBuffer;
    public static bool UseCompressSave = false;
    public static CompressionType CompressedType = CompressionType.None;
    public static CompressionType CompressionTypeForSaves = CompressionType.LZ4;
    public static int CompressionLevelForSaves = 0;
    static Stream compressionStream = null;
    public static bool EnableCompress;
    public static bool EnableDecompress;

    public static void CreateCompressBuffer()
    {
        compressBuffer = CompressionStream.CreateBuffer(CompressionTypeForSaves == CompressionType.LZ4 ? lz4Wrapper : zstdWrapper, (int)MB); //Bigger buffer for GS2 compatible
    }

    private static void WriteHeader(FileStream fileStream)
    {
        for (int i = 0; i < 3; i++)
            fileStream.WriteByte(0xCC);
        switch (CompressionTypeForSaves)
        {
            case CompressionType.Zstd:
                fileStream.WriteByte(0xCD);
                break;
            case CompressionType.LZ4:
            default:
                fileStream.WriteByte(0xCC);
                break;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameSave), "AutoSave")]
    [HarmonyPatch(typeof(GameSave), "SaveAsLastExit")]
    static void BeforeAutoSave()
    {
        UseCompressSave = EnableCompress && CompressionTypeForSaves != CompressionType.None;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameSave), "SaveCurrentGame")]
    static IEnumerable<CodeInstruction> SaveCurrentGame_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        /* BinaryWriter binaryWriter = new BinaryWriter(fileStream); => Create compressionStream and replace binaryWriter.
         * set PerformanceMonitor.BeginStream to compressionStream.
         * fileStream.Seek(6L, SeekOrigin.Begin); binaryWriter.Write(position); => Disable seek&write function.
         * binaryWriter.Dispose(); => Dispose compressionStream before fileStream close.
        */
        try
        {
            var matcher = new CodeMatcher(instructions, generator)
                .MatchForward(false, new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(BinaryWriter), new Type[] { typeof(FileStream) })))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "CreateBinaryWriter"))
                .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PerformanceMonitor), "BeginStream")))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "MonitorStream"))
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IO.Stream), "Seek")))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthWrite0"))
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BinaryWriter), "Write", new Type[] { typeof(long) })))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthWrite1"))
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IDisposable), "Dispose")))
                .Advance(1)
                .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "DisposecompressionStream")));
            EnableCompress = true;
            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            SaveUtil.logger.LogError("SaveCurrentGame_Transpiler failed. Mod version not compatible with game version.");
            SaveUtil.logger.LogError(ex);
        }
        return instructions;
    }

    public static void MonitorStream(Stream fileStream)
    {
        PerformanceMonitor.BeginStream(UseCompressSave ? compressionStream : fileStream);
    }

    public static BinaryWriter CreateBinaryWriter(FileStream fileStream)
    {
        if (UseCompressSave)
        {
            SaveUtil.logger.LogDebug("Begin compress save");
            WriteHeader(fileStream);
                compressionStream = new CompressionStream(CompressionTypeForSaves == CompressionType.LZ4 ? lz4Wrapper : zstdWrapper, CompressionLevelForSaves, fileStream, compressBuffer, true); //need to dispose after use
                return ((CompressionStream)compressionStream).BufferWriter;
        }
        SaveUtil.logger.LogDebug("Begin normal save");
        return new BinaryWriter(fileStream);
    }

    public static long FileLengthWrite0(FileStream fileStream, long offset, SeekOrigin origin)
    {
        if (!UseCompressSave)
            return fileStream.Seek(offset, origin);
        return 0L;
    }

    public static void FileLengthWrite1(BinaryWriter binaryWriter, long value)
    {
        if (!UseCompressSave)
            binaryWriter.Write(value);
    }

    public static void DisposecompressionStream()
    {
        if (!UseCompressSave) return;
        var writeflag = compressionStream.CanWrite;
        compressionStream?.Dispose(); //Dispose need to be done before fstream closed.
        compressionStream = null;
        if (writeflag) //Reset UseCompressSave after writing to file
            UseCompressSave = false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameSave), "LoadCurrentGame")]
    [HarmonyPatch(typeof(GameSave), "LoadGameDesc")]
    [HarmonyPatch(typeof(GameSave), "ReadHeader")]
    [HarmonyPatch(typeof(GameSave), "ReadHeaderAndDescAndProperty")]
    [HarmonyPatch(typeof(GameSave), "ReadModes")]
    static IEnumerable<CodeInstruction> LoadCurrentGame_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
    {
        /* using (BinaryReader binaryReader = new BinaryReader(fileStream)) => Create decompressionStream and replace binaryReader.
         * set PerformanceMonitor.BeginStream to decompressionStream.
         * if (fileStream.Length != binaryReader.ReadInt64()) => Replace binaryReader.ReadInt64() to pass file length check.
         * fileStream.Seek((long)num2, SeekOrigin.Current); => Use decompressionStream.Read to seek forward
         * binaryReader.Dispose(); => Dispose decompressionStream before fileStream close.
         */
        try
        {
            var matcher = new CodeMatcher(instructions, iLGenerator)
                .MatchForward(false, new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(BinaryReader), new Type[] { typeof(FileStream) })))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "CreateBinaryReader"))
                .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PerformanceMonitor), "BeginStream")));

            if (matcher.IsValid)
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "MonitorStream"));

            matcher.Start().MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BinaryReader), "ReadInt64")))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthRead"))
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IDisposable), "Dispose")))
                .Advance(1)
                .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "DisposecompressionStream")))
                .MatchBack(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IO.Stream), "Seek")));
            if (matcher.IsValid)
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "ReadSeek"));

            EnableDecompress = true;
            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            SaveUtil.logger.LogError("LoadCurrentGame_Transpiler failed. Mod version not compatible with game version.");
            SaveUtil.logger.LogError(ex);
        }
        return instructions;
    }

    public static BinaryReader CreateBinaryReader(FileStream fileStream)
    {
        switch (CompressedType = SaveUtil.SaveGetCompressType(fileStream))
        {
            case CompressionType.LZ4:
            case CompressionType.Zstd:
                UseCompressSave = true;
                compressionStream = new DecompressionStream(CompressedType == CompressionType.LZ4 ? lz4Wrapper : zstdWrapper, fileStream);
                return new PeekableReader((DecompressionStream)compressionStream);
            case CompressionType.None:
                UseCompressSave = false;
                fileStream.Seek(0, SeekOrigin.Begin);
                return new BinaryReader(fileStream);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static long FileLengthRead(BinaryReader binaryReader)
    {
        switch (CompressedType)
        {
            case CompressionType.LZ4:
            case CompressionType.Zstd:
                binaryReader.ReadInt64();
                return compressionStream.Length;
            case CompressionType.None:
            default:
                return binaryReader.ReadInt64();
        }
    }

    public static long ReadSeek(FileStream fileStream, long offset, SeekOrigin origin)
    {
        switch (CompressedType)
        {
            case CompressionType.LZ4:
            case CompressionType.Zstd:
                while (offset > 0)
                    offset -= compressionStream.Read(compressBuffer.outBuffer, 0, (int)offset);
                return compressionStream.Position;
            case CompressionType.None:
            default:
                return fileStream.Seek(offset, origin);
        }
    }
}