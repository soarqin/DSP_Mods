using System;
using System.Linq;
using System.Reflection;

namespace UXAssist.Common.Utils;

public static class ReflectionUtil
{
    /// <summary>
    /// Returns all types from the assembly matching the predicate, tolerating partially loadable assemblies.
    /// </summary>
    public static Type[] GetTypesFiltered(Assembly assembly, Func<Type, bool> predicate)
    {
        try
        {
            return [.. assembly.GetTypes().Where(predicate)];
        }
        catch (ReflectionTypeLoadException ex)
        {
            return [.. ex.Types.Where(t => t != null).Where(predicate)];
        }
    }

    public static Type[] GetTypesInNamespace(Assembly assembly, string nameSpace) => GetTypesFiltered(assembly, t => string.Equals(t.Namespace, nameSpace, StringComparison.Ordinal));

    public static Type[] GetTypesInNamespacePrefix(Assembly assembly, string prefix)
    {
        return GetTypesFiltered(assembly, t =>
            t.Namespace != null &&
            (t.Namespace == prefix || t.Namespace.StartsWith(prefix + ".", StringComparison.Ordinal)));
    }
}
