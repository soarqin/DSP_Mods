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
    private Harmony _patchSave, _patchUISave, _patchUILoad;
    public static string StringFromCompresstionType(CompressionType type)
    {
        return type switch
        {
            CompressionType.LZ4 => "lz4",
            CompressionType.Zstd => "zstd",
            CompressionType.None => "none",
            _ => throw new ArgumentException("Unknown compression type.")
        };
    }

    private static CompressionType CompressionTypeFromString(string str)
    {
        return str switch
        {
            "lz4" => CompressionType.LZ4,
            "zstd" => CompressionType.Zstd,
            _ => CompressionType.None
        };
    }

    public void Awake()
    {
        SaveUtil.Logger = Logger;
        if (LZ4API.Avaliable && ZstdAPI.Avaliable)
        {
            PatchSave.CompressionTypeForSavesConfig = Config.Bind("Compression", "Type", StringFromCompresstionType(PatchSave.CompressionTypeForSaves),
                        new ConfigDescription("Set default compression type for manual saves.",
                            new AcceptableValueList<string>("lz4", "zstd", "none"), new { }));
            PatchSave.CompressionTypeForSaves = CompressionTypeFromString(PatchSave.CompressionTypeForSavesConfig.Value);
            PatchSave.CompressionTypeForAutoSavesConfig = Config.Bind("Compression", "TypeForAuto", StringFromCompresstionType(PatchSave.CompressionTypeForAutoSaves),
                        new ConfigDescription("Set default compression type for auto saves and last-exit save.",
                            new AcceptableValueList<string>("lz4", "zstd", "none"), new { }));
            PatchSave.CompressionTypeForAutoSaves = CompressionTypeFromString(PatchSave.CompressionTypeForAutoSavesConfig.Value);
            PatchSave.CompressionLevelForSaves = Config.Bind("Compression", "Level", PatchSave.CompressionLevelForSaves,
                    "Set default compression level for manual saves.\n0 for default level.\n3 ~ 12 for lz4, -5 ~ 22 for zstd.\nSmaller level leads to faster speed and less compression ratio.")
                .Value;
            PatchSave.CompressionLevelForAutoSaves = Config.Bind("Compression", "LevelForAuto", PatchSave.CompressionLevelForAutoSaves,
                    "Set default compression level for auto saves and last-exit save.\n0 for default level.\n3 ~ 12 for lz4, -5 ~ 22 for zstd.\nSmaller level leads to faster speed and less compression ratio.")
                .Value;
            PatchSave.EnableForAutoSaves = Config.Bind("Compression", "EnableForAutoSaves", true,
                    "Enable the feature for auto saves and last-exit save.");
            PatchSave.CreateCompressBuffer();
            if (GameConfig.gameVersion != SaveUtil.VerifiedVersion)
            {
                SaveUtil.Logger.LogWarning(
                    $"Save version mismatch. Expect:{SaveUtil.VerifiedVersion}, Current:{GameConfig.gameVersion}. MOD may not work as expected.");
            }

            _patchSave = Harmony.CreateAndPatchAll(typeof(PatchSave));
            if (PatchSave.EnableCompress)
                _patchUISave = Harmony.CreateAndPatchAll(typeof(PatchUISaveGame));
            _patchUILoad = Harmony.CreateAndPatchAll(typeof(PatchUILoadGame));
        }
        else
            SaveUtil.Logger.LogWarning("Either nonewrap.dll, lz4warp.dll or zstdwrap.dll is not avaliable.");
        I18N.Init();
        I18N.Add("Store", "Store (No Compression)", "存储(不压缩)");
        I18N.Add("Decompress", "Decompress", "解压存档");
        I18N.Add("Save with Compression", "Save (Compress)", "压缩保存");
        I18N.Add("Compression for auto saves", "Compression for auto saves", "　　自动存档压缩方式");
        I18N.Add("Compression for manual saves", "Compression for manual saves", "　　手动存档压缩方式");
        I18N.Apply();
    }

    public void OnDestroy()
    {
        if (_patchUISave != null)
        {
            PatchUISaveGame.OnDestroy();
            _patchUISave.UnpatchSelf();
            _patchUISave = null;
        }
        if (_patchUILoad != null)
        {
            PatchUILoadGame.OnDestroy();
            _patchUILoad.UnpatchSelf();
            _patchUILoad = null;
        }
        _patchSave?.UnpatchSelf();
        _patchSave = null;
    }
}

