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
    private Harmony patchSave, patchUISave, patchUILoad;
    string StringFromCompresstionType(CompressionType type)
    {
        switch (type)
        {
            case CompressionType.LZ4: return "lz4";
            case CompressionType.Zstd: return "zstd";
            case CompressionType.None: return "none";
            default: throw new ArgumentException("Unknown compression type.");
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
                        new ConfigDescription("Set default compression type for manual saves.",
                            new AcceptableValueList<string>("lz4", "zstd", "none"), new { }))
                    .Value);
            PatchSave.CompressionLevelForSaves = Config.Bind("Compression", "Level", PatchSave.CompressionLevelForSaves,
                    "Set default compression level.\n0 for default level.\n3 ~ 12 for lz4, -5 ~ 22 for zstd.\nSmaller level leads to faster speed and less compression ratio.")
                .Value;
            PatchSave.CreateCompressBuffer();
            if (GameConfig.gameVersion != SaveUtil.VerifiedVersion)
            {
                SaveUtil.logger.LogWarning(
                    $"Save version mismatch. Expect:{SaveUtil.VerifiedVersion}, Current:{GameConfig.gameVersion}. MOD may not work as expected.");
            }

            patchSave = Harmony.CreateAndPatchAll(typeof(PatchSave));
            if (PatchSave.EnableCompress)
                patchUISave = Harmony.CreateAndPatchAll(typeof(PatchUISaveGame));
            patchUILoad = Harmony.CreateAndPatchAll(typeof(PatchUILoadGame));
        }
        else
            SaveUtil.logger.LogWarning("Either lz4warp.dll or zstdwrap.dll is not avaliable.");
    }

    public void OnDestroy()
    {
        if (patchUISave != null)
        {
            PatchUISaveGame.OnDestroy();
            patchUISave.UnpatchSelf();
        }
        if (patchUILoad != null)
        {
            PatchUILoadGame.OnDestroy();
            patchUILoad.UnpatchSelf();
        }
        patchSave?.UnpatchSelf();
    }
}

class PatchSave
{
    public static readonly WrapperDefines LZ4Wrapper = new LZ4API(), ZstdWrapper = new ZstdAPI(), NoneWrapper = new NoneAPI();
    private const long SizeInMBytes = 1024 * 1024;
    private static CompressionStream.CompressBuffer _compressBuffer;
    public static bool UseCompressSave;
    private static CompressionType _compressionTypeForLoading = CompressionType.None;
    public static CompressionType CompressionTypeForSaves = CompressionType.Zstd;
    public static int CompressionLevelForSaves;
    private static Stream _compressionStream;
    public static bool EnableCompress;

    public static void CreateCompressBuffer()
    {
        switch (CompressionTypeForSaves)
        {
            case CompressionType.LZ4:
                _compressBuffer = CompressionStream.CreateBuffer(LZ4Wrapper, (int)SizeInMBytes);
                break;
            case CompressionType.Zstd:
                _compressBuffer = CompressionStream.CreateBuffer(ZstdWrapper, (int)SizeInMBytes);
                break;
            case CompressionType.None:
                _compressBuffer = CompressionStream.CreateBuffer(NoneWrapper, (int)SizeInMBytes);
                break;
            default:
                throw new ArgumentException("Unknown compression type.");
        }
    }

