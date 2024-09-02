using System.IO;
using System.Reflection;
using UnityEngine;

namespace UXAssist.Common;

public static class Util
{
    public static Texture2D LoadTexture(string path)
    {
        var fileData = System.IO.File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(fileData);
        return tex;
    }
    
    public static Sprite LoadSprite(string path)
    {
        var tex = LoadTexture(path);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
    
    public static string PluginFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
}