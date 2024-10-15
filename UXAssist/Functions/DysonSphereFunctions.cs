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
        if (index < 0)
        {
            if (dysonSpheres[star.index] == null) return;
            var dysonSphere = new DysonSphere();
            dysonSpheres[star.index] = dysonSphere;
            dysonSphere.Init(GameMain.data, star);
            dysonSphere.ResetNew();
            return;
        }

        var ds = dysonSpheres[star.index];
        if (ds?.layersIdBased[index] == null) return;
        var pool = ds.rocketPool;
        for (var id = ds.rocketCursor - 1; id > 0; id--)
        {
            if (pool[id].id != id) continue;
            if (pool[id].nodeLayerId != index) continue;
            ds.RemoveDysonRocket(id);
        }

        ds.RemoveLayer(index);
    }
}
