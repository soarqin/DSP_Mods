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
            UIMessageBox.Show(Localization.CheatEnabler.Translate(), Localization.YouAreNotInAnySystem.Translate(), Localization.OK.Translate(), UIMessageBox.ERROR, null);
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
            UIMessageBox.Show(Localization.CheatEnabler.Translate(), string.Format(Localization.ThereIsNoDysonSphereDataOn0.Translate(), star.displayName), Localization.OK.Translate(), UIMessageBox.ERROR, null);
            return null;
        }
        if (requireLayer && sphere.layerCount == 0)
        {
            UIMessageBox.Show(Localization.CheatEnabler.Translate(), string.Format(Localization.ThereIsNoDysonSphereShellOn0.Translate(), star.displayName), Localization.OK.Translate(), UIMessageBox.ERROR, null);
            return null;
        }
        return (sphere, star);
    }
}
