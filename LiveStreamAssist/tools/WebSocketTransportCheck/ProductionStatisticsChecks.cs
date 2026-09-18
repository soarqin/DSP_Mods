using System.Collections.Generic;
using LiveStreamAssist.Api;

namespace WebSocketTransportCheck;

static partial class ProductionChecks
{
    static DataCall FactoryRead(int index, string member = "productIndices") => new DataCall
    {
        Method = DataMethod.Read,
        Root = "statistics",
        Path = new[] { new PathSeg("production"), new PathSeg("factoryStatPool"), new PathSeg(index), new PathSeg(member) }
    };

    static void ProductionStatisticsRefresh()
    {
        var refresh = new ProductionExtraInfoRefresh();
        var queued = new List<int>();
        var indices = FactoryRead(0);
        refresh.OnRead(indices, 3, false, 0, queued.Add);
        Check(queued.Count == 1 && queued[0] == 0,
            "product-index reads request native extra info even before an item has production history");

        refresh.OnRead(FactoryRead(0, "productPool"), 3, false, 0, queued.Add);
        refresh.OnRead(indices, 3, false, 1999, queued.Add);
        Check(queued.Count == 1, "repeated item reads share the per-factory refresh interval");
        refresh.OnRead(FactoryRead(1), 3, false, 1999, queued.Add);
        Check(queued.Count == 2 && queued[1] == 1, "new factory queries do not wait for another factory's refresh interval");

        queued.Clear();
        refresh.OnRead(indices, 3, true, 2000, queued.Add);
        refresh.OnRead(FactoryRead(2), 3, true, 2500, queued.Add);
        refresh.OnRead(FactoryRead(2), 3, true, 2501, queued.Add);
        Check(queued.Count == 0, "reads do not extend an active native calculation batch");
        refresh.OnRead(FactoryRead(1), 3, false, 3000, queued.Add);
        Check(queued.Count == 2 && new HashSet<int>(queued).SetEquals(new[] { 0, 2 }),
            "the next idle read flushes deferred factories once, including when its own cache is fresh");

        refresh.OnRead(FactoryRead(2), 3, true, 5000, queued.Add);
        refresh.Reset();
        queued.Clear();
        refresh.OnRead(indices, 3, false, 0, queued.Add);
        Check(queued.Count == 1 && queued[0] == 0, "a new session clears pending work and refresh deadlines");

        refresh.Reset();
        queued.Clear();
        var gameRead = FactoryRead(2, "productPool");
        gameRead.Root = "game";
        var gamePath = new List<PathSeg> { new PathSeg("statistics") };
        gamePath.AddRange(gameRead.Path);
        gameRead.Path = gamePath.ToArray();
        refresh.OnRead(gameRead, 3, false, 0, queued.Add);
        Check(queued.Count == 1 && queued[0] == 2, "the game.statistics alias refreshes the same native factory data");

        refresh.Reset();
        queued.Clear();
        var describe = FactoryRead(0);
        describe.Method = DataMethod.Describe;
        refresh.OnRead(describe, 3, false, 0, queued.Add);
        refresh.OnRead(FactoryRead(-1), 3, false, 0, queued.Add);
        refresh.OnRead(FactoryRead(3), 3, false, 0, queued.Add);
        refresh.OnRead(indices, 0, false, 0, queued.Add);
        foreach (var unrelated in new[]
        {
            new DataCall { Method = DataMethod.Read, Root = "statistics", Path = new[] { new PathSeg("production") } },
            new DataCall { Method = DataMethod.Read, Root = "statistics", Path = new[] { new PathSeg("production"), new PathSeg("factoryStatPool") } },
            new DataCall { Method = DataMethod.Read, Root = "history", Path = indices.Path },
            new DataCall { Method = DataMethod.Read, Root = "game", Path = indices.Path }
        })
            refresh.OnRead(unrelated, 3, false, 0, queued.Add);
        Check(queued.Count == 0, "metadata, unrelated reads, and invalid factory indices do not request calculations");
    }
}
