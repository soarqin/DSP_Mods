using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace UXAssist.Common.ModCompat;

public static class ModCompatHelper
{
    public static bool TryGetLoadedPluginInfo(string guid, out BepInEx.PluginInfo pluginInfo)
    {
        return Chainloader.PluginInfos.TryGetValue(guid, out pluginInfo) && pluginInfo != null;
    }

    public static bool TryGetPluginType(BepInEx.PluginInfo pluginInfo, string typeName, out Type type)
    {
        type = null;
        if (pluginInfo?.Instance == null) return false;
        try
        {
            type = pluginInfo.Instance.GetType().Assembly.GetType(typeName, throwOnError: false);
        }
        catch
        {
            // ignored
        }
        return type != null;
    }

    public static bool TryGetPluginType(string guid, string typeName, out Type type)
    {
        type = null;
        return TryGetLoadedPluginInfo(guid, out var pluginInfo) && TryGetPluginType(pluginInfo, typeName, out type);
    }

    public static bool TryGetField(Type type, string fieldName, out FieldInfo field)
    {
        field = null;
        if (type == null) return false;
        field = AccessTools.Field(type, fieldName);
        return field != null;
    }

    public static bool TryGetFieldValue<T>(Type type, string fieldName, object instance, out T value)
    {
        value = default;
        if (!TryGetField(type, fieldName, out var field)) return false;
        try
        {
            var result = field.GetValue(instance);
            if (result is T t)
            {
                value = t;
                return true;
            }
        }
        catch
        {
            // ignored
        }
        return false;
    }

    public static bool TryGetMethod(Type type, string methodName, out MethodInfo method)
    {
        method = null;
        if (type == null) return false;
        method = AccessTools.Method(type, methodName);
        return method != null;
    }

    public static bool TryGetPropertySetter(Type type, string propertyName, out MethodInfo setter)
    {
        setter = null;
        if (type == null) return false;
        var property = AccessTools.Property(type, propertyName);
        if (property == null) return false;
        setter = property.GetSetMethod(nonPublic: true);
        return setter != null;
    }
}
