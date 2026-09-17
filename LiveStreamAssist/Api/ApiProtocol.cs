using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LiveStreamAssist.Api;

internal static class ApiLimits
{
    public const string ApiVersion = "1.0.0";
    public const string Path = "/api/v1";
    public const int MaxConnections = 8;
    public const int MaxPendingRequestsPerConnection = 8;
    public const int MaxPendingRequests = 64;
    public const int MaxRequestBytes = 65536;
    public const int MaxResponseBytes = 262144;
    public const int MaxJsonDepth = 32;
    public const int MaxIdLength = 64;
    public const int MaxPathSegments = 16;
    public const int MaxSelectMembers = 32;
    public const int DefaultPageSize = 64;
    public const int MaxPageSize = 128;
    public const int RequestTimeoutMs = 5000;
    public const int MaxRequestsPerFrame = 8;
    public const int MainThreadBudgetMs = 2;
    public const int MaxIncomingSocketBytes = MaxRequestBytes + 8192;
    public const int CloseGoingAway = 1001;
    public const int CloseUnsupportedData = 1003;
    public const int CloseInvalidPayload = 1007;
    public const int ClosePolicyViolation = 1008;
    public const int CloseMessageTooBig = 1009;
    public const int CloseTryAgainLater = 1013;
}

internal sealed class ApiError
{
    public int Code;
    public string Kind;
    public string Message;
    public int? SegmentIndex;
    public string Member;
    public string Root;

    public ApiError WithPath(int? segmentIndex = null, string member = null, string root = null)
    {
        SegmentIndex = segmentIndex;
        Member = member;
        Root = root;
        return this;
    }
}

internal sealed class ApiRequest
{
    public string Id;
    public string Method;
    public JObject Params;
}

internal enum DataMethod
{
    Roots,
    Describe,
    Read
}

internal struct ReadOptions
{
    public bool HasSelect;
    public string[] Select;
    public bool HasOffset;
    public int Offset;
    public bool HasLimit;
    public int Limit;

    public int EffectiveOffset => HasOffset ? Offset : 0;
    public int EffectiveLimit => HasLimit ? Limit : ApiLimits.DefaultPageSize;
}

internal sealed class DataCall
{
    public DataMethod Method;
    public string Root;
    public PathSeg[] Path;
    public ReadOptions Options;
}

internal enum PathKind
{
    Member,
    Index,
    Key
}

internal readonly struct PathSeg
{
    public readonly PathKind Kind;
    public readonly string Name;
    public readonly int Index;
    public readonly object Key;

    public PathSeg(string name)
    {
        Kind = PathKind.Member;
        Name = name;
        Index = 0;
        Key = null;
    }

    public PathSeg(int index)
    {
        Kind = PathKind.Index;
        Name = null;
        Index = index;
        Key = null;
    }

    public PathSeg(object key)
    {
        Kind = PathKind.Key;
        Name = null;
        Index = 0;
        Key = key;
    }
}

internal static class ApiErrors
{
    public static ApiError ParseError() =>
        New(-32700, "PARSE_ERROR", "Malformed JSON.");

    public static ApiError InvalidRequest(string message = "Invalid request.") =>
        New(-32600, "INVALID_REQUEST", message);

    public static ApiError MethodNotFound() =>
        New(-32601, "METHOD_NOT_FOUND", "Unknown method.");

    public static ApiError InvalidParams(string message = "Invalid parameters.") =>
        New(-32602, "INVALID_PARAMS", message);

    public static ApiError InternalError() =>
        New(-32603, "INTERNAL_ERROR", "Unexpected server failure.");

    public static ApiError GameNotReady() =>
        New(-32001, "GAME_NOT_READY", "Game data is unavailable.");

    public static ApiError RootNotFound(string root) =>
        New(-32002, "ROOT_NOT_FOUND", "Unknown root.").WithPath(root: root);

    public static ApiError RootUnavailable(string root) =>
        New(-32003, "ROOT_UNAVAILABLE", "Root is unavailable.").WithPath(root: root);

    public static ApiError MemberNotFound(string member, int? segmentIndex = null) =>
        New(-32004, "MEMBER_NOT_FOUND", "Requested member does not exist.").WithPath(segmentIndex, member);

