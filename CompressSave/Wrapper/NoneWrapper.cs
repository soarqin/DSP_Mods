using System;
using System.Collections.Generic;
using System.IO;
using MonoMod.Utils;

namespace CompressSave.Wrapper;

public class NoneAPI: WrapperDefines
{
    public static readonly bool Avaliable;

    static NoneAPI()
    {
        Avaliable = true;
        string assemblyPath = System.Reflection.Assembly.GetAssembly(typeof(NoneAPI)).Location;
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
                    "nonewrap.dll", [
                        "nonewrap.dll",
                        "x64/nonewrap.dll",
                        "plugins/x64/nonewrap.dll",
                        "BepInEx/scripts/x64/nonewrap.dll",
                        Path.Combine(root, "nonewrap.dll"),
                        Path.Combine(root, "x64/nonewrap.dll"),
                        Path.Combine(root, "plugins/x64/nonewrap.dll")
                    ]
                },
            };
            typeof(NoneAPI).ResolveDynDllImports(map);
        }
        catch (Exception e)
        {
            Avaliable = false;
            Console.WriteLine($"Error: {e}");
        }
    }

    public NoneAPI()
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

    [DynDllImport(libraryName: "nonewrap.dll", "CompressBufferBound")] protected static CompressBufferBoundFunc CompressBufferBound_;
    [DynDllImport(libraryName: "nonewrap.dll", "CompressBegin")] protected static CompressBeginFunc CompressBegin_;
    [DynDllImport(libraryName: "nonewrap.dll", "CompressEnd")] protected static CompressEndFunc CompressEnd_;
    [DynDllImport(libraryName: "nonewrap.dll", "CompressUpdate")] protected static CompressUpdateFunc CompressUpdate_;
    [DynDllImport(libraryName: "nonewrap.dll", "CompressContextFree")] protected static CompressContextFreeFunc CompressContextFree_;
    [DynDllImport(libraryName: "nonewrap.dll", "DecompressBegin")] protected static DecompressBeginFunc DecompressBegin_;
    [DynDllImport(libraryName: "nonewrap.dll", "DecompressEnd")] protected static DecompressEndFunc DecompressEnd_;
    [DynDllImport(libraryName: "nonewrap.dll", "DecompressUpdate")] protected static DecompressUpdateFunc DecompressUpdate_;
    [DynDllImport(libraryName: "nonewrap.dll", "DecompressContextReset")] protected static DecompressContextResetFunc DecompressContextReset_;
}
