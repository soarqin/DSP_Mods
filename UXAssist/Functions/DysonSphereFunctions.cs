using System.Linq;

namespace UXAssist.Functions;

public static class DysonSphereFunctions
{
    public static StarData CurrentStarForDysonSystem()
    {
        StarData star = null;
        var dysonEditor = UIRoot.instance?.uiGame?.dysonEditor;
        if (dysonEditor != null && dysonEditor.gameObject.activeSelf)
        {
            star = dysonEditor.selection.viewStar;
        }
        return star ?? GameMain.data?.localStar;
    }

    public static void InitCurrentDysonLayer(StarData star, int index)
    {
        star ??= CurrentStarForDysonSystem();
        if (star == null) return;
        var dysonSpheres = GameMain.data?.dysonSpheres;
        if (dysonSpheres == null) return;
        var dysonEditor = UIRoot.instance?.uiGame.dysonEditor;
        if (index < 0)
        {
            if (dysonSpheres[star.index] == null) return;
            var idsToRemove = dysonSpheres[star.index].layersSorted.Select(layer => layer.id).ToArray();
            var dysonSphere = new DysonSphere();
            dysonSpheres[star.index] = dysonSphere;
            dysonSphere.Init(GameMain.data, star);
            dysonSphere.ResetNew();
            if (dysonEditor == null) return;
            foreach (var id in idsToRemove)
            {
                if (!dysonEditor.IsRender(id, false, true))
                {
                    dysonEditor.SwitchRenderState(id, false, true);
                }
                if (!dysonEditor.IsRender(id, false, false))
                {
                    dysonEditor.SwitchRenderState(id, false, false);
                }
            }
            dysonEditor.selection.ClearAllSelection();
            return;
        }

        var ds = dysonSpheres[star.index];
        if (ds?.layersIdBased[index] == null) return;
        var pool = ds.rocketPool;
        var idToRemove = -1;
        for (var id = ds.rocketCursor - 1; id > 0; id--)
        {
            if (pool[id].id != id) continue;
            if (pool[id].nodeLayerId != index) continue;
            ds.RemoveDysonRocket(id);
            idToRemove = id;
            break;
        }
        if (idToRemove < 0) return;
        ds.RemoveLayer(idToRemove);
        if (dysonEditor == null) return;
        if (!dysonEditor.IsRender(idToRemove, false, true))
        {
            dysonEditor.SwitchRenderState(idToRemove, false, true);
        }
        if (!dysonEditor.IsRender(idToRemove, false, false))
        {
            dysonEditor.SwitchRenderState(idToRemove, false, false);
        }
        dysonEditor.selection.ClearAllSelection();
    }
}