    public static ApiError MemberNotAllowed(string member, int? segmentIndex = null) =>
        New(-32005, "MEMBER_NOT_ALLOWED", "Member is outside the read policy.").WithPath(segmentIndex, member);

    public static ApiError NullPath(int segmentIndex) =>
        New(-32006, "NULL_PATH", "Null encountered before completing the path.").WithPath(segmentIndex);

    public static ApiError IndexOutOfRange(int segmentIndex) =>
        New(-32007, "INDEX_OUT_OF_RANGE", "Element index is outside the current array or list.").WithPath(segmentIndex);

    public static ApiError KeyNotFound(int segmentIndex) =>
        New(-32008, "KEY_NOT_FOUND", "Dictionary has no matching key.").WithPath(segmentIndex);

    public static ApiError UnsupportedType(int? segmentIndex = null, string member = null) =>
        New(-32009, "UNSUPPORTED_TYPE", "Value type or traversal is unsupported.").WithPath(segmentIndex, member);

    public static ApiError ReadFailed(int? segmentIndex = null, string member = null) =>
        New(-32010, "READ_FAILED", "A permitted read failed.").WithPath(segmentIndex, member);

    public static ApiError LimitExceeded(string message = "A documented resource limit would be exceeded.") =>
        New(-32011, "LIMIT_EXCEEDED", message);

    public static ApiError ServerBusy() =>
        New(-32012, "SERVER_BUSY", "Admission capacity is exhausted.");

    public static ApiError RequestTimeout() =>
        New(-32013, "REQUEST_TIMEOUT", "Response production exceeded its deadline.");

    public static ApiError SessionChanged() =>
        New(-32014, "SESSION_CHANGED", "Session changed after admission.");

    public static ApiError UnsupportedValue() =>
        New(-32015, "UNSUPPORTED_VALUE", "Value cannot be represented under the scalar rules.");

    static ApiError New(int code, string kind, string message) =>
        new ApiError { Code = code, Kind = kind, Message = message };
}

internal static class ApiProtocol
{
    public const string JsonRpc = "2.0";
    public const string MethodPing = "system.ping";
    public const string MethodInfo = "system.info";
    public const string MethodValidate = "system.validate";
    public const string MethodStats = "system.stats";
    public const string MethodRoots = "data.roots";
    public const string MethodDescribe = "data.describe";
    public const string MethodRead = "data.read";