public class PatchSave
{
    public static readonly WrapperDefines LZ4Wrapper = new LZ4API(), ZstdWrapper = new ZstdAPI();
    private static readonly WrapperDefines NoneWrapper = new NoneAPI();
    private static CompressionStream.CompressBuffer _compressBuffer;
    public static bool UseCompressSave;
    private static CompressionType _compressionTypeForLoading = CompressionType.None;
    private static CompressionType _compressionTypeForSaving = CompressionType.Zstd;
    private static int _compressionLevelForSaving;
    public static CompressionType CompressionTypeForSaves = CompressionType.Zstd;
    public static CompressionType CompressionTypeForAutoSaves = CompressionType.Zstd;
    public static ConfigEntry<string> CompressionTypeForSavesConfig;
    public static ConfigEntry<string> CompressionTypeForAutoSavesConfig;
    public static int CompressionLevelForSaves;
    public static int CompressionLevelForAutoSaves;
    public static ConfigEntry<bool> EnableForAutoSaves;
    private static Stream _compressionStream;
    public static bool EnableCompress;

    public static void CreateCompressBuffer()
    {
        const int bufSize = CompressionStream.Mb;
        var outBufSize = LZ4Wrapper.CompressBufferBound(bufSize);
        outBufSize = Math.Max(outBufSize, ZstdWrapper.CompressBufferBound(bufSize));
        outBufSize = Math.Max(outBufSize, NoneWrapper.CompressBufferBound(bufSize));
        _compressBuffer = CompressionStream.CreateBuffer((int)outBufSize, bufSize);
        _compressionTypeForSaving = CompressionTypeForSaves;
        _compressionLevelForSaving = CompressionLevelForSaves;
    }

