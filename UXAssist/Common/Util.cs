using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UXAssist.Common;

public static class Util
{
    public static Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
    {
        return assembly.GetTypes().Where(t => string.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
    }

    public static byte[] LoadEmbeddedResource(string path, Assembly assembly = null)
    {
        if (assembly == null)
        {
            assembly = Assembly.GetCallingAssembly();
        }
        var info = assembly.GetName();
        var name = info.Name;
        using var stream = assembly.GetManifestResourceStream($"{name}.{path.Replace('/', '.')}")!;
        var buffer = new byte[stream.Length];
        _ = stream.Read(buffer, 0, buffer.Length);
        return buffer;
    }

    public static Texture2D LoadTexture(string path)
    {
        var fileData = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(fileData);
        return tex;
    }
    
    public static Sprite LoadSprite(string path)
    {
        var tex = LoadTexture(path);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
    
    public static Texture2D LoadEmbeddedTexture(string path, Assembly assembly = null)
    {
        var fileData = LoadEmbeddedResource(path, assembly);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(fileData);
        return tex;
    }
    
    public static Sprite LoadEmbeddedSprite(string path, Assembly assembly = null)
    {
        var tex = LoadEmbeddedTexture(path, assembly);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
    
    public static string PluginFolder(Assembly assembly = null) => Path.GetDirectoryName((assembly == null ? Assembly.GetCallingAssembly() : assembly).Location);
}
