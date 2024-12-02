using System.Linq;

namespace LogisticHub.Module;

using UXAssist.Common;

public static class AuxData
{
    public static (long, bool)[] Fuels;
    
    public static void Init()
    {
        GameLogic.OnDataLoaded += () =>
        {
            var maxId = LDB.items.dataArray.Select(data => data.ID).Prepend(0).Max();
            Fuels = new (long, bool)[maxId + 1];
            foreach (var data in LDB.items.dataArray)
                Fuels[data.ID] = (data.HeatValue, data.Productive);
        };
    }

    public static int AlignUpToPowerOf2(int n)
    {
        if (n < 16) return 16;
        n--;
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        return n + 1;
    }
}
