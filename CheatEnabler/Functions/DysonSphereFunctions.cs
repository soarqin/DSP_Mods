using HarmonyLib;
using UXAssist.Common;

namespace CheatEnabler.Functions;

public static class DysonSphereFunctions
{
    public static void Init()
    {
        I18N.Add("You are not in any system.", "You are not in any system.", "你不在任何星系中");
        I18N.Add("There is no Dyson Sphere shell on \"{0}\".", "There is no Dyson Sphere shell on \"{0}\".", "“{0}”上没有可建造的戴森壳");
        I18N.Add("This will complete all Dyson Sphere shells on \"{0}\" instantly. Are you sure?", "This will complete all Dyson Sphere shells on \"{0}\" instantly. Are you sure?", "这将立即完成“{0}”上的所有戴森壳。你确定吗？");
    }

    public static void CompleteShellsInstantly()
    {
        StarData star = null;
        var dysonEditor = UIRoot.instance?.uiGame?.dysonEditor;
        if (dysonEditor != null && dysonEditor.gameObject.activeSelf)
        {
            star = dysonEditor.selection.viewStar;
        }
        if (star == null)
        {
            star = GameMain.data?.localStar;
            if (star == null)
            {
                UIMessageBox.Show("CheatEnabler".Translate(), "You are not in any system.".Translate(), "确定".Translate(), 3, null);
                return;
            }
        }
        var dysonSphere = GameMain.data?.dysonSpheres[star.index];
        if (dysonSphere == null || dysonSphere.layerCount == 0)
        {
            UIMessageBox.Show("CheatEnabler".Translate(), string.Format("There is no Dyson Sphere shell on \"{0}\".".Translate(), star.displayName), "确定".Translate(), 3, null);
            return;
        }

        UIMessageBox.Show("CheatEnabler".Translate(), string.Format("This will complete all Dyson Sphere shells on \"{0}\" instantly. Are you sure?".Translate(), star.displayName), "取消".Translate(), "确定".Translate(), 2, null, () =>
        {
            var totalNodeSpInfo = AccessTools.Field(typeof(DysonSphereLayer), "totalNodeSP");
            var totalFrameSpInfo = AccessTools.Field(typeof(DysonSphereLayer), "totalFrameSP");
            var totalCpInfo = AccessTools.Field(typeof(DysonSphereLayer), "totalCP");

            var rocketCount = 0L;
            var solarSailCount = 0L;
            foreach (var dysonSphereLayer in dysonSphere.layersIdBased)
            {
                if (dysonSphereLayer == null) continue;
                long totalNodeSp = 0;
                long totalFrameSp = 0;
                long totalCp = 0;
                for (var i = dysonSphereLayer.frameCursor - 1; i >= 0; i--)
                {
                    var dysonFrame = dysonSphereLayer.framePool[i];
                    if (dysonFrame == null || dysonFrame.id != i) continue;
                    totalFrameSp += dysonFrame.spMax;
                    var spMax = dysonFrame.spMax / 2;
                    if (dysonFrame.spA < spMax)
                    {
                        rocketCount += spMax - dysonFrame.spA;
                        dysonFrame.spA = spMax;
                        dysonSphere.UpdateProgress(dysonFrame);
                    }
                    if (dysonFrame.spB < spMax)
                    {
                        rocketCount += spMax - dysonFrame.spB;
                        dysonFrame.spB = spMax;
                        dysonSphere.UpdateProgress(dysonFrame);
                    }
                }
                for (var i = dysonSphereLayer.nodeCursor - 1; i >= 0; i--)
                {
                    var dysonNode = dysonSphereLayer.nodePool[i];
                    if (dysonNode == null || dysonNode.id != i) continue;
                    dysonNode.spOrdered = 0;
                    dysonNode._spReq = 0;
                    totalNodeSp += dysonNode.spMax;
                    var diff = dysonNode.spMax - dysonNode.sp;
                    if (diff > 0)
                    {
                        rocketCount += diff;
                        dysonNode.sp = dysonNode.spMax;
                        dysonSphere.UpdateProgress(dysonNode);
                    }
                    dysonNode._cpReq = 0;
                    dysonNode.cpOrdered = 0;
                    foreach (var shell in dysonNode.shells)
                    {
                        var nodeIndex = shell.nodeIndexMap[dysonNode.id];
                        var cpMax = (shell.vertsqOffset[nodeIndex + 1] - shell.vertsqOffset[nodeIndex]) * shell.cpPerVertex;
                        totalCp += cpMax;
                        diff = cpMax - shell.nodecps[nodeIndex];
                        shell.nodecps[nodeIndex] = cpMax;
                        shell.nodecps[shell.nodecps.Length - 1] += diff;
                        solarSailCount += diff;
                        if (totalCpInfo != null)
                        {
                            shell.SetMaterialDynamicVars();
                        }
                    }
                }

                totalNodeSpInfo?.SetValue(dysonSphereLayer, totalNodeSp);
                totalFrameSpInfo?.SetValue(dysonSphereLayer, totalFrameSp);
                totalCpInfo?.SetValue(dysonSphereLayer, totalCp);
            }
            dysonSphere.CheckAutoNodes();

            var productRegister = dysonSphere.productRegister;
            if (productRegister != null)
            {
                lock (productRegister)
                {
                    var count = rocketCount;
                    while (count > 0x40000000L)
                    {
                        productRegister[ProductionStatistics.DYSON_STRUCTURE_ID] += 0x40000000;
                        count -= 0x40000000;
                    }
                    if (count > 0L) productRegister[ProductionStatistics.DYSON_STRUCTURE_ID] += (int)count;
                    count = solarSailCount;
                    while (count > 0x40000000L)
                    {
                        productRegister[ProductionStatistics.SOLAR_SAIL_ID] += 0x40000000;
                        productRegister[ProductionStatistics.DYSON_CELL_ID] += 0x40000000;
                        count -= 0x40000000;
                    }
                    if (count > 0L)
                    {
                        productRegister[ProductionStatistics.SOLAR_SAIL_ID] += (int)count;
                        productRegister[ProductionStatistics.DYSON_CELL_ID] += (int)count;
                    }
                }
            }
            var consumeRegister = dysonSphere.consumeRegister;
            if (consumeRegister != null)
            {
                lock (consumeRegister)
                {
                    var count = solarSailCount;
                    while (count > 0x40000000L)
                    {
                        consumeRegister[ProductionStatistics.SOLAR_SAIL_ID] += 0x40000000;
                        count -= 0x40000000;
                    }
                    if (count > 0L) consumeRegister[ProductionStatistics.SOLAR_SAIL_ID] += (int)count;
                }
            }
        });
    }
}
