using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using LiveStreamAssist.Api;
using UnityEngine;

namespace ReflectionReaderCheck;

static class Program
{
    static int _failed;

    static int Main()
    {
        var reader = new ReflectionReader(typeof(Program).Assembly);
        try
        {
            InheritedPrivateFields(reader);
            BlockedGetter(reader);
            ClusterStringProperty(reader);
            NullIntermediateAndTerminal(reader);
            DictionaryKeys(reader);
            Indexes(reader);
            ShallowCycle(reader);
            Int64Precision(reader);
            NonFinite(reader);
            UnityAndStreamBlocked(reader);
            NullCollectionPaging(reader);
            RuntimeProjectionMembers(reader);
            UnsignedEnum(reader);
            RuntimeTypeBoundary(reader);
            FrameworkInternals(reader);
        }
        finally
        {
            reader.ClearCache();
        }

        if (_failed > 0)
        {
            Console.Error.WriteLine($"FAILED {_failed} check(s)");
            return 1;
        }

        Console.WriteLine("OK");
        return 0;
    }

    static void InheritedPrivateFields(ReflectionReader reader)
    {
        var obj = new DerivedFixture { Visible = 3 };
        obj.SetBaseSecret(9);
        var result = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("baseSecret") }, default);
        Check(result.Error == null, "inherited private field readable");
        Check(Equals(result.Encoded, 9), "inherited private field value");
        var desc = reader.Describe(obj, typeof(DerivedFixture), Array.Empty<PathSeg>());
        Check(HasMember(desc, "baseSecret"), "describe lists inherited private field");
        Check(HasMember(desc, "Visible"), "describe lists derived field");
    }

    static void BlockedGetter(ReflectionReader reader)
    {
        var obj = new DerivedFixture();
        var read = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Boom") }, default);
        Check(read.Error != null && read.Error.Kind == "MEMBER_NOT_ALLOWED", "blocked getter is MEMBER_NOT_ALLOWED");
        var backing = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("<Boom>k__BackingField") }, default);
        Check(backing.Error != null && backing.Error.Kind == "MEMBER_NOT_FOUND", "backing field not exposed");
        var desc = reader.Describe(obj, typeof(DerivedFixture), Array.Empty<PathSeg>());
        Check(!HasMember(desc, "Boom"), "blocked getter omitted from describe");
    }

    static void ClusterStringProperty(ReflectionReader reader)
    {
        var described = reader.Describe(null, typeof(GameDesc), Array.Empty<PathSeg>());
        Check(described.Error == null && HasMember(described, "clusterString"),
            "cluster string property is allowlisted");
    }

    static void NullIntermediateAndTerminal(ReflectionReader reader)
    {
        var obj = new DerivedFixture();
        var terminal = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Child") }, default);
        Check(terminal.Error == null && terminal.IsNull, "null terminal succeeds");
        var mid = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Child"), new PathSeg("Visible") }, default);
        Check(mid.Error != null && mid.Error.Kind == "NULL_PATH", "null intermediate is NULL_PATH");
        var desc = reader.Describe(obj, typeof(DerivedFixture), new[] { new PathSeg("Child") });
        Check(desc.Error == null && desc.IsNull, "describe null terminal");
        Check(desc.TypeName == "ReflectionReaderCheck.DerivedFixture", "null uses declared type");
    }

    static void DictionaryKeys(ReflectionReader reader)
    {
        var obj = new DerivedFixture();
        obj.IntDict[1001] = 4;
        obj.StrDict["a"] = 5;
        var ok = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("IntDict"), new PathSeg((object)1001) }, default);
        Check(ok.Error == null && Equals(ok.Encoded, 4), "int dictionary key");
        var mismatch = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("IntDict"), new PathSeg((object)"1001") }, default);
        Check(mismatch.Error != null && mismatch.Error.Kind == "INVALID_PARAMS", "string key not converted to int");
        var missing = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("IntDict"), new PathSeg((object)9) }, default);
        Check(missing.Error != null && missing.Error.Kind == "KEY_NOT_FOUND", "missing int key");
        var str = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("StrDict"), new PathSeg((object)"a") }, default);
        Check(str.Error == null && Equals(str.Encoded, 5), "string dictionary key");
        var summary = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("IntDict") }, default);
        Check(summary.Error == null && summary.Encoded is Dictionary<string, object> d && Equals(d["$kind"], "dictionary"),
            "dictionary terminal is summary");
    }

    static void Indexes(ReflectionReader reader)
    {
        var obj = new DerivedFixture { Arr = new[] { 10, 20, 30 }, List = new List<int> { 7, 8 } };
        var a0 = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Arr"), new PathSeg(0) }, default);
        Check(a0.Error == null && Equals(a0.Encoded, 10), "array index 0");
        var oob = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Arr"), new PathSeg(3) }, default);
        Check(oob.Error != null && oob.Error.Kind == "INDEX_OUT_OF_RANGE", "array oob");
        var page = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Arr") },
            new ReadOptions { HasOffset = true, Offset = 1, HasLimit = true, Limit = 1 });
        Check(page.Error == null, "array page");
        var dict = (Dictionary<string, object>)page.Encoded;
        var items = (List<object>)dict["items"];
        Check(items.Count == 1 && Equals(items[0], 20) && Equals(dict["totalCount"], 3) && Equals(dict["hasMore"], true),
            "array page contents");
        var empty = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("List") },
            new ReadOptions { HasOffset = true, Offset = 8, HasLimit = true, Limit = 2 });
        var emptyDict = (Dictionary<string, object>)empty.Encoded;
        Check(((List<object>)emptyDict["items"]).Count == 0 && Equals(emptyDict["hasMore"], false) && Equals(emptyDict["offset"], 8),
            "offset past end is empty page");
    }

    static void ShallowCycle(ReflectionReader reader)
    {
        var obj = new DerivedFixture();
        obj.Cycle = obj;
        var summary = reader.Read(obj, typeof(DerivedFixture), Array.Empty<PathSeg>(), default);
        Check(summary.Error == null && summary.Encoded is Dictionary<string, object> d && Equals(d["$kind"], "object"),
            "cyclic object without select is summary");
        var hop = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Cycle"), new PathSeg("Cycle") }, default);
        Check(hop.Error == null && hop.Encoded is Dictionary<string, object> d2 && Equals(d2["$kind"], "object"),
            "explicit cyclic path stays a summary");
    }

    static void Int64Precision(ReflectionReader reader)
    {
        var obj = new DerivedFixture { Big = 9007199254740993L, BigU = 18446744073709551615UL };
        var signed = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Big") }, default);
        Check(Equals(signed.Encoded, "9007199254740993"), "int64 beyond 2^53 is decimal string");
        var unsigned = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("BigU") }, default);
        Check(Equals(unsigned.Encoded, ulong.MaxValue.ToString(CultureInfo.InvariantCulture)), "uint64 is decimal string");
    }

    static void NonFinite(ReflectionReader reader)
    {
        var obj = new DerivedFixture { Nan = double.NaN, Inf = double.PositiveInfinity };
        var nan = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Nan") }, default);
        Check(nan.Error != null && nan.Error.Kind == "UNSUPPORTED_VALUE", "NaN is UNSUPPORTED_VALUE");
        var inf = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Inf") }, default);
        Check(inf.Error != null && inf.Error.Kind == "UNSUPPORTED_VALUE", "Infinity is UNSUPPORTED_VALUE");
    }

    static void UnityAndStreamBlocked(ReflectionReader reader)
    {
        var obj = new DerivedFixture();
        var unity = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Texture") }, default);
        Check(unity.Error != null && unity.Error.Kind == "MEMBER_NOT_ALLOWED", "UnityEngine.Object field blocked");
        var stream = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Buffer") }, default);
        Check(stream.Error != null && stream.Error.Kind == "MEMBER_NOT_ALLOWED", "Stream field blocked");
        var desc = reader.Describe(obj, typeof(DerivedFixture), Array.Empty<PathSeg>());
        Check(!HasMember(desc, "Texture") && !HasMember(desc, "Buffer"), "infrastructure omitted from describe");
        var vec = reader.Read(obj, typeof(DerivedFixture), new[] { new PathSeg("Vec") },
            new ReadOptions { HasSelect = true, Select = new[] { "x" } });
        Check(vec.Error == null, "Unity vector fields readable");
    }

    static bool HasMember(WalkResult desc, string name)
    {
        if (desc.Members == null) return false;
        foreach (var m in desc.Members)
        {
            if (m.Name == name) return true;
        }

        return false;
    }

    static void NullCollectionPaging(ReflectionReader reader)
    {
        var result = reader.Read(new DerivedFixture(), typeof(DerivedFixture), new[] { new PathSeg("Arr") },
            new ReadOptions { HasLimit = true, Limit = 1 });
        Check(result.Error == null && result.IsNull, "null array with paging remains null");
    }

    static void RuntimeProjectionMembers(ReflectionReader reader)
    {
        var values = new BaseFixture[] { new DerivedFixture { Visible = 42 } };
        var options = new ReadOptions { HasSelect = true, Select = new[] { "Visible" } };
        var result = reader.Read(values, values.GetType(), Array.Empty<PathSeg>(), options);
        Check(result.Error == null, "page projection resolves members on actual derived objects");
        var empty = reader.Read(Array.Empty<BaseFixture>(), typeof(BaseFixture[]), Array.Empty<PathSeg>(), options);
        Check(empty.Error == null, "empty page does not resolve unread member names");
    }

    static void UnsignedEnum(ReflectionReader reader)
    {
        try
        {
            var result = reader.Read(WideEnum.Max, typeof(WideEnum), Array.Empty<PathSeg>(), default);
            Check(result.Error == null && result.Encoded is Dictionary<string, object> value &&
                  Equals(value["value"], "18446744073709551615"), "ulong enum preserves its full range");
        }
        catch (OverflowException)
        {
            Check(false, "ulong enum preserves its full range");
        }
    }

    static void RuntimeTypeBoundary(ReflectionReader reader)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("UnlistedFixture"), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("main").DefineType("ForeignFixture", TypeAttributes.Public, typeof(BaseFixture));
        type.DefineField("Secret", typeof(int), FieldAttributes.Public);
        var foreign = (BaseFixture)Activator.CreateInstance(type.CreateType());
        var result = reader.Read(foreign, typeof(BaseFixture), new[] { new PathSeg("Secret") }, default);
        Check(result.Error?.Kind == "UNSUPPORTED_TYPE", "runtime type cannot bypass the allowed assembly boundary");
        var described = reader.Describe(foreign, typeof(BaseFixture), Array.Empty<PathSeg>());
        Check(described.Error?.Kind == "UNSUPPORTED_TYPE", "describe enforces the runtime type boundary");
    }

    static void FrameworkInternals(ReflectionReader reader)
    {
        var list = new GameListFixture();
        var result = reader.Read(list, list.GetType(), new[] { new PathSeg("_items") }, default);
        Check(result.Error?.Kind == "MEMBER_NOT_FOUND", "game-defined subclasses do not expose BCL container internals");
        var array = Array.CreateInstance(typeof(BaseFixture), new[] { 1 }, new[] { 5 });
        result = reader.Read(array, array.GetType(), Array.Empty<PathSeg>(), default);
        Check(result.Error?.Kind == "UNSUPPORTED_TYPE", "nonzero-based game arrays are rejected");
    }

    static void Check(bool cond, string name)
    {
        if (cond)
        {
            Console.WriteLine("ok  " + name);
            return;
        }

        _failed++;
        Console.Error.WriteLine("fail " + name);
    }
}

public class BaseFixture
{
    int baseSecret;

    public void SetBaseSecret(int value) => baseSecret = value;
}

enum WideEnum : ulong { Max = ulong.MaxValue }

class GameListFixture : List<int>;

class DerivedFixture : BaseFixture
{
    public int Visible;
    public DerivedFixture Child = null;
    public DerivedFixture Cycle = null;
    public long Big;
    public ulong BigU;
    public double Nan;
    public double Inf;
    public Dictionary<int, int> IntDict = new Dictionary<int, int>();
    public Dictionary<string, int> StrDict = new Dictionary<string, int>();
    public int[] Arr;
    public List<int> List;
    public Texture Texture = null;
    public MemoryStream Buffer = new MemoryStream();
    public Vector3 Vec = new Vector3(1f, 2f, 3f);
    public int Boom => throw new InvalidOperationException("getter must not run");
}
