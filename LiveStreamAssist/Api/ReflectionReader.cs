using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LiveStreamAssist.Api;

internal sealed class MemberInfoDto
{
    public string Name;
    public string Type;
    public string Kind;
}

internal sealed class CollectionInfoDto
{
    public string Kind;
    public string ElementType;
    public string KeyType;
    public int? Count;
}

internal sealed class WalkResult
{
    public object Encoded;
    public string TypeName;
    public bool IsNull;
    public MemberInfoDto[] Members;
    public CollectionInfoDto Collection;
    public ApiError Error;
}

internal sealed class ReflectionReader
{
    const int MaxCache = 1024;
    static readonly Assembly GameAssembly = typeof(GameData).Assembly;
    static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, false);
    internal static Action<string> Warn;

    readonly Dictionary<Type, CachedType> _cache = new Dictionary<Type, CachedType>();
    readonly HashSet<Assembly> _extraAssemblies;
    readonly object _cacheGate = new object();

    public ReflectionReader(params Assembly[] extraAllowed)
    {
        if (extraAllowed != null && extraAllowed.Length > 0)
            _extraAssemblies = new HashSet<Assembly>(extraAllowed);
    }

    public void ClearCache()
    {
        lock (_cacheGate) _cache.Clear();
    }

    public WalkResult Describe(object root, Type declaredType, PathSeg[] path)
    {
        var walked = Walk(root, declaredType, path);
        if (walked.Error != null) return Fail(walked.Error);
        return DescribeTerminal(walked.Value, walked.DeclaredType, walked.RuntimeType);
    }

    public WalkResult Read(object root, Type declaredType, PathSeg[] path, ReadOptions options)
    {
        var walked = Walk(root, declaredType, path);
        if (walked.Error != null) return Fail(walked.Error);
        return ReadTerminal(walked.Value, walked.DeclaredType, walked.RuntimeType, options);
    }

    public WalkResult DescribeTerminal(object value, Type declaredType, Type runtimeType)
    {
        var typeName = TypeName(value == null ? declaredType : runtimeType ?? value.GetType());
        var result = new WalkResult
        {
            TypeName = typeName,
            IsNull = value == null,
            Members = Array.Empty<MemberInfoDto>(),
            Encoded = null
        };
        var effective = value == null ? declaredType : runtimeType ?? value.GetType();
        if (TryCollectionKind(effective, out var kind, out var elementType, out var keyType))
        {
            int? count = null;
            if (value != null)
                count = GetCount(value, effective);
            result.Collection = new CollectionInfoDto
            {
                Kind = kind,
                ElementType = TypeName(elementType),
                KeyType = keyType == null ? null : TypeName(keyType),
                Count = count
            };
            return result;
        }

        if (value != null && IsScalar(Unwrap(effective)))
            return result;

        if (effective != null && IsAllowedType(effective) && !IsScalar(Unwrap(effective)))
            result.Members = GetExposedMembers(effective);
        return result;
    }

    public WalkResult ReadTerminal(object value, Type declaredType, Type runtimeType, ReadOptions options)
    {
        var typeName = TypeName(value == null ? declaredType : runtimeType ?? value.GetType());
        if (value == null)
        {
            if (options.HasOffset || options.HasLimit)
            {
                var declared = declaredType;
                if (!TryCollectionKind(declared, out var nullKind, out _, out _) ||
                    (!string.Equals(nullKind, "array", StringComparison.Ordinal) &&
                     !string.Equals(nullKind, "list", StringComparison.Ordinal)))
                    return Fail(ApiErrors.InvalidParams("offset and limit are valid only for array or list terminals."));
                return Fail(ApiErrors.InvalidParams("offset and limit require a non-null array or list."));
            }

            return new WalkResult { TypeName = typeName, IsNull = true, Encoded = null };
        }

        var actual = runtimeType ?? value.GetType();
        if (TryCollectionKind(actual, out var kind, out var elementType, out _))
        {
            if (string.Equals(kind, "dictionary", StringComparison.Ordinal))
            {
                if (options.HasSelect)
                    return Fail(ApiErrors.InvalidParams("select is invalid for dictionary terminals."));
                if (options.HasOffset || options.HasLimit)
                    return Fail(ApiErrors.InvalidParams("offset and limit are valid only for array or list terminals."));
                return Ok(typeName, CollectionSummary(kind, actual, GetCount(value, actual)));
            }

            if (options.HasSelect && IsScalar(Unwrap(elementType)))
                return Fail(ApiErrors.InvalidParams("select is invalid for scalar collection elements."));
            if (options.HasSelect && TryCollectionKind(elementType, out _, out _, out _))
                return Fail(ApiErrors.InvalidParams("select is invalid for collection elements."));

            var page = ReadPage(value, actual, elementType, options);
            if (page.Error != null) return page;
            page.TypeName = typeName;
            return page;
        }

        if (options.HasOffset || options.HasLimit)
            return Fail(ApiErrors.InvalidParams("offset and limit are valid only for array or list terminals."));

        if (IsScalar(Unwrap(actual)))
        {
            if (options.HasSelect)
                return Fail(ApiErrors.InvalidParams("select is invalid for scalar terminals."));
            if (!TryEncodeScalar(value, actual, out var encoded, out var err))
                return Fail(err);
            return Ok(typeName, encoded);
        }

        if (!IsAllowedType(actual))
            return Fail(ApiErrors.UnsupportedType());

        if (options.HasSelect)
        {
            var projected = Project(value, actual, options.Select);
            if (projected.Error != null) return projected;
            projected.TypeName = typeName;
            return projected;
        }

        return Ok(typeName, ObjectSummary(actual));
    }

    WalkStep Walk(object root, Type declaredType, PathSeg[] path)
    {
        var current = root;
        var declared = declaredType;
        var runtime = current == null ? null : current.GetType();
        if (path == null || path.Length == 0)
            return new WalkStep { Value = current, DeclaredType = declared, RuntimeType = runtime };

        for (var i = 0; i < path.Length; i++)
        {
            if (current == null)
                return new WalkStep { Error = ApiErrors.NullPath(i) };
            runtime = current.GetType();
            var seg = path[i];
            if (seg.Kind == PathKind.Member)
            {
                var slot = FindMember(runtime, seg.Name);
                if (slot == null)
                    return new WalkStep { Error = ApiErrors.MemberNotFound(seg.Name, i) };
                if (!slot.Allowed)
                    return new WalkStep { Error = ApiErrors.MemberNotAllowed(seg.Name, i) };
                if (!TryGetMemberValue(current, slot, i, out current, out var err))
                    return new WalkStep { Error = err };
                declared = slot.DeclaredType;
                runtime = current == null ? null : current.GetType();
                continue;
            }

            if (seg.Kind == PathKind.Index)
            {
                if (!TryCollectionKind(runtime, out var kind, out var elementType, out _) ||
                    !string.Equals(kind, "array", StringComparison.Ordinal) &&
                    !string.Equals(kind, "list", StringComparison.Ordinal))
                    return new WalkStep { Error = ApiErrors.UnsupportedType(i) };
                if (!TryGetIndex(current, runtime, seg.Index, i, out current, out var err))
                    return new WalkStep { Error = err };
                declared = elementType;
                runtime = current == null ? null : current.GetType();
                continue;
            }

            if (!TryCollectionKind(runtime, out var dkind, out var valueType, out var keyType) ||
                !string.Equals(dkind, "dictionary", StringComparison.Ordinal))
                return new WalkStep { Error = ApiErrors.UnsupportedType(i) };
            if (!TryCoerceKey(seg.Key, keyType, out var key, out var keyErr))
                return new WalkStep { Error = keyErr ?? ApiErrors.InvalidParams("dictionary key type mismatch.") };
            if (!TryGetKey(current, key, i, out current, out var kerr))
                return new WalkStep { Error = kerr };
            declared = valueType;
            runtime = current == null ? null : current.GetType();
        }

        return new WalkStep { Value = current, DeclaredType = declared, RuntimeType = current == null ? null : current.GetType() };
    }

    WalkResult ReadPage(object collection, Type collectionType, Type elementType, ReadOptions options)
    {
        var total = GetCount(collection, collectionType);
        var offset = options.EffectiveOffset;
        var limit = options.EffectiveLimit;
        if (options.HasSelect)
        {
            var probe = GetCached(elementType);
            if (options.Select != null)
            {
                foreach (var name in options.Select)
                {
                    var slot = FindMember(elementType, name);
                    if (slot == null) return Fail(ApiErrors.MemberNotFound(name));
                    if (!slot.Allowed) return Fail(ApiErrors.MemberNotAllowed(name));
                }
            }

            _ = probe;
        }

        var items = new List<object>();
        if (offset < total)
        {
            var take = Math.Min(limit, total - offset);
            for (var i = 0; i < take; i++)
            {
                if (!TryGetIndex(collection, collectionType, offset + i, null, out var item, out var err))
                    return Fail(err);
                if (item == null)
                {
                    items.Add(null);
                    continue;
                }

                var itemType = item.GetType();
                if (options.HasSelect)
                {
                    if (IsScalar(Unwrap(itemType)) || TryCollectionKind(itemType, out _, out _, out _))
                        return Fail(ApiErrors.InvalidParams("select is invalid for scalar or collection elements."));
                    var projected = Project(item, itemType, options.Select);
                    if (projected.Error != null) return projected;
                    items.Add(projected.Encoded);
                }
                else if (IsScalar(Unwrap(itemType)))
                {
                    if (!TryEncodeScalar(item, itemType, out var encoded, out var encErr))
                        return Fail(encErr);
                    items.Add(encoded);
                }
                else if (TryCollectionKind(itemType, out var ikind, out _, out _))
                    items.Add(CollectionSummary(ikind, itemType, GetCount(item, itemType)));
                else if (!IsAllowedType(itemType))
                    return Fail(ApiErrors.UnsupportedType());
                else
                    items.Add(ObjectSummary(itemType));
            }
        }

        return new WalkResult
        {
            Encoded = new Dictionary<string, object>
            {
                ["items"] = items,
                ["offset"] = offset,
                ["totalCount"] = total,
                ["hasMore"] = offset + items.Count < total
            }
        };
    }

    WalkResult Project(object value, Type type, string[] select)
    {
        var map = new Dictionary<string, object>(select.Length);
        foreach (var name in select)
        {
            var slot = FindMember(type, name);
            if (slot == null) return Fail(ApiErrors.MemberNotFound(name));
            if (!slot.Allowed) return Fail(ApiErrors.MemberNotAllowed(name));
            if (!TryGetMemberValue(value, slot, null, out var memberValue, out var err))
                return Fail(err);
            if (memberValue == null)
            {
                map[name] = null;
                continue;
            }

            var mt = memberValue.GetType();
            if (IsScalar(Unwrap(mt)))
            {
                if (!TryEncodeScalar(memberValue, mt, out var encoded, out var encErr))
                    return Fail(encErr);
                map[name] = encoded;
            }
            else if (TryCollectionKind(mt, out var kind, out _, out _))
                map[name] = CollectionSummary(kind, mt, GetCount(memberValue, mt));
            else if (!IsAllowedType(mt))
                return Fail(ApiErrors.UnsupportedType(member: name));
            else
                map[name] = ObjectSummary(mt);
        }

        return new WalkResult { Encoded = map };
    }

    bool TryGetMemberValue(object instance, MemberSlot slot, int? segmentIndex, out object value, out ApiError error)
    {
        value = null;
        error = null;
        try
        {
            if (slot.Field != null)
            {
                value = slot.Field.GetValue(instance);
                return true;
            }

            value = slot.Property.GetValue(instance, null);
            return true;
        }
        catch (Exception ex)
        {
            Warn?.Invoke($"API read failed at {slot.Name}: {ex}");
            error = ApiErrors.ReadFailed(segmentIndex, slot.Name);
            return false;
        }
    }

    bool TryGetIndex(object collection, Type collectionType, int index, int? segmentIndex, out object value, out ApiError error)
    {
        value = null;
        error = null;
        var count = GetCount(collection, collectionType);
        if (index < 0 || index >= count)
        {
            error = ApiErrors.IndexOutOfRange(segmentIndex ?? 0);
            return false;
        }

        try
        {
            if (collectionType.IsArray)
            {
                value = ((Array)collection).GetValue(index);
                return true;
            }

            value = ((IList)collection)[index];
            return true;
        }
        catch (Exception ex)
        {
            Warn?.Invoke($"API index read failed: {ex}");
            error = ApiErrors.ReadFailed(segmentIndex);
            return false;
        }
    }

    static bool TryGetKey(object collection, object key, int segmentIndex, out object value, out ApiError error)
    {
        value = null;
        error = null;
        try
        {
            var dict = (IDictionary)collection;
            if (!dict.Contains(key))
            {
                error = ApiErrors.KeyNotFound(segmentIndex);
                return false;
            }

            value = dict[key];
            return true;
        }
        catch (Exception ex)
        {
            Warn?.Invoke($"API key read failed: {ex}");
            error = ApiErrors.ReadFailed(segmentIndex);
            return false;
        }
    }

    static bool TryCoerceKey(object raw, Type keyType, out object key, out ApiError error)
    {
        key = null;
        error = null;
        if (keyType == typeof(int))
        {
            if (raw is int i)
            {
                key = i;
                return true;
            }

            error = ApiErrors.InvalidParams("dictionary key type mismatch.");
            return false;
        }

        if (keyType == typeof(string))
        {
            if (raw is string s)
            {
                key = s;
                return true;
            }

            error = ApiErrors.InvalidParams("dictionary key type mismatch.");
            return false;
        }

        error = ApiErrors.UnsupportedType();
        return false;
    }

    static int GetCount(object collection, Type type)
    {
        if (type.IsArray) return ((Array)collection).Length;
        return ((ICollection)collection).Count;
    }

    MemberSlot FindMember(Type type, string name)
    {
        var cached = GetCached(type);
        return cached.ByName.TryGetValue(name, out var slot) ? slot : null;
    }

    MemberInfoDto[] GetExposedMembers(Type type)
    {
        return GetCached(type).Exposed;
    }

    CachedType GetCached(Type type)
    {
        lock (_cacheGate)
        {
            if (_cache.TryGetValue(type, out var hit))
                return hit;
        }

        var built = BuildMembers(type);
        lock (_cacheGate)
        {
            if (_cache.TryGetValue(type, out var hit))
                return hit;
            if (_cache.Count < MaxCache)
                _cache[type] = built;
            return built;
        }
    }

    CachedType BuildMembers(Type type)
    {
        var byName = new Dictionary<string, MemberSlot>(StringComparer.Ordinal);
        for (var t = type; t != null && t != typeof(object); t = t.BaseType)
        {
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.IsStatic || field.IsDefined(typeof(CompilerGeneratedAttribute), false) || field.Name.IndexOf('<') >= 0)
                    continue;
                if (byName.ContainsKey(field.Name))
                    continue;
                var ft = field.FieldType;
                byName[field.Name] = new MemberSlot
                {
                    Name = field.Name,
                    Kind = "field",
                    DeclaredType = ft,
                    Field = field,
                    Allowed = IsAllowedType(ft)
                };
            }

            foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (prop.GetIndexParameters().Length != 0)
                    continue;
                if (byName.ContainsKey(prop.Name))
                    continue;
                var allowed = IsAllowedProperty(prop) && IsAllowedType(prop.PropertyType) && prop.CanRead && prop.GetGetMethod(true) != null;
                byName[prop.Name] = new MemberSlot
                {
                    Name = prop.Name,
                    Kind = "property",
                    DeclaredType = prop.PropertyType,
                    Property = prop,
                    Allowed = allowed
                };
            }
        }

        var exposed = new List<MemberInfoDto>();
        foreach (var pair in byName)
        {
            if (!pair.Value.Allowed) continue;
            exposed.Add(new MemberInfoDto
            {
                Name = pair.Value.Name,
                Type = TypeName(pair.Value.DeclaredType),
                Kind = pair.Value.Kind
            });
        }

        exposed.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return new CachedType { ByName = byName, Exposed = exposed.ToArray() };
    }

    static bool IsAllowedProperty(PropertyInfo prop)
    {
        return prop.DeclaringType == typeof(GameHistoryData) &&
               string.Equals(prop.Name, "currentTech", StringComparison.Ordinal);
    }

    bool IsAllowedType(Type type)
    {
        if (type == null) return false;
        type = Unwrap(type);
        if (IsForbidden(type)) return false;
        if (IsScalar(type) || IsUnityStruct(type)) return true;
        if (IsSupportedArray(type) || IsSupportedList(type) || IsSupportedDictionary(type)) return true;
        if (type.Assembly == GameAssembly) return true;
        if (_extraAssemblies != null && _extraAssemblies.Contains(type.Assembly)) return true;
        return false;
    }

    bool IsSupportedArray(Type type)
    {
        if (!type.IsArray || type.GetArrayRank() != 1) return false;
        var element = type.GetElementType();
        return element != null && !element.IsPointer && IsAllowedType(element);
    }

    bool IsSupportedList(Type type)
    {
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(List<>)) return false;
        return IsAllowedType(type.GetGenericArguments()[0]);
    }

    bool IsSupportedDictionary(Type type)
    {
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Dictionary<,>)) return false;
        var args = type.GetGenericArguments();
        if (args[0] != typeof(int) && args[0] != typeof(string)) return false;
        return IsAllowedType(args[1]);
    }

    bool TryCollectionKind(Type type, out string kind, out Type elementType, out Type keyType)
    {
        kind = null;
        elementType = null;
        keyType = null;
        if (type == null) return false;
        type = Unwrap(type);
        if (IsSupportedArray(type))
        {
            kind = "array";
            elementType = type.GetElementType();
            return true;
        }

        if (IsSupportedList(type))
        {
            kind = "list";
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        if (IsSupportedDictionary(type))
        {
            kind = "dictionary";
            var args = type.GetGenericArguments();
            keyType = args[0];
            elementType = args[1];
            return true;
        }

        return false;
    }

    static bool IsForbidden(Type t)
    {
        if (t.IsPointer || t.IsByRef || t.IsGenericParameter) return true;
        if (t == typeof(IntPtr) || t == typeof(UIntPtr) || t == typeof(TypedReference)) return true;
        if (typeof(Delegate).IsAssignableFrom(t)) return true;
        if (typeof(Type).IsAssignableFrom(t) || typeof(MemberInfo).IsAssignableFrom(t) ||
            typeof(Assembly).IsAssignableFrom(t) || typeof(Module).IsAssignableFrom(t))
            return true;
        if (typeof(System.IO.Stream).IsAssignableFrom(t) || typeof(System.IO.TextReader).IsAssignableFrom(t) ||
            typeof(System.IO.TextWriter).IsAssignableFrom(t))
            return true;
        if (typeof(Task).IsAssignableFrom(t) || typeof(Thread).IsAssignableFrom(t) || typeof(WaitHandle).IsAssignableFrom(t))
            return true;
        if (t == typeof(SpinLock) || t == typeof(Mutex) || t == typeof(Semaphore) || t == typeof(ReaderWriterLock) ||
            t == typeof(ReaderWriterLockSlim))
            return true;
        if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return true;
        return false;
    }

    static bool IsScalar(Type t)
    {
        if (t.IsEnum) return true;
        return t == typeof(bool) || t == typeof(string) || t == typeof(char) ||
               t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) ||
               t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) ||
               t == typeof(float) || t == typeof(double) || t == typeof(decimal) ||
               t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(Guid);
    }

    static bool IsUnityStruct(Type t)
    {
        return t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4) || t == typeof(Quaternion);
    }

    static Type Unwrap(Type t)
    {
        return Nullable.GetUnderlyingType(t) ?? t;
    }

    static bool TryEncodeScalar(object value, Type type, out object encoded, out ApiError error)
    {
        encoded = null;
        error = null;
        type = Unwrap(type);
        if (value == null)
            return true;
        if (type.IsEnum)
        {
            var name = Enum.GetName(type, value);
            var underlying = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            encoded = new Dictionary<string, object>
            {
                ["name"] = name,
                ["value"] = underlying.ToString(CultureInfo.InvariantCulture)
            };
            return true;
        }

        if (type == typeof(bool))
        {
            encoded = (bool)value;
            return true;
        }

        if (type == typeof(uint))
        {
            encoded = (uint)value;
            return true;
        }

        if (type == typeof(int) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) ||
            type == typeof(ushort))
        {
            encoded = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            return true;
        }

        if (type == typeof(string))
        {
            var s = (string)value;
            if (!CheckStringSize(s, out error)) return false;
            encoded = s;
            return true;
        }

        if (type == typeof(char))
        {
            encoded = ((char)value).ToString();
            return true;
        }

        if (type == typeof(long))
        {
            encoded = ((long)value).ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (type == typeof(ulong))
        {
            encoded = ((ulong)value).ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (type == typeof(decimal))
        {
            encoded = ((decimal)value).ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (type == typeof(float))
        {
            var f = (float)value;
            if (float.IsNaN(f) || float.IsInfinity(f))
            {
                error = ApiErrors.UnsupportedValue();
                return false;
            }

            encoded = f;
            return true;
        }

        if (type == typeof(double))
        {
            var d = (double)value;
            if (double.IsNaN(d) || double.IsInfinity(d))
            {
                error = ApiErrors.UnsupportedValue();
                return false;
            }

            encoded = d;
            return true;
        }

        if (type == typeof(DateTime))
        {
            encoded = ((DateTime)value).ToString("o", CultureInfo.InvariantCulture);
            return true;
        }

        if (type == typeof(DateTimeOffset))
        {
            encoded = ((DateTimeOffset)value).ToString("o", CultureInfo.InvariantCulture);
            return true;
        }

        if (type == typeof(Guid))
        {
            encoded = ((Guid)value).ToString("D");
            return true;
        }

        error = ApiErrors.UnsupportedType();
        return false;
    }

    static bool CheckStringSize(string s, out ApiError error)
    {
        error = null;
        if (s == null) return true;
        if (s.Length > ApiLimits.MaxResponseBytes || Utf8.GetByteCount(s) > ApiLimits.MaxResponseBytes)
        {
            error = ApiErrors.LimitExceeded("String value exceeds maxResponseBytes.");
            return false;
        }

        return true;
    }

    static Dictionary<string, object> ObjectSummary(Type type)
    {
        return new Dictionary<string, object>
        {
            ["$kind"] = "object",
            ["$type"] = TypeName(type)
        };
    }

    static Dictionary<string, object> CollectionSummary(string kind, Type type, int count)
    {
        return new Dictionary<string, object>
        {
            ["$kind"] = kind,
            ["$type"] = TypeName(type),
            ["count"] = count
        };
    }

    internal static string TypeName(Type type)
    {
        if (type == null) return null;
        type = Unwrap(type);
        if (type.IsArray && type.GetArrayRank() == 1)
            return TypeName(type.GetElementType()) + "[]";
        return type.FullName ?? type.Name;
    }

    static WalkResult Fail(ApiError error) => new WalkResult { Error = error };

    static WalkResult Ok(string typeName, object encoded) =>
        new WalkResult { TypeName = typeName, Encoded = encoded, IsNull = encoded == null };

    sealed class MemberSlot
    {
        public string Name;
        public string Kind;
        public Type DeclaredType;
        public FieldInfo Field;
        public PropertyInfo Property;
        public bool Allowed;
    }

    sealed class CachedType
    {
        public Dictionary<string, MemberSlot> ByName;
        public MemberInfoDto[] Exposed;
    }

    struct WalkStep
    {
        public object Value;
        public Type DeclaredType;
        public Type RuntimeType;
        public ApiError Error;
    }
}
