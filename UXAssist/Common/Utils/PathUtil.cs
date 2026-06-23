using System.IO;
using System.Reflection;

namespace UXAssist.Common.Utils;

public static class PathUtil
{
    public static string PluginFolder(Assembly assembly = null) => Path.GetDirectoryName((assembly == null ? Assembly.GetCallingAssembly() : assembly).Location);
}