    private static void WriteHeader(FileStream fileStream)
    {
        if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
        switch (_compressionTypeForSaving)
        {
            case CompressionType.Zstd:
                for (var i = 0; i < 3; i++)
                    fileStream.WriteByte(0xCC);
                fileStream.WriteByte(0xCD);
                break;
            case CompressionType.LZ4:
                for (var i = 0; i < 4; i++)
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
    private static void BeforeAutoSave()
    {
        UseCompressSave = EnableForAutoSaves.Value && EnableCompress;
        if (!UseCompressSave) return;
        _compressionTypeForSaving = CompressionTypeForAutoSaves;
        _compressionLevelForSaving = CompressionLevelForAutoSaves;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameSave), "SaveCurrentGame")]
    private static IEnumerable<CodeInstruction> SaveCurrentGame_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        /* BinaryWriter binaryWriter = new BinaryWriter(fileStream); => Create compressionStream and replace binaryWriter.
         * set PerformanceMonitor.BeginStream to compressionStream.
         * fileStream.Seek(6L, SeekOrigin.Begin); binaryWriter.Write(position); => Disable seek&write function.
         * binaryWriter.Dispose(); => Dispose compressionStream before fileStream close.
        */
        var matcher = new CodeMatcher(instructions, generator);
        try
        {
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(BinaryWriter), new [] { typeof(FileStream) }))
            ).Set(
                OpCodes.Call, AccessTools.Method(typeof(PatchSave), "CreateBinaryWriter")
            ).MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PerformanceMonitor), "BeginStream"))
            ).Set(
                OpCodes.Call, AccessTools.Method(typeof(PatchSave), "MonitorStream")
            ).MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Stream), "Seek"))
            ).Set(
                OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthWrite0")
            ).MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BinaryWriter), "Write", new [] { typeof(long) }))
            ).Set(
                OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthWrite1")
            ).MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(IDisposable), "Dispose"))
            ).Advance(1).Insert(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "DisposeCompressionStream"))
            );
            EnableCompress = true;
            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            SaveUtil.Logger.LogError(
                "SaveCurrentGame_Transpiler failed. Mod version not compatible with game version.");
            SaveUtil.Logger.LogError(ex);
        }
        return matcher.InstructionEnumeration();
    }

    public static void MonitorStream(Stream fileStream)
    {
        PerformanceMonitor.BeginStream(UseCompressSave ? _compressionStream : fileStream);
    }

    public static BinaryWriter CreateBinaryWriter(FileStream fileStream)
    {
        if (UseCompressSave)
        {
            SaveUtil.Logger.LogDebug("Begin compress save");
            WriteHeader(fileStream);
            _compressionStream = _compressionTypeForSaving switch
            {
                CompressionType.LZ4 => new CompressionStream(LZ4Wrapper, _compressionLevelForSaving, fileStream, _compressBuffer, true),
                CompressionType.Zstd => new CompressionStream(ZstdWrapper, _compressionLevelForSaving, fileStream, _compressBuffer, true),
                CompressionType.None => new CompressionStream(NoneWrapper, 0, fileStream, _compressBuffer, true),
                _ => _compressionStream
            };

            return ((CompressionStream)_compressionStream).BufferWriter;
        }

        SaveUtil.Logger.LogDebug("Begin normal save");
        return new BinaryWriter(fileStream);
    }

    public static long FileLengthWrite0(FileStream fileStream, long offset, SeekOrigin origin)
    {
        return UseCompressSave ? 0L : fileStream.Seek(offset, origin);
    }

    public static void FileLengthWrite1(BinaryWriter binaryWriter, long value)
    {
        if (UseCompressSave) return;
        binaryWriter.Write(value);
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
        if (writeflag && _compressionTypeForSaving == CompressionType.None)
        {
            stream = ((CompressionStream)_compressionStream).OutStream;
        }
        // Dispose need to be done before fstream closed.
        _compressionStream.Dispose();
        _compressionStream = null;
        if (!writeflag) return;
        // Reset UseCompressSave after writing to file
        if (stream != null)
        {
            // Ugly implementation, but it works. May find a better solution someday.
            var saveLen = stream.Seek(0L, SeekOrigin.End);
            stream.Seek(6L, SeekOrigin.Begin);
            var writer = new BinaryWriter(stream);
            writer.Write(saveLen);
            writer.Dispose();
        }
        _compressionTypeForSaving = CompressionTypeForSaves;
        _compressionLevelForSaving = CompressionLevelForSaves;
        UseCompressSave = false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameSave), "LoadCurrentGame")]
    [HarmonyPatch(typeof(GameSave), "LoadGameDesc")]
    [HarmonyPatch(typeof(GameSave), "ReadHeader")]
    [HarmonyPatch(typeof(GameSave), "ReadHeaderAndDescAndProperty")]
    [HarmonyPatch(typeof(GameSave), "ReadModes")]
    private static IEnumerable<CodeInstruction> LoadCurrentGame_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        /* using (BinaryReader binaryReader = new BinaryReader(fileStream)) => Create decompressionStream and replace binaryReader.
         * set PerformanceMonitor.BeginStream to decompressionStream.
         * if (fileStream.Length != binaryReader.ReadInt64()) => Replace binaryReader.ReadInt64() to pass file length check.
         * fileStream.Seek((long)num2, SeekOrigin.Current); => Use decompressionStream.Read to seek forward
         * binaryReader.Dispose(); => Dispose decompressionStream before fileStream close.
         */
        var matcher = new CodeMatcher(instructions, generator);
        try
        {
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(BinaryReader), new [] { typeof(FileStream) }))
            ).Set(
                OpCodes.Call, AccessTools.Method(typeof(PatchSave), "CreateBinaryReader")
            ).MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PerformanceMonitor), "BeginStream"))
            );

            if (matcher.IsValid)
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "MonitorStream"));

            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BinaryReader), "ReadInt64"))
            ).Set(
                OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthRead")
            ).MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(IDisposable), "Dispose"))
            ).Advance(1).Insert(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "DisposeCompressionStream"))
            ).MatchBack(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Stream), "Seek"))
            );
            if (matcher.IsValid)
                matcher.Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "ReadSeek"));

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            SaveUtil.Logger.LogError(
                "LoadCurrentGame_Transpiler failed. Mod version not compatible with game version.");
            SaveUtil.Logger.LogError(ex);
        }

        return matcher.InstructionEnumeration();
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
                    offset -= _compressionStream.Read(_compressBuffer.OutBuffer, 0, (int)offset);
                return _compressionStream.Position;
            case CompressionType.None:
                return fileStream.Seek(offset, origin);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}