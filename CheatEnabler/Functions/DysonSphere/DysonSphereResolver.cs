using System;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;
using CheatEnabler;

namespace CheatEnabler.Functions.DysonSphere;

[ModFeature("DysonSphereResolver")]
public static class DysonSphereResolver
{
    public static (global::DysonSphere sphere, StarData star)? ResolveCurrent()
    {
        var star = GameMain.localStar;
        if (star == null) return null;
        var sphere = GameMain.data.dysonSpheres[star.index];
        if (sphere == null) return null;
        return (sphere, star);
    }

    public static StarData ResolveEditorOrLocalStar()
    {
        var dysonEditor = UIRoot.instance?.uiGame?.dysonEditor;
        if (dysonEditor != null && dysonEditor.gameObject.activeSelf)
        {
            return dysonEditor.selection.viewStar;
        }
        var star = GameMain.localStar;
        if (star == null)
        {
            UIMessageBox.Show("CheatEnabler".Translate(), "You are not in any system.".Translate(), Localization.Ok.Translate(), UIMessageBox.ERROR, null);
        }
        return star;
    }

    public static (global::DysonSphere sphere, StarData star)? ResolveEditorOrLocalSphere(bool requireLayer = true)
    {
        var star = ResolveEditorOrLocalStar();
        if (star == null) return null;
        var sphere = GameMain.data?.dysonSpheres[star.index];
        if (sphere == null)
        {
            UIMessageBox.Show("CheatEnabler".Translate(), string.Format("There is no Dyson Sphere data on \"{0}\".".Translate(), star.displayName), Localization.Ok.Translate(), UIMessageBox.ERROR, null);
            return null;
        }
        if (requireLayer && sphere.layerCount == 0)
        {
            UIMessageBox.Show("CheatEnabler".Translate(), string.Format("There is no Dyson Sphere shell on \"{0}\".".Translate(), star.displayName), Localization.Ok.Translate(), UIMessageBox.ERROR, null);
            return null;
        }
        return (sphere, star);
    }
}
