using System;
using System.Collections.Generic;
using System.IO;
using MonoMod.Utils;

namespace CompressSave.Wrapper;

public class LZ4API: WrapperDefines
{
    public static readonly bool Avaliable;

    static LZ4API()
    {
        Avaliable = true;
        var assemblyPath = System.Reflection.Assembly.GetAssembly(typeof(LZ4API)).Location;
        var root = string.Empty;
        try
        {
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                root = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
            }

            var map = new Dictionary<string, List<DynDllMapping>>
            {
                {
                    "lz4wrap.dll", [
                        "lz4wrap.dll",
                        "x64/lz4wrap.dll",
                        "plugins/x64/lz4wrap.dll",
                        "BepInEx/scripts/x64/lz4wrap.dll",
                        Path.Combine(root, "lz4wrap.dll"),
                        Path.Combine(root, "x64/lz4wrap.dll"),
                        Path.Combine(root, "plugins/x64/lz4wrap.dll")
                    ]
                },
            };
            typeof(LZ4API).ResolveDynDllImports(map);
        }
        catch (Exception e)
        {
            Avaliable = false;
            Console.WriteLine($"Error: {e}");
        }
    }

    public LZ4API()
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

    [DynDllImport(libraryName: "lz4wrap.dll", "CompressBufferBound")] protected static CompressBufferBoundFunc CompressBufferBound_;
    [DynDllImport(libraryName: "lz4wrap.dll", "CompressBegin")] protected static CompressBeginFunc CompressBegin_;
    [DynDllImport(libraryName: "lz4wrap.dll", "CompressEnd")] protected static CompressEndFunc CompressEnd_;
    [DynDllImport(libraryName: "lz4wrap.dll", "CompressUpdate")] protected static CompressUpdateFunc CompressUpdate_;
    [DynDllImport(libraryName: "lz4wrap.dll", "CompressContextFree")] protected static CompressContextFreeFunc CompressContextFree_;
    [DynDllImport(libraryName: "lz4wrap.dll", "DecompressBegin")] protected static DecompressBeginFunc DecompressBegin_;
    [DynDllImport(libraryName: "lz4wrap.dll", "DecompressEnd")] protected static DecompressEndFunc DecompressEnd_;
    [DynDllImport(libraryName: "lz4wrap.dll", "DecompressUpdate")] protected static DecompressUpdateFunc DecompressUpdate_;
    [DynDllImport(libraryName: "lz4wrap.dll", "DecompressContextReset")] protected static DecompressContextResetFunc DecompressContextReset_;
}
