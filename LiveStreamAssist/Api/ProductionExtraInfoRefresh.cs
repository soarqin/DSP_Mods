using System;
using System.Collections.Generic;

namespace LiveStreamAssist.Api;

internal sealed class ProductionExtraInfoRefresh
{
    const int RefreshIntervalMs = 2000;
    readonly Dictionary<int, long> _nextRefreshMs = new Dictionary<int, long>();
    readonly HashSet<int> _pendingFactories = new HashSet<int>();

    public void Reset()
    {
        _nextRefreshMs.Clear();
        _pendingFactories.Clear();
    }

    public void OnRead(DataCall call, int factoryCount, bool calculating, long nowMs, Action<int> addFactory)
    {
        var factoryIndex = GetFactoryIndex(call);
        if (factoryIndex < 0 || factoryIndex >= factoryCount) return;

        if (!_nextRefreshMs.TryGetValue(factoryIndex, out var next) || nowMs >= next)
            _pendingFactories.Add(factoryIndex);

        // Let the native batch finish before requeuing factories it has already processed.
        if (calculating) return;
        foreach (var index in _pendingFactories)
        {
            if (index >= factoryCount) continue;
            addFactory(index);
            _nextRefreshMs[index] = nowMs + RefreshIntervalMs;
        }
        _pendingFactories.Clear();
    }

    static int GetFactoryIndex(DataCall call)
    {
        if (call.Method != DataMethod.Read || call.Path == null) return -1;
        var path = call.Path;
        var offset = 0;
        if (call.Root == "game")
        {
            if (path.Length == 0 || path[0].Kind != PathKind.Member || path[0].Name != "statistics")
                return -1;
            offset = 1;
        }
        else if (call.Root != "statistics")
        {
            return -1;
        }

        if (path.Length < offset + 3 ||
            path[offset].Kind != PathKind.Member || path[offset].Name != "production" ||
            path[offset + 1].Kind != PathKind.Member || path[offset + 1].Name != "factoryStatPool" ||
            path[offset + 2].Kind != PathKind.Index)
            return -1;
        return path[offset + 2].Index;
    }
}
