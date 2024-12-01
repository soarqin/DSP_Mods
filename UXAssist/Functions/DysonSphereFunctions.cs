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

    public static void InitCurrentDysonLayer(StarData star, int layerId)
    {
        star ??= CurrentStarForDysonSystem();
        if (star == null) return;
        var dysonSpheres = GameMain.data?.dysonSpheres;
        if (dysonSpheres == null) return;
        var dysonEditor = UIRoot.instance?.uiGame.dysonEditor;
        if (layerId < 0)
        {
            if (dysonSpheres[star.index] == null) return;
            var dysonSphere = new DysonSphere();
            dysonSpheres[star.index] = dysonSphere;
            dysonSphere.Init(GameMain.data, star);
            dysonSphere.ResetNew();

            if (!dysonEditor) return;
            if (dysonEditor.selection.viewStar == star)
            {
                dysonEditor.selection.viewDysonSphere = dysonSphere;
                dysonEditor.selection.NotifyDysonShpereChange();
            }
            return;
        }

        var ds = dysonSpheres[star.index];
        if (ds?.layersIdBased[layerId] == null) return;
        var pool = ds.rocketPool;
        for (var id = ds.rocketCursor - 1; id > 0; id--)
        {
            if (pool[id].id != id) continue;
            if (pool[id].nodeLayerId != layerId) continue;
            ds.RemoveDysonRocket(id);
            break;
        }
        ds.RemoveLayer(layerId);
        if (!dysonEditor) return;
        if (!dysonEditor.IsRender(layerId, false, true))
        {
            dysonEditor.SwitchRenderState(layerId, false, true);
        }
        if (!dysonEditor.IsRender(layerId, false, false))
        {
            dysonEditor.SwitchRenderState(layerId, false, false);
        }
        dysonEditor.selection.ClearAllSelection();
    }
}