    static readonly HashSet<string> SystemMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        MethodPing, MethodInfo, MethodValidate, MethodStats
    };

    static readonly HashSet<string> DataMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        MethodRoots, MethodDescribe, MethodRead
    };

    static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.None,
        DateParseHandling = DateParseHandling.None,
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.None,
        MaxDepth = ApiLimits.MaxJsonDepth
    });

    public static bool IsSystemMethod(string method) => SystemMethods.Contains(method);
    public static bool IsDataMethod(string method) => DataMethods.Contains(method);

    public static bool TryParseMessage(string text, out ApiRequest request, out ApiError error, out object id)
    {
        request = null;
        error = null;
        id = null;
        JToken token;
        try
        {
            using (var sr = new StringReader(text))
            using (var reader = new JsonTextReader(sr))
            {
                reader.DateParseHandling = DateParseHandling.None;
                reader.MaxDepth = ApiLimits.MaxJsonDepth;
                var load = new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    LineInfoHandling = LineInfoHandling.Ignore
                };
                token = JToken.Load(reader, load);
                if (reader.Read() && reader.TokenType != JsonToken.None)
                {
                    error = ApiErrors.ParseError();
                    return false;
                }
            }
        }
        catch (Exception)
        {
            error = ApiErrors.ParseError();
            return false;
        }

        if (token is JArray)
        {
            error = ApiErrors.InvalidRequest("Batch requests are not supported.");
            return false;
        }

        if (!(token is JObject obj))
        {
            error = ApiErrors.InvalidRequest();
            return false;
        }

        foreach (var prop in obj.Properties())
        {
            if (prop.Name != "jsonrpc" && prop.Name != "id" && prop.Name != "method" && prop.Name != "params")
            {
                error = ApiErrors.InvalidRequest("Unknown envelope field.");
                return false;
            }
        }

        var jsonrpc = obj["jsonrpc"];
        if (jsonrpc == null || jsonrpc.Type != JTokenType.String || !string.Equals(jsonrpc.Value<string>(), JsonRpc, StringComparison.Ordinal))
        {
            error = ApiErrors.InvalidRequest("jsonrpc must be \"2.0\".");
            return false;
        }

        if (!obj.TryGetValue("id", out var idToken) || idToken == null || idToken.Type == JTokenType.Null)
        {
            error = ApiErrors.InvalidRequest("id is required.");
            return false;
        }

        if (idToken.Type != JTokenType.String)
        {
            error = ApiErrors.InvalidRequest("id must be a string.");
            return false;
        }

        var idValue = idToken.Value<string>();
        if (string.IsNullOrEmpty(idValue))
        {
            error = ApiErrors.InvalidRequest("id must be nonempty.");
            return false;
        }

        if (idValue.Length > ApiLimits.MaxIdLength)
        {
            error = ApiErrors.InvalidRequest("id exceeds maxIdLength.");
            return false;
        }

        id = idValue;

        if (!obj.TryGetValue("method", out var methodToken) || methodToken == null || methodToken.Type != JTokenType.String)
        {
            error = ApiErrors.InvalidRequest("method is required.");
            return false;
        }

        var method = methodToken.Value<string>();
        if (string.IsNullOrEmpty(method))
        {
            error = ApiErrors.InvalidRequest("method is required.");
            return false;
        }

        JObject parms;
        if (!obj.TryGetValue("params", out var paramsToken) || paramsToken == null || paramsToken.Type == JTokenType.Null)
            parms = new JObject();
        else if (paramsToken is JObject p)
            parms = p;
        else
        {
            error = ApiErrors.InvalidParams("params must be an object.");
            return false;
        }

        request = new ApiRequest { Id = idValue, Method = method, Params = parms };
        return true;
    }

    public static bool TryParseDataCall(ApiRequest request, out DataCall call, out ApiError error)
    {
        call = null;
        error = null;
        switch (request.Method)
        {
            case MethodRoots:
                error = RejectUnknownParams(request.Params);
                if (error != null) return false;
                call = new DataCall { Method = DataMethod.Roots, Path = Array.Empty<PathSeg>() };
                return true;
            case MethodDescribe:
                return TryParseDescribe(request.Params, out call, out error);
            case MethodRead:
                return TryParseRead(request.Params, out call, out error);
            default:
                error = ApiErrors.MethodNotFound();
                return false;
        }
    }

    public static object HandleSystem(string method, JObject parms, GameVersionSnapshot version, string modVersion)
    {
        switch (method)
        {
            case MethodPing:
            {
                var err = RejectUnknownParams(parms);
                if (err != null) return err;
                return new Dictionary<string, object>
                {
                    ["serverTimeUtc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                };
            }
            case MethodInfo:
            {
                var err = RejectUnknownParams(parms);
                if (err != null) return err;
                return BuildInfo(version, modVersion);
            }
            case MethodValidate:
                return HandleValidate(parms);
            case MethodStats:
            {
                var err = RejectUnknownParams(parms);
                if (err != null) return err;
                return new Dictionary<string, object>
                {
                    ["available"] = false,
                    ["metrics"] = null
                };
            }
            default:
                return ApiErrors.MethodNotFound();
        }
    }

    public static Dictionary<string, object> BuildInfo(GameVersionSnapshot version, string modVersion)
    {
        var ready = version != null && version.Ready;
        return new Dictionary<string, object>
        {
            ["apiVersion"] = ApiLimits.ApiVersion,
            ["modVersion"] = modVersion ?? "",
            ["gameVersionReady"] = ready,
            ["gameVersion"] = ready ? version.GameVersion : null,
            ["gameBuild"] = ready ? (object)version.GameBuild : null,
            ["capabilities"] = new Dictionary<string, object>
            {
                ["reflection.read"] = true,
                ["reflection.describe"] = true,
                ["api.stats"] = false,
                ["subscriptions"] = false
            },
            ["limits"] = new Dictionary<string, object>
            {
                ["maxConnections"] = ApiLimits.MaxConnections,
                ["maxPendingRequestsPerConnection"] = ApiLimits.MaxPendingRequestsPerConnection,
                ["maxPendingRequests"] = ApiLimits.MaxPendingRequests,
                ["maxRequestBytes"] = ApiLimits.MaxRequestBytes,
                ["maxResponseBytes"] = ApiLimits.MaxResponseBytes,
                ["maxJsonDepth"] = ApiLimits.MaxJsonDepth,
                ["maxIdLength"] = ApiLimits.MaxIdLength,
                ["maxPathSegments"] = ApiLimits.MaxPathSegments,
                ["maxSelectMembers"] = ApiLimits.MaxSelectMembers,
                ["defaultPageSize"] = ApiLimits.DefaultPageSize,
                ["maxPageSize"] = ApiLimits.MaxPageSize,
                ["requestTimeoutMs"] = ApiLimits.RequestTimeoutMs,
                ["maxRequestsPerFrame"] = ApiLimits.MaxRequestsPerFrame,
                ["mainThreadBudgetMs"] = ApiLimits.MainThreadBudgetMs
            }
        };
    }

    public static bool CapabilityEnabled(string name)
    {
        switch (name)
        {
            case "reflection.read":
            case "reflection.describe":
                return true;
            default:
                return false;
        }
    }

    public static byte[] SerializeEnvelope(object id, object result, ApiError error)
    {
        var envelope = new Dictionary<string, object>
        {
            ["jsonrpc"] = JsonRpc,
            ["id"] = id
        };
        if (error != null)
            envelope["error"] = ErrorBody(error);
        else
            envelope["result"] = result;
        return SerializeBounded(envelope);
    }

    public static Dictionary<string, object> ErrorBody(ApiError error)
    {
        var data = new Dictionary<string, object> { ["kind"] = error.Kind };
        if (error.SegmentIndex.HasValue) data["segmentIndex"] = error.SegmentIndex.Value;
        if (error.Member != null) data["member"] = error.Member;
        if (error.Root != null) data["root"] = error.Root;
        return new Dictionary<string, object>
        {
            ["code"] = error.Code,
            ["message"] = error.Message,
            ["data"] = data
        };
    }

    public static byte[] SerializeBounded(object value)
    {
        using (var bounded = new BoundedStream(ApiLimits.MaxResponseBytes))
        using (var writer = new StreamWriter(bounded, new UTF8Encoding(false), 1024, true))
        using (var json = new JsonTextWriter(writer))
        {
            json.Formatting = Formatting.None;
            Serializer.Serialize(json, value);
            json.Flush();
            writer.Flush();
            return bounded.ToArray();
        }
    }

    public static ApiError RejectUnknownParams(JObject parms, params string[] allowed)
    {
        if (parms == null) return null;
        foreach (var prop in parms.Properties())
        {
            var known = false;
            for (var i = 0; i < allowed.Length; i++)
            {
                if (string.Equals(prop.Name, allowed[i], StringComparison.Ordinal))
                {
                    known = true;
                    break;
                }
            }

            if (!known)
                return ApiErrors.InvalidParams("Unknown parameter '" + prop.Name + "'.");
        }

        return null;
    }

    static object HandleValidate(JObject parms)
    {
        var err = RejectUnknownParams(parms, "apiMajor", "requiredCapabilities");
        if (err != null) return err;
        if (!parms.TryGetValue("apiMajor", out var majorToken) || majorToken == null || majorToken.Type == JTokenType.Null)
            return ApiErrors.InvalidParams("apiMajor is required.");
        if (!TryGetInt32(majorToken, out var apiMajor) || apiMajor < 1)
            return ApiErrors.InvalidParams("apiMajor must be a positive 32-bit integer.");

        var missing = new List<string>();
        if (parms.TryGetValue("requiredCapabilities", out var capToken) && capToken != null && capToken.Type != JTokenType.Null)
        {
            if (!(capToken is JArray arr))
                return ApiErrors.InvalidParams("requiredCapabilities must be an array.");
            if (arr.Count > 32)
                return ApiErrors.LimitExceeded("requiredCapabilities exceeds 32 entries.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in arr)
            {
                if (item == null || item.Type != JTokenType.String)
                    return ApiErrors.InvalidParams("Capability names must be nonempty strings.");
                var name = item.Value<string>();
                if (string.IsNullOrEmpty(name))
                    return ApiErrors.InvalidParams("Capability names must be nonempty strings.");
                if (!seen.Add(name))
                    return ApiErrors.InvalidParams("requiredCapabilities must be distinct.");
                if (!CapabilityEnabled(name))
                    missing.Add(name);
            }
        }

        var majorOk = apiMajor == 1;
        return new Dictionary<string, object>
        {
            ["compatible"] = majorOk && missing.Count == 0,
            ["apiVersion"] = ApiLimits.ApiVersion,
            ["apiMajorCompatible"] = majorOk,
            ["missingCapabilities"] = missing
        };
    }

    static bool TryParseDescribe(JObject parms, out DataCall call, out ApiError error)
    {
        call = null;
        error = RejectUnknownParams(parms, "root", "path");
        if (error != null) return false;
        if (!TryParseRoot(parms, out var root, out error)) return false;
        if (!TryParsePath(parms, out var path, out error)) return false;
        call = new DataCall { Method = DataMethod.Describe, Root = root, Path = path };
        return true;
    }

    static bool TryParseRead(JObject parms, out DataCall call, out ApiError error)
    {
        call = null;
        error = RejectUnknownParams(parms, "root", "path", "select", "offset", "limit");
        if (error != null) return false;
        if (!TryParseRoot(parms, out var root, out error)) return false;
        if (!TryParsePath(parms, out var path, out error)) return false;
        var options = new ReadOptions();
        if (parms.TryGetValue("select", out var selectToken) && selectToken != null && selectToken.Type != JTokenType.Null)
        {
            if (!(selectToken is JArray arr))
            {
                error = ApiErrors.InvalidParams("select must be an array of member names.");
                return false;
            }

            if (arr.Count < 1)
            {
                error = ApiErrors.InvalidParams("select must contain at least one member.");
                return false;
            }

            if (arr.Count > ApiLimits.MaxSelectMembers)
            {
                error = ApiErrors.LimitExceeded("select exceeds maxSelectMembers.");
                return false;
            }

            var names = new string[arr.Count];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < arr.Count; i++)
            {
                var item = arr[i];
                if (item == null || item.Type != JTokenType.String)
                {
                    error = ApiErrors.InvalidParams("select members must be nonempty strings.");
                    return false;
                }

                var name = item.Value<string>();
                if (string.IsNullOrEmpty(name))
                {
                    error = ApiErrors.InvalidParams("select members must be nonempty strings.");
                    return false;
                }

                if (!seen.Add(name))
                {
                    error = ApiErrors.InvalidParams("select members must be distinct.");
                    return false;
                }

                names[i] = name;
            }

            options.HasSelect = true;
            options.Select = names;
        }

        if (parms.TryGetValue("offset", out var offsetToken) && offsetToken != null && offsetToken.Type != JTokenType.Null)
        {
            if (!TryGetInt32(offsetToken, out var offset) || offset < 0)
            {
                error = ApiErrors.InvalidParams("offset must be a nonnegative 32-bit integer.");
                return false;
            }

            options.HasOffset = true;
            options.Offset = offset;
        }

        if (parms.TryGetValue("limit", out var limitToken) && limitToken != null && limitToken.Type != JTokenType.Null)
        {
            if (!TryGetInt32(limitToken, out var limit))
            {
                error = ApiErrors.InvalidParams("limit must be an integer.");
                return false;
            }

            if (limit < 1)
            {
                error = ApiErrors.InvalidParams("limit must be at least 1.");
                return false;
            }

            if (limit > ApiLimits.MaxPageSize)
            {
                error = ApiErrors.LimitExceeded("limit exceeds maxPageSize.");
                return false;
            }

            options.HasLimit = true;
            options.Limit = limit;
        }

        call = new DataCall { Method = DataMethod.Read, Root = root, Path = path, Options = options };
        return true;
    }

    static bool TryParseRoot(JObject parms, out string root, out ApiError error)
    {
        root = null;
        error = null;
        if (!parms.TryGetValue("root", out var token) || token == null || token.Type == JTokenType.Null)
        {
            error = ApiErrors.InvalidParams("root is required.");
            return false;
        }

        if (token.Type != JTokenType.String)
        {
            error = ApiErrors.InvalidParams("root must be a string.");
            return false;
        }

        root = token.Value<string>();
        if (string.IsNullOrEmpty(root))
        {
            error = ApiErrors.InvalidParams("root must be a nonempty string.");
            return false;
        }

        return true;
    }

    static bool TryParsePath(JObject parms, out PathSeg[] path, out ApiError error)
    {
        path = Array.Empty<PathSeg>();
        error = null;
        if (!parms.TryGetValue("path", out var token) || token == null || token.Type == JTokenType.Null)
            return true;
        if (!(token is JArray arr))
        {
            error = ApiErrors.InvalidParams("path must be an array.");
            return false;
        }

        if (arr.Count > ApiLimits.MaxPathSegments)
        {
            error = ApiErrors.LimitExceeded("path exceeds maxPathSegments.");
            return false;
        }

        var segs = new PathSeg[arr.Count];
        for (var i = 0; i < arr.Count; i++)
        {
            if (!TryParseSegment(arr[i], out segs[i], out error))
                return false;
        }

        path = segs;
        return true;
    }

    static bool TryParseSegment(JToken token, out PathSeg seg, out ApiError error)
    {
        seg = default;
        error = null;
        if (token == null || token.Type == JTokenType.Null)
        {
            error = ApiErrors.InvalidParams("path segments cannot be null.");
            return false;
        }

        if (token.Type == JTokenType.String)
        {
            var name = token.Value<string>();
            if (string.IsNullOrEmpty(name))
            {
                error = ApiErrors.InvalidParams("member names must be nonempty.");
                return false;
            }

            seg = new PathSeg(name);
            return true;
        }

        if (token.Type == JTokenType.Integer)
        {
            if (!TryGetInt32(token, out var index) || index < 0)
            {
                error = ApiErrors.InvalidParams("indexes must be nonnegative 32-bit integers.");
                return false;
            }

            seg = new PathSeg(index);
            return true;
        }

        if (token is JObject obj)
        {
            JProperty keyProp = null;
            foreach (var prop in obj.Properties())
            {
                if (!string.Equals(prop.Name, "key", StringComparison.Ordinal))
                {
                    error = ApiErrors.InvalidParams("key segments may only contain 'key'.");
                    return false;
                }

                keyProp = prop;
            }

            if (keyProp == null || keyProp.Value == null || keyProp.Value.Type == JTokenType.Null)
            {
                error = ApiErrors.InvalidParams("key is required.");
                return false;
            }

            if (keyProp.Value.Type == JTokenType.String)
            {
                seg = new PathSeg((object)keyProp.Value.Value<string>());
                return true;
            }

            if (keyProp.Value.Type == JTokenType.Integer && TryGetInt32(keyProp.Value, out var key))
            {
                seg = new PathSeg((object)key);
                return true;
            }

            error = ApiErrors.InvalidParams("dictionary keys must be strings or 32-bit integers.");
            return false;
        }

        error = ApiErrors.InvalidParams("unsupported path segment.");
        return false;
    }

    public static bool TryGetInt32(JToken token, out int value)
    {
        value = 0;
        if (token == null || token.Type != JTokenType.Integer) return false;
        try
        {
            var n = token.Value<long>();
            if (n < int.MinValue || n > int.MaxValue) return false;
            value = (int)n;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static int ApiMajor => 1;
}

internal sealed class BoundedStream : MemoryStream
{
    readonly int _max;

    public BoundedStream(int max) => _max = max;

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (Length + count > _max)
            throw new ResponseTooLargeException();
        base.Write(buffer, offset, count);
    }

    public override void WriteByte(byte value)
    {
        if (Length + 1 > _max)
            throw new ResponseTooLargeException();
        base.WriteByte(value);
    }
}

internal sealed class ResponseTooLargeException : Exception;
internal sealed class GameVersionSnapshot
{
    public readonly bool Ready;
    public readonly string GameVersion;
    public readonly int GameBuild;

    public GameVersionSnapshot(bool ready, string gameVersion, int gameBuild)
    {
        Ready = ready;
        GameVersion = gameVersion;
        GameBuild = gameBuild;
    }

    public static readonly GameVersionSnapshot Empty = new GameVersionSnapshot(false, null, 0);
}

internal sealed class SessionSnapshot
{
    public readonly int Generation;
    public readonly string SessionId;

    public SessionSnapshot(int generation, string sessionId)
    {
        Generation = generation;
        SessionId = sessionId;
    }
}
