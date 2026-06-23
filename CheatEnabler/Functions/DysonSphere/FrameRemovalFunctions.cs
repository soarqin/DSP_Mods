using System;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.GameConstants;
using UXAssist.Common.ModFeatures;
using CheatEnabler;

namespace CheatEnabler.Functions.DysonSphere;

[ModFeature("FrameRemovalFunctions")]
public static class FrameRemovalFunctions
{
    public static void RemoveAllFrames()
    {
        var resolved = DysonSphereResolver.ResolveEditorOrLocalSphere();
        if (resolved == null) return;
        var (dysonSphere, star) = resolved.Value;

        UIMessageBox.Show("CheatEnabler".Translate(), string.Format("This will remove all frames on \"{0}\". Are you sure?".Translate(), star.displayName), Localization.Cancel.Translate(), Localization.Ok.Translate(), UIMessageBox.QUESTION, null, () =>
        {
            var totalFrameSpInfo = AccessTools.Field(typeof(DysonSphereLayer), "totalFrameSP");

            foreach (var dysonSphereLayer in dysonSphere.layersIdBased)
            {
                if (dysonSphereLayer == null) continue;
                for (var i = dysonSphereLayer.nodeCursor - 1; i >= 0; i--)
                {
                    var dysonNode = dysonSphereLayer.nodePool[i];
                    if (dysonNode == null || dysonNode.id != i) continue;
                    if (dysonNode.frames.Count > 0)
                        dysonNode.frames.Clear();
                    dysonNode.RecalcSpReq();
                }
                for (var i = dysonSphereLayer.shellCursor - 1; i >= 0; i--)
                {
                    var dysonShell = dysonSphereLayer.shellPool[i];
                    if (dysonShell == null || dysonShell.id != i) continue;
                    if (dysonShell.frames.Count > 0)
                        dysonShell.frames.Clear();
                }
                for (var i = dysonSphereLayer.frameCursor - 1; i >= 0; i--)
                {
                    var dysonFrame = dysonSphereLayer.framePool[i];
                    if (dysonFrame == null || dysonFrame.id != i) continue;
                    dysonFrame.Free();
                    dysonSphereLayer.framePool[i] = null;
                }
                dysonSphereLayer.frameCursor = 1;
                dysonSphereLayer.frameRecycleCursor = 0;
                dysonSphereLayer.SetFrameCapacity(DysonSphereConstants.DefaultLayerPoolCapacity);
                totalFrameSpInfo?.SetValue(dysonSphereLayer, 0);
            }
            dysonSphere.CheckAutoNodes();
            dysonSphere.PickAutoNode();
            dysonSphere.modelRenderer.RebuildModels();
            dysonSphere.needRecalculatePower = true;
        });
    }
}