    private static void WriteHeader(FileStream fileStream)
    {
        switch (CompressionTypeForSaves)
        {
            case CompressionType.Zstd:
                for (int i = 0; i < 3; i++)
                    fileStream.WriteByte(0xCC);
                fileStream.WriteByte(0xCD);
                break;
            case CompressionType.LZ4:
                for (int i = 0; i < 4; i++)
                    fileStream.WriteByte(0xCC);
                break;
            case CompressionType.None:
                break;
            default:
                throw new ArgumentException("Unknown compression type.");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameSave), "AutoSave")]
    [HarmonyPatch(typeof(GameSave), "SaveAsLastExit")]
    static void BeforeAutoSave()
    {
        UseCompressSave = EnableCompress;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameSave), "SaveCurrentGame")]
    static IEnumerable<CodeInstruction> SaveCurrentGame_Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        /* BinaryWriter binaryWriter = new BinaryWriter(fileStream); => Create compressionStream and replace binaryWriter.
         * set PerformanceMonitor.BeginStream to compressionStream.
         * fileStream.Seek(6L, SeekOrigin.Begin); binaryWriter.Write(position); => Disable seek&write function.
         * binaryWriter.Dispose(); => Dispose compressionStream before fileStream close.
        */
        try
        {
            var matcher = new CodeMatcher(instructions, generator)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Newobj,
                        AccessTools.Constructor(typeof(BinaryWriter), new Type[] { typeof(FileStream) })))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "CreateBinaryWriter"))
                .MatchForward(false,
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PerformanceMonitor), "BeginStream")))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "MonitorStream"))
                .MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IO.Stream), "Seek")))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthWrite0"))
                .MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt,
                        AccessTools.Method(typeof(BinaryWriter), "Write", new Type[] { typeof(long) })))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthWrite1"))
                .MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IDisposable), "Dispose")))
                .Advance(1)
                .Insert(new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(PatchSave), "DisposeCompressionStream")));
            EnableCompress = true;
            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            SaveUtil.logger.LogError(
                "SaveCurrentGame_Transpiler failed. Mod version not compatible with game version.");
            SaveUtil.logger.LogError(ex);
        }

        return instructions;
    }

    public static void MonitorStream(Stream fileStream)
    {
        PerformanceMonitor.BeginStream(UseCompressSave ? _compressionStream : fileStream);
    }

    public static BinaryWriter CreateBinaryWriter(FileStream fileStream)
    {
        if (UseCompressSave)
        {
            SaveUtil.logger.LogDebug("Begin compress save");
            WriteHeader(fileStream);
            switch (CompressionTypeForSaves)
            {
                case CompressionType.LZ4:
                    _compressionStream = new CompressionStream(LZ4Wrapper, CompressionLevelForSaves, fileStream, _compressBuffer, true);
                    break;
                case CompressionType.Zstd:
                    _compressionStream = new CompressionStream(ZstdWrapper, CompressionLevelForSaves, fileStream, _compressBuffer, true);
                    break;
                case CompressionType.None:
                    _compressionStream = new CompressionStream(NoneWrapper, 0, fileStream, _compressBuffer, true);
                    break;
            }

            return ((CompressionStream)_compressionStream).BufferWriter;
        }

        SaveUtil.logger.LogDebug("Begin normal save");
        return new BinaryWriter(fileStream);
    }

    public static long FileLengthWrite0(FileStream fileStream, long offset, SeekOrigin origin)
    {
        if (!UseCompressSave)
        {
            return fileStream.Seek(offset, origin);
        }
        return 0L;
    }

    public static void FileLengthWrite1(BinaryWriter binaryWriter, long value)
    {
        if (!UseCompressSave)
        {
            binaryWriter.Write(value);
        }
    }

    public static void DisposeCompressionStream()
    {
        if (!UseCompressSave) return;
        if (_compressionStream == null)
        {
            UseCompressSave = false;
            return;
        }
        var writeflag = _compressionStream.CanWrite;
        Stream stream = null;
        if (writeflag && CompressionTypeForSaves == CompressionType.None)
        {
            stream = ((CompressionStream)_compressionStream).outStream;
        }
        _compressionStream.Dispose(); //Dispose need to be done before fstream closed.
        _compressionStream = null;
        if (writeflag) //Reset UseCompressSave after writing to file
        {
            if (stream != null)
            {
                // Ugly implementation, but it works. May find a better solution someday.
                var saveLen = stream.Seek(0L, SeekOrigin.End);
                stream.Seek(6L, SeekOrigin.Begin);
                var writer = new BinaryWriter(stream);
                writer.Write(saveLen);
                writer.Dispose();
            }
            UseCompressSave = false;
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameSave), "LoadCurrentGame")]
    [HarmonyPatch(typeof(GameSave), "LoadGameDesc")]
    [HarmonyPatch(typeof(GameSave), "ReadHeader")]
    [HarmonyPatch(typeof(GameSave), "ReadHeaderAndDescAndProperty")]
    [HarmonyPatch(typeof(GameSave), "ReadModes")]
    static IEnumerable<CodeInstruction> LoadCurrentGame_Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator iLGenerator)
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
                .MatchForward(false,
                    new CodeMatch(OpCodes.Newobj,
                        AccessTools.Constructor(typeof(BinaryReader), new Type[] { typeof(FileStream) })))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "CreateBinaryReader"))
                .MatchForward(false,
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PerformanceMonitor), "BeginStream")));

            if (matcher.IsValid)
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "MonitorStream"));

            matcher.Start().MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BinaryReader), "ReadInt64")))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthRead"))
                .MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IDisposable), "Dispose")))
                .Advance(1)
                .Insert(new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(PatchSave), "DisposeCompressionStream")))
                .MatchBack(false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IO.Stream), "Seek")));
            if (matcher.IsValid)
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "ReadSeek"));

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            SaveUtil.logger.LogError(
                "LoadCurrentGame_Transpiler failed. Mod version not compatible with game version.");
            SaveUtil.logger.LogError(ex);
        }

        return instructions;
    }

    public static BinaryReader CreateBinaryReader(FileStream fileStream)
    {
        switch (_compressionTypeForLoading = SaveUtil.SaveGetCompressType(fileStream))
        {
            case CompressionType.LZ4:
                UseCompressSave = true;
                _compressionStream = new DecompressionStream(LZ4Wrapper, fileStream);
                return new PeekableReader((DecompressionStream)_compressionStream);
            case CompressionType.Zstd:
                UseCompressSave = true;
                _compressionStream = new DecompressionStream(ZstdWrapper, fileStream);
                return new PeekableReader((DecompressionStream)_compressionStream);
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
        switch (_compressionTypeForLoading)
        {
            case CompressionType.LZ4:
            case CompressionType.Zstd:
                binaryReader.ReadInt64();
                return _compressionStream.Length;
            case CompressionType.None:
                return binaryReader.ReadInt64();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static long ReadSeek(FileStream fileStream, long offset, SeekOrigin origin)
    {
        switch (_compressionTypeForLoading)
        {
            case CompressionType.LZ4:
            case CompressionType.Zstd:
                while (offset > 0)
                    offset -= _compressionStream.Read(_compressBuffer.outBuffer, 0, (int)offset);
                return _compressionStream.Position;
            case CompressionType.None:
                return fileStream.Seek(offset, origin);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}