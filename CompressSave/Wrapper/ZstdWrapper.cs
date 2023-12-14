using System;
using System.Collections.Generic;
using System.IO;
using MonoMod.Utils;

namespace CompressSave.Wrapper;

public class ZstdAPI: WrapperDefines
{
    public static readonly bool Avaliable;

    static ZstdAPI()
    {
        Avaliable = true;
        string assemblyPath = System.Reflection.Assembly.GetAssembly(typeof(ZstdAPI)).Location;
        string root = string.Empty;
        try
        {
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                root = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
            }

            var map = new Dictionary<string, List<DynDllMapping>>
            {
                {
                    "zstdwrap.dll", [
                        "zstdwrap.dll",
                        "x64/zstdwrap.dll",
                        "plugins/x64/zstdwrap.dll",
                        "BepInEx/scripts/x64/zstdwrap.dll",
                        Path.Combine(root, "zstdwrap.dll"),
                        Path.Combine(root, "x64/zstdwrap.dll"),
                        Path.Combine(root, "plugins/x64/zstdwrap.dll")
                    ]
                },
            };
            typeof(ZstdAPI).ResolveDynDllImports(map);
        }
        catch (Exception e)
        {
            Avaliable = false;
            Console.WriteLine($"Error: {e}");
        }
    }

    public ZstdAPI()
    {
        CompressBufferBound = CompressBufferBound_;
        CompressBegin = CompressBegin_;
        CompressEnd = CompressEnd_;
        CompressUpdate = CompressUpdate_;
        CompressContextFree = CompressContextFree_;
        DecompressBegin = DecompressBegin_;
        DecompressEnd = DecompressEnd_;
        DecompressUpdate = DecompressUpdate_;
        DecompressContextReset = DecompressContextReset_;
    }

    [DynDllImport(libraryName: "zstdwrap.dll", "CompressBufferBound")] protected static CompressBufferBoundFunc CompressBufferBound_;
    [DynDllImport(libraryName: "zstdwrap.dll", "CompressBegin")] protected static CompressBeginFunc CompressBegin_;
    [DynDllImport(libraryName: "zstdwrap.dll", "CompressEnd")] protected static CompressEndFunc CompressEnd_;
    [DynDllImport(libraryName: "zstdwrap.dll", "CompressUpdate")] protected static CompressUpdateFunc CompressUpdate_;
    [DynDllImport(libraryName: "zstdwrap.dll", "CompressContextFree")] protected static CompressContextFreeFunc CompressContextFree_;
    [DynDllImport(libraryName: "zstdwrap.dll", "DecompressBegin")] protected static DecompressBeginFunc DecompressBegin_;
    [DynDllImport(libraryName: "zstdwrap.dll", "DecompressEnd")] protected static DecompressEndFunc DecompressEnd_;
    [DynDllImport(libraryName: "zstdwrap.dll", "DecompressUpdate")] protected static DecompressUpdateFunc DecompressUpdate_;
    [DynDllImport(libraryName: "zstdwrap.dll", "DecompressContextReset")] protected static DecompressContextResetFunc DecompressContextReset_;
}
