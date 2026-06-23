using System;
using System.Reflection;
using UnityEngine;
using UXAssist.Common.Utils;

namespace UXAssist.Common;

public static class Util
{
    [Obsolete("Use ReflectionUtil.GetTypesFiltered")]
    public static Type[] GetTypesFiltered(Assembly assembly, Func<Type, bool> predicate)
        => ReflectionUtil.GetTypesFiltered(assembly, predicate);

    [Obsolete("Use ReflectionUtil.GetTypesInNamespace")]
    public static Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
        => ReflectionUtil.GetTypesInNamespace(assembly, nameSpace);

    [Obsolete("Use ReflectionUtil.GetTypesInNamespacePrefix")]
    public static Type[] GetTypesInNamespacePrefix(Assembly assembly, string prefix)
        => ReflectionUtil.GetTypesInNamespacePrefix(assembly, prefix);

    [Obsolete("Use ResourceUtil.LoadEmbeddedResource")]
    public static byte[] LoadEmbeddedResource(string path, Assembly assembly = null)
        => ResourceUtil.LoadEmbeddedResource(path, assembly);

    [Obsolete("Use ResourceUtil.LoadTexture")]
    public static Texture2D LoadTexture(string path)
        => ResourceUtil.LoadTexture(path);

    [Obsolete("Use ResourceUtil.LoadSprite")]
    public static Sprite LoadSprite(string path)
        => ResourceUtil.LoadSprite(path);

    [Obsolete("Use ResourceUtil.LoadEmbeddedTexture")]
    public static Texture2D LoadEmbeddedTexture(string path, Assembly assembly = null)
        => ResourceUtil.LoadEmbeddedTexture(path, assembly);

    [Obsolete("Use ResourceUtil.LoadEmbeddedSprite")]
    public static Sprite LoadEmbeddedSprite(string path, Assembly assembly = null)
        => ResourceUtil.LoadEmbeddedSprite(path, assembly);

    [Obsolete("Use PathUtil.PluginFolder")]
    public static string PluginFolder(Assembly assembly = null)
        => PathUtil.PluginFolder(assembly);
}
