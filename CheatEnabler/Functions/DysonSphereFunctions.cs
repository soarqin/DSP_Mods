using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;

namespace CheatEnabler.Functions;

public static class DysonSphereFunctions
{
    public static ConfigEntry<bool> IllegalDysonShellFunctionsEnabled;
    public static ConfigEntry<int> ShellsCountForFunctions;

    public static void Init()
    {
        I18N.Add("You are not in any system.", "You are not in any system.", "你不在任何星系中");
        I18N.Add("There is no Dyson Sphere shell on \"{0}\".", "There is no Dyson Sphere shell on \"{0}\".", "“{0}”上没有可建造的戴森壳");
        I18N.Add("There is no Dyson Sphere data on \"{0}\".", "There is no Dyson Sphere data on \"{0}\".", "“{0}”上没有戴森球数据");
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
                UIMessageBox.Show("CheatEnabler".Translate(), "You are not in any system.".Translate(), "确定".Translate(), UIMessageBox.ERROR, null);
                return;
            }
        }
        var dysonSphere = GameMain.data?.dysonSpheres[star.index];
        if (dysonSphere == null || dysonSphere.layerCount == 0)
        {
            UIMessageBox.Show("CheatEnabler".Translate(), string.Format("There is no Dyson Sphere shell on \"{0}\".".Translate(), star.displayName), "确定".Translate(), UIMessageBox.ERROR, null);
            return;
        }

        UIMessageBox.Show("CheatEnabler".Translate(), string.Format("This will complete all Dyson Sphere shells on \"{0}\" instantly. Are you sure?".Translate(), star.displayName), "取消".Translate(), "确定".Translate(), UIMessageBox.QUESTION, null, () =>
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

    private static DysonNode QuickAddDysonNode(this DysonSphereLayer layer, int protoId, Vector3 pos)
    {
        int nodeId;
        if (layer.nodeRecycleCursor > 0)
        {
            int[] array = layer.nodeRecycle;
            int num = layer.nodeRecycleCursor - 1;
            layer.nodeRecycleCursor = num;
            nodeId = array[num];
        }
        else
        {
            int nodePoolIndex = layer.nodeCursor;
            layer.nodeCursor = nodePoolIndex + 1;
            nodeId = nodePoolIndex;
            if (nodeId == layer.nodeCapacity)
            {
                layer.SetNodeCapacity(layer.nodeCapacity * 2);
            }
        }
        DysonNode node = null;
        if (layer.nodePool[nodeId] == null)
        {
            node = new DysonNode();
            layer.nodePool[nodeId] = node;
        }
        else
        {
            node = layer.nodePool[nodeId];
            node.SetEmpty();
        }
        node.id = nodeId;
        node.protoId = protoId;
        node.layerId = layer.id;
        node.pos = pos;
        node.reserved = false;
        node.sp = 0;
        node.spMax = DysonNode.kSpPerNode;
        layer.dysonSphere.AddDysonNodeRData(node, true);
        return node;
    }

    private static DysonFrame QuickAddDysonFrame(this DysonSphereLayer layer, int protoId, DysonNode nodeA, DysonNode nodeB, bool euler)
    {
        int newId;
        if (layer.frameRecycleCursor > 0)
        {
            var array = layer.frameRecycle;
            var index = layer.frameRecycleCursor - 1;
            layer.frameRecycleCursor = index;
            newId = array[index];
        }
        else
        {
            var index = layer.frameCursor;
            layer.frameCursor = index + 1;
            newId = index;
            if (newId == layer.frameCapacity)
            {
                layer.SetFrameCapacity(layer.frameCapacity * 2);
            }
        }
        DysonFrame frame = layer.framePool[newId];
        if (frame == null)
        {
            frame = new DysonFrame();
            layer.framePool[newId] = frame;
        }
        else
        {
            frame = layer.framePool[newId];
            frame.SetEmpty();
        }
        frame.id = newId;
        frame.layerId = layer.id;
        frame.protoId = protoId + DysonSphereSegmentRenderer.nodeProtoCount;
        frame.reserved = false;
        frame.nodeA = nodeA;
        frame.nodeB = nodeB;
        frame.euler = euler;
        frame.spA = 0;
        frame.spB = 0;
        frame.spMax = frame.segCount * DysonFrame.kSpPerSeg;
        nodeA.frames.Add(frame);
        nodeB.frames.Add(frame);
        return frame;
    }

    private static readonly ThreadLocal<Dictionary<int, Vector3>> _vmap = new(() => new(16384));
    private static int CalculateTriangleVertCount(VectorLF3[] polygon)
    {
        if (polygon.Length != 3) return -1;
        VectorLF3 sum = VectorLF3.zero;
        double num = 0.0;
        for (int i = 0; i < 3; i++)
        {
            var nodeApos = polygon[i];
            var nodeBpos = polygon[(i + 1) % 3];
            float num2 = Vector3.Distance(nodeApos, nodeBpos);
            VectorLF3 vectorLF2 = (VectorLF3)(nodeApos + nodeBpos) * 0.5;
            sum += vectorLF2 * (double)num2;
            num += (double)num2;
        }
        var radius = polygon[0].magnitude;
        radius = Math.Round(radius * 10.0) / 10.0;
        for (int j = 0; j < 3; j++)
        {
            polygon[j] = polygon[j].normalized * radius;
        }
        var center = (sum / num).normalized * radius;
        float num3 = 0f;
        for (int k = 0; k < 3; k++)
        {
            float num4 = Vector3.Distance(center, polygon[k]);
            if (num4 > num3)
            {
                num3 = num4;
            }
        }
        var gridScale = (int)(Math.Pow(radius / 4000.0, 0.75) + 0.5);
        gridScale = ((gridScale < 1) ? 1 : gridScale);
        var gridSize = (float)gridScale * 80f;
        var cpPerVertex = gridScale * gridScale * 2;

        var num5 = (int)((double)num3 / 0.8660254037844386 / (double)gridSize + 2.5);
        var xaxis = VectorLF3.Cross(center, Vector3.up).normalized;
        if (xaxis.magnitude < 0.1)
        {
            xaxis = new VectorLF3(0f, 0f, 1f);
        }
        var yaxis = VectorLF3.Cross(xaxis, center).normalized;
        var raydir = xaxis * 0.915662593339561 + yaxis * 0.40194777665596015;
        var w1axis = xaxis * (0.5 * (double)gridSize) - yaxis * (0.8660254037844386 * (double)gridSize);
        var w2axis = xaxis * (0.5 * (double)gridSize) + yaxis * (0.8660254037844386 * (double)gridSize);
        var w0axis = xaxis * (double)gridSize;
        double num6 = 0.5773502691896258;
        var t1axis = yaxis * ((double)gridSize * num6 * 0.5) - xaxis * ((double)gridSize * 0.5);
        var t2axis = yaxis * ((double)gridSize * num6 * 0.5) + xaxis * ((double)gridSize * 0.5);
        var t0axis = yaxis * ((double)gridSize / 0.8660254037844386 * 0.5);
        var polyn = new VectorLF3[3];
        var polynu = new double[3];
        for (int l = 0; l < 3; l++)
        {
            Vector3 vector = polygon[l];
            Vector3 vector2 = polygon[(l + 1) % 3];
            polyn[l] = VectorLF3.Cross(vector, vector2).normalized;
            polynu[l] = polyn[l].x * raydir.x + polyn[l].y * raydir.y + polyn[l].z * raydir.z;
        }
        var vmap = _vmap.Value;
        vmap.Clear();
        double num7 = (double)gridSize * 0.5;
        for (int m = -num5; m <= num5; m++)
        {
            for (int n = -num5; n <= num5; n++)
            {
                if (m - n <= num5 && m - n >= -num5)
                {
                    VectorLF3 vectorLF3;
                    vectorLF3.x = center.x + w0axis.x * (double)m - w1axis.x * (double)n;
                    vectorLF3.y = center.y + w0axis.y * (double)m - w1axis.y * (double)n;
                    vectorLF3.z = center.z + w0axis.z * (double)m - w1axis.z * (double)n;
                    double num8 = radius / Math.Sqrt(vectorLF3.x * vectorLF3.x + vectorLF3.y * vectorLF3.y + vectorLF3.z * vectorLF3.z);
                    vectorLF3.x *= num8;
                    vectorLF3.y *= num8;
                    vectorLF3.z *= num8;
                    int num9 = 0;
                    for (int num10 = 0; num10 < 3; num10++)
                    {
                        double num11 = -(polyn[num10].x * vectorLF3.x + polyn[num10].y * vectorLF3.y + polyn[num10].z * vectorLF3.z) / polynu[num10];
                        if (num11 >= 0.0)
                        {
                            VectorLF3 normalized2 = new VectorLF3(vectorLF3.x + num11 * raydir.x, vectorLF3.y + num11 * raydir.y, vectorLF3.z + num11 * raydir.z).normalized;
                            normalized2.x *= radius;
                            normalized2.y *= radius;
                            normalized2.z *= radius;
                            VectorLF3 vectorLF4 = polygon[num10] - normalized2;
                            VectorLF3 vectorLF5 = polygon[(num10 + 1) % 3] - normalized2;
                            double num12 = vectorLF4.x * vectorLF5.x + vectorLF4.y * vectorLF5.y + vectorLF4.z * vectorLF5.z;
                            if (num12 < 0.0 || (num12 == 0.0 && vectorLF4.x == 0.0 && vectorLF4.y == 0.0 && vectorLF4.z == 0.0))
                            {
                                num9++;
                            }
                        }
                    }
                    if ((num9 & 1) == 1)
                    {
                        int num13 = DysonShell._get_key(m, n);
                        vmap[num13] = vectorLF3;
                    }
                    else
                    {
                        for (int num14 = 0; num14 < 3; num14++)
                        {
                            VectorLF3 vectorLF6 = polygon[num14];
                            VectorLF3 vectorLF7 = polyn[num14];
                            VectorLF3 vectorLF8 = vectorLF3 - vectorLF6;
                            double num15 = vectorLF7.x * vectorLF8.x + vectorLF7.y * vectorLF8.y + vectorLF7.z * vectorLF8.z;
                            double num16 = Math.Abs(num15);
                            if (num16 <= num7)
                            {
                                VectorLF3 vectorLF9 = polygon[(num14 + 1) % 3];
                                VectorLF3 vectorLF10 = vectorLF3 - vectorLF7 * num15;
                                VectorLF3 vectorLF11 = vectorLF9 - vectorLF6;
                                double magnitude = vectorLF11.magnitude;
                                VectorLF3 vectorLF12 = vectorLF11 / magnitude;
                                VectorLF3 vectorLF13 = vectorLF10 - vectorLF6;
                                double num17 = vectorLF12.x * vectorLF13.x + vectorLF12.y * vectorLF13.y + vectorLF12.z * vectorLF13.z;
                                double num18;
                                if (num17 < 0.0)
                                {
                                    num18 = vectorLF8.magnitude;
                                }
                                else if (num17 > magnitude)
                                {
                                    num18 = (vectorLF3 - vectorLF9).magnitude;
                                }
                                else
                                {
                                    num18 = num16;
                                }
                                if (num18 <= num7)
                                {
                                    int num19 = DysonShell._get_key(m, n);
                                    vmap[num19] = vectorLF3;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        return vmap.Count;
    }

    private static bool MyGenerateGeometry(this DysonShell shell)
    {
        VectorLF3 sum = VectorLF3.zero;
        double num = 0.0;
        for (int i = 0; i < shell.frames.Count; i++)
        {
            float num2 = Vector3.Distance(shell.frames[i].nodeA.pos, shell.frames[i].nodeB.pos);
            VectorLF3 vectorLF2 = (VectorLF3)(shell.frames[i].nodeA.pos + shell.frames[i].nodeB.pos) * 0.5;
            sum += vectorLF2 * (double)num2;
            num += (double)num2;
        }
        shell.radius = shell.polygon[0].magnitude;
        shell.radius = Math.Round(shell.radius * 10.0) / 10.0;
        for (int j = 0; j < shell.polygon.Count; j++)
        {
            shell.polygon[j] = shell.polygon[j].normalized * shell.radius;
        }
        var normalized = (sum / num).normalized;
        shell.center = normalized * shell.radius;
        float num3 = 0f;
        for (int k = 0; k < shell.polygon.Count; k++)
        {
            float num4 = Vector3.Distance(shell.center, shell.polygon[k]);
            if (num4 > num3)
            {
                num3 = num4;
            }
        }
        shell.gridScale = (int)(Math.Pow(shell.radius / 4000.0, 0.75) + 0.5);
        shell.gridScale = ((shell.gridScale < 1) ? 1 : shell.gridScale);
        shell.gridSize = (float)shell.gridScale * 80f;
        shell.cpPerVertex = shell.gridScale * shell.gridScale * 2;
        int num5 = (int)((double)num3 / 0.8660254037844386 / (double)shell.gridSize + 2.5);
        shell.xaxis = VectorLF3.Cross(normalized, Vector3.up).normalized;
        if (shell.xaxis.magnitude < 0.1)
        {
            shell.xaxis = new VectorLF3(0f, 0f, 1f);
        }
        shell.yaxis = VectorLF3.Cross(shell.xaxis, normalized).normalized;
        shell.raydir = shell.xaxis * 0.915662593339561 + shell.yaxis * 0.40194777665596015;
        shell.w1axis = shell.xaxis * (0.5 * (double)shell.gridSize) - shell.yaxis * (0.8660254037844386 * (double)shell.gridSize);
        shell.w2axis = shell.xaxis * (0.5 * (double)shell.gridSize) + shell.yaxis * (0.8660254037844386 * (double)shell.gridSize);
        shell.w0axis = shell.xaxis * (double)shell.gridSize;
        double num6 = 0.5773502691896258;
        shell.t1axis = shell.yaxis * ((double)shell.gridSize * num6 * 0.5) - shell.xaxis * ((double)shell.gridSize * 0.5);
        shell.t2axis = shell.yaxis * ((double)shell.gridSize * num6 * 0.5) + shell.xaxis * ((double)shell.gridSize * 0.5);
        shell.t0axis = shell.yaxis * ((double)shell.gridSize / 0.8660254037844386 * 0.5);
        int count = shell.polygon.Count;
        shell.polyn = new VectorLF3[count];
        shell.polynu = new double[count];
        for (int l = 0; l < count; l++)
        {
            Vector3 vector = shell.polygon[l];
            Vector3 vector2 = shell.polygon[(l + 1) % count];
            shell.polyn[l] = VectorLF3.Cross(vector, vector2).normalized;
            shell.polynu[l] = shell.polyn[l].x * shell.raydir.x + shell.polyn[l].y * shell.raydir.y + shell.polyn[l].z * shell.raydir.z;
        }
        DysonShell.s_vmap.Clear();
        DysonShell.s_outvmap.Clear();
        DysonShell.s_ivmap.Clear();
        double num7 = (double)shell.gridSize * 0.5;
        for (int m = -num5; m <= num5; m++)
        {
            for (int n = -num5; n <= num5; n++)
            {
                if (m - n <= num5 && m - n >= -num5)
                {
                    VectorLF3 vectorLF3;
                    vectorLF3.x = shell.center.x + shell.w0axis.x * (double)m - shell.w1axis.x * (double)n;
                    vectorLF3.y = shell.center.y + shell.w0axis.y * (double)m - shell.w1axis.y * (double)n;
                    vectorLF3.z = shell.center.z + shell.w0axis.z * (double)m - shell.w1axis.z * (double)n;
                    double num8 = shell.radius / Math.Sqrt(vectorLF3.x * vectorLF3.x + vectorLF3.y * vectorLF3.y + vectorLF3.z * vectorLF3.z);
                    vectorLF3.x *= num8;
                    vectorLF3.y *= num8;
                    vectorLF3.z *= num8;
                    int num9 = 0;
                    for (int num10 = 0; num10 < count; num10++)
                    {
                        double num11 = -(shell.polyn[num10].x * vectorLF3.x + shell.polyn[num10].y * vectorLF3.y + shell.polyn[num10].z * vectorLF3.z) / shell.polynu[num10];
                        if (num11 >= 0.0)
                        {
                            VectorLF3 normalized2 = new VectorLF3(vectorLF3.x + num11 * shell.raydir.x, vectorLF3.y + num11 * shell.raydir.y, vectorLF3.z + num11 * shell.raydir.z).normalized;
                            normalized2.x *= shell.radius;
                            normalized2.y *= shell.radius;
                            normalized2.z *= shell.radius;
                            VectorLF3 vectorLF4 = shell.polygon[num10] - normalized2;
                            VectorLF3 vectorLF5 = shell.polygon[(num10 + 1) % count] - normalized2;
                            double num12 = vectorLF4.x * vectorLF5.x + vectorLF4.y * vectorLF5.y + vectorLF4.z * vectorLF5.z;
                            if (num12 < 0.0 || (num12 == 0.0 && vectorLF4.x == 0.0 && vectorLF4.y == 0.0 && vectorLF4.z == 0.0))
                            {
                                num9++;
                            }
                        }
                    }
                    if ((num9 & 1) == 1)
                    {
                        int num13 = DysonShell._get_key(m, n);
                        DysonShell.s_vmap[num13] = vectorLF3;
                    }
                    else
                    {
                        for (int num14 = 0; num14 < count; num14++)
                        {
                            VectorLF3 vectorLF6 = shell.polygon[num14];
                            VectorLF3 vectorLF7 = shell.polyn[num14];
                            VectorLF3 vectorLF8 = vectorLF3 - vectorLF6;
                            double num15 = vectorLF7.x * vectorLF8.x + vectorLF7.y * vectorLF8.y + vectorLF7.z * vectorLF8.z;
                            double num16 = Math.Abs(num15);
                            if (num16 <= num7)
                            {
                                VectorLF3 vectorLF9 = shell.polygon[(num14 + 1) % count];
                                VectorLF3 vectorLF10 = vectorLF3 - vectorLF7 * num15;
                                VectorLF3 vectorLF11 = vectorLF9 - vectorLF6;
                                double magnitude = vectorLF11.magnitude;
                                VectorLF3 vectorLF12 = vectorLF11 / magnitude;
                                VectorLF3 vectorLF13 = vectorLF10 - vectorLF6;
                                double num17 = vectorLF12.x * vectorLF13.x + vectorLF12.y * vectorLF13.y + vectorLF12.z * vectorLF13.z;
                                double num18;
                                if (num17 < 0.0)
                                {
                                    num18 = vectorLF8.magnitude;
                                }
                                else if (num17 > magnitude)
                                {
                                    num18 = (vectorLF3 - vectorLF9).magnitude;
                                }
                                else
                                {
                                    num18 = num16;
                                }
                                if (num18 <= num7)
                                {
                                    int num19 = DysonShell._get_key(m, n);
                                    DysonShell.s_vmap[num19] = vectorLF3;
                                    DysonShell.s_outvmap[num19] = vectorLF3;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        int count2 = DysonShell.s_vmap.Count;
        if (count2 > 32767)
        {
            return false;
        }
        shell.verts = new Vector3[count2];
        shell.uvs = new Vector2[count2];
        shell.uv2s = new Vector2[count2];
        shell.vkeys = new int[count2];
        shell._gen_points_topo_indices(count2);
        shell.vAdjs = new short[count2 * 6];
        for (int num20 = 0; num20 < shell.vAdjs.Length; num20++)
        {
            shell.vAdjs[num20] = -1;
        }
        int num21 = 0;
        foreach (KeyValuePair<int, Vector3> keyValuePair in DysonShell.s_vmap)
        {
            Vector3 value = keyValuePair.Value;
            DysonShell.s_ivmap[keyValuePair.Key] = num21;
            shell.verts[num21] = value;
            shell.uv2s[num21].x = (float)(DysonShell.s_outvmap.ContainsKey(keyValuePair.Key) ? 0 : 1);
            shell.vkeys[num21] = keyValuePair.Key;
            num21++;
        }
        foreach (KeyValuePair<int, int> keyValuePair2 in DysonShell.s_ivmap)
        {
            int key = keyValuePair2.Key;
            int num22 = DysonShell.s_ivmap[key];
            int num23 = key + 65536;
            int num24 = key - 1;
            int num25 = key - 65537;
            int num26 = key - 65536;
            int num27 = key + 1;
            int num28 = key + 65537;
            bool flag = DysonShell.s_ivmap.ContainsKey(num23);
            bool flag2 = DysonShell.s_ivmap.ContainsKey(num24);
            bool flag3 = DysonShell.s_ivmap.ContainsKey(num25);
            bool flag4 = DysonShell.s_ivmap.ContainsKey(num26);
            bool flag5 = DysonShell.s_ivmap.ContainsKey(num27);
            bool flag6 = DysonShell.s_ivmap.ContainsKey(num28);
            shell.vAdjs[num22 * 6] = (short)(flag ? DysonShell.s_ivmap[num23] : (-1));
            shell.vAdjs[num22 * 6 + 1] = (short)(flag2 ? DysonShell.s_ivmap[num24] : (-1));
            shell.vAdjs[num22 * 6 + 2] = (short)(flag3 ? DysonShell.s_ivmap[num25] : (-1));
            shell.vAdjs[num22 * 6 + 3] = (short)(flag4 ? DysonShell.s_ivmap[num26] : (-1));
            shell.vAdjs[num22 * 6 + 4] = (short)(flag5 ? DysonShell.s_ivmap[num27] : (-1));
            shell.vAdjs[num22 * 6 + 5] = (short)(flag6 ? DysonShell.s_ivmap[num28] : (-1));
        }
        int num29 = 0;
        int num30 = 0;
        for (int num31 = 0; num31 < count2; num31++)
        {
            double num32 = shell.radius * 2.0;
            int num33 = -1;
            VectorLF3 vectorLF14 = shell.verts[num31];
            for (int num34 = 0; num34 < count; num34++)
            {
                VectorLF3 vectorLF15 = shell.polygon[num34];
                VectorLF3 vectorLF16 = shell.polygon[(num34 + 1) % count];
                VectorLF3 vectorLF17 = shell.polyn[num34];
                VectorLF3 vectorLF18 = vectorLF14 - vectorLF15;
                double num35 = vectorLF17.x * vectorLF18.x + vectorLF17.y * vectorLF18.y + vectorLF17.z * vectorLF18.z;
                VectorLF3 vectorLF19 = vectorLF14 - vectorLF17 * num35;
                VectorLF3 vectorLF20 = vectorLF14 - vectorLF16;
                double num36 = vectorLF17.x * vectorLF20.x + vectorLF17.y * vectorLF20.y + vectorLF17.z * vectorLF20.z;
                VectorLF3 vectorLF21 = vectorLF16 - vectorLF15;
                double magnitude2 = vectorLF21.magnitude;
                VectorLF3 vectorLF22 = vectorLF21 / magnitude2;
                VectorLF3 vectorLF23 = vectorLF19 - vectorLF15;
                double num37 = vectorLF22.x * vectorLF23.x + vectorLF22.y * vectorLF23.y + vectorLF22.z * vectorLF23.z;
                double num38;
                if (num37 < 0.0)
                {
                    num38 = (vectorLF14 - vectorLF15).magnitude;
                    num38 -= Math.Abs(num35) * 0.001;
                }
                else if (num37 > magnitude2)
                {
                    num38 = (vectorLF14 - vectorLF16).magnitude;
                    num38 -= Math.Abs(num36) * 0.001;
                }
                else
                {
                    num38 = Math.Abs(num35);
                    num38 -= Math.Abs(num35) * 0.001;
                }
                if (num38 < num32)
                {
                    num32 = num38;
                    num33 = num34;
                }
            }
            shell.uv2s[num31].y = (float)num33;
            if (num29 + num30 < 49 && shell._is_point_in_shell(vectorLF14))
            {
                double num39 = VectorLF3.Dot(vectorLF14 - shell.polygon[num33], shell.polyn[num33]);
                if (num39 > 0.1)
                {
                    num29++;
                }
                else if (num39 < -0.1)
                {
                    num30++;
                }
            }
        }
        if (num29 > num30)
        {
            shell.clockwise = true;
        }
        else
        {
            shell.clockwise = false;
        }
        shell.vertAttr = new int[count2];
        shell.vertsq = new short[count2];
        shell.vertsqOffset = new int[shell.nodes.Count + 1];
        int count3 = shell.nodes.Count;
        int num40 = count3 / 2;
        for (int num41 = 0; num41 < count2; num41++)
        {
            Vector3 vector3 = shell.verts[num41];
            double num42 = double.MaxValue;
            int num43 = 0;
            int num44 = 0;
            int num45 = num41 + 479001600;
            for (int num46 = 0; num46 < count3; num46++)
            {
                int num47 = num45 % count3 - num46;
                if (num47 < 0)
                {
                    num47 = -num47;
                }
                if (num47 > num40)
                {
                    num47 = count3 - num47;
                }
                double num48 = (double)(vector3 - shell.nodes[num46].pos).sqrMagnitude;
                num48 += (double)num47;
                if (num48 < num42)
                {
                    num42 = num48;
                    num43 = shell.nodes[num46].id;
                    num44 = num46;
                }
            }
            shell.vertAttr[num41] = num43;
            shell.vertsqOffset[num44]++;
        }
        int num49 = 0;
        for (int num50 = 0; num50 < shell.vertsqOffset.Length; num50++)
        {
            num49 += shell.vertsqOffset[num50];
        }
        Assert.True(num49 == count2);
        for (int num51 = shell.vertsqOffset.Length - 1; num51 >= 0; num51--)
        {
            shell.vertsqOffset[num51] = num49;
            if (num51 > 0)
            {
                num49 -= shell.vertsqOffset[num51 - 1];
            }
        }
        Assert.Zero(num49);
        shell._openListPrepare();
        int num52 = shell.randSeed;
        int num53 = 0;
        while (num53 < count3)
        {
            Vector3 pos = shell.nodes[num53].pos;
            int num54 = shell.nodes[num53].id;
            float num55 = float.MaxValue;
            int num56 = -1;
            for (int num57 = 0; num57 < count2; num57++)
            {
                if (shell.vertAttr[num57] == num54 && (shell.vAdjs[num57 * 6] >= 0 || shell.vAdjs[num57 * 6 + 1] >= 0 || shell.vAdjs[num57 * 6 + 2] >= 0 || shell.vAdjs[num57 * 6 + 3] >= 0 || shell.vAdjs[num57 * 6 + 4] >= 0 || shell.vAdjs[num57 * 6 + 5] >= 0))
                {
                    float sqrMagnitude = (shell.verts[num57] - pos).sqrMagnitude;
                    if (sqrMagnitude < num55)
                    {
                        num55 = sqrMagnitude;
                        num56 = num57;
                    }
                }
            }
            if (num56 >= 0)
            {
                shell._openListAdd(num56);
            }
            int num58 = shell.vertsqOffset[num53];
            int num59 = shell.vertsqOffset[num53 + 1] - shell.vertsqOffset[num53];
            double num60 = 0.0;
            for (; ; )
            {
                int num61 = shell._traverseRandomVertex(shell.nodes[num53].id, ref num52);
                if (num61 < 0)
                {
                    break;
                }
                shell.vertsq[num58++] = (short)num61;
                num60 += 1.0;
                shell.uvs[num61].x = (float)num53;
                shell.uvs[num61].y = (float)(num60 / (double)num59);
                if (num58 == shell.vertsqOffset[num53 + 1])
                {
                    goto Block_57;
                }
            }
        IL_1329:
            if (num58 < shell.vertsqOffset[num53 + 1])
            {
                for (int num62 = 0; num62 < count2; num62++)
                {
                    if (shell.vertAttr[num62] == num54 && !shell._tmp_marked[num62])
                    {
                        shell.vertsq[num58++] = (short)num62;
                        num60 += 1.0;
                        shell.uvs[num62].x = (float)num53;
                        shell.uvs[num62].y = (float)(num60 / (double)num59);
                    }
                }
            }
            Assert.False(num58 > shell.vertsqOffset[num53 + 1]);
            Assert.False(num58 < shell.vertsqOffset[num53 + 1]);
            shell._openListClear();
            num53++;
            continue;
        Block_57:
            Assert.Zero(shell._tmp_openCount);
            goto IL_1329;
        }
        shell._openListFree();
        shell.vertexCount = count2;
        return true;
    }

    private static int QuickAddDysonShell(this DysonSphereLayer layer, int protoId, DysonNode[] nodes, DysonFrame[] frames, bool limit)
    {
        int shellId = 0;
        if (layer.shellRecycleCursor > 0)
        {
            int[] array = layer.shellRecycle;
            int index = layer.shellRecycleCursor - 1;
            layer.shellRecycleCursor = index;
            shellId = array[index];
        }
        else
        {
            int index = layer.shellCursor;
            layer.shellCursor = index + 1;
            shellId = index;
            if (shellId == layer.shellCapacity)
            {
                layer.SetShellCapacity(layer.shellCapacity * 2);
            }
        }
        var shell = layer.shellPool[shellId];
        if (shell == null)
        {
            shell = new DysonShell(layer);
            layer.shellPool[shellId] = shell;
        }
        else
        {
            shell.SetEmpty();
        }
        shell.id = shellId;
        shell.layerId = layer.id;
        shell.protoId = protoId;
        shell.randSeed = layer.id * 10000 + shellId;
        for (int j = 0; j < nodes.Length; j++)
        {
            DysonNode dysonNode = nodes[j];
            DysonFrame dysonFrame = frames[j];
            List<Vector3> segments = dysonFrame.GetSegments();
            if (dysonNode == dysonFrame.nodeA)
            {
                for (int k = 0; k < segments.Count - 1; k++)
                {
                    shell.polygon.Add(segments[k]);
                }
            }
            else
            {
                for (int l = segments.Count - 1; l >= 1; l--)
                {
                    shell.polygon.Add(segments[l]);
                }
            }
            shell.nodeIndexMap[dysonNode.id] = shell.nodes.Count;
            shell.nodes.Add(dysonNode);
            shell.frames.Add(dysonFrame);
        }
        if (!shell.MyGenerateGeometry() || (limit && DysonShell.s_vmap.Count < 32000))
        {
            CheatEnabler.Logger.LogDebug($"Stripped VertCount: {DysonShell.s_vmap.Count}");
            shell.Free();
            layer.shellPool[shellId] = null;
            int recycleIndex = layer.shellRecycleCursor;
            layer.shellRecycleCursor = recycleIndex + 1;
            layer.shellRecycle[recycleIndex] = shellId;
            return 0;
        }
        CheatEnabler.Logger.LogDebug($"Shell {shellId} VertCount: {DysonShell.s_vmap.Count}");
        for (int j = 0; j < shell.nodes.Count; j++)
        {
            shell.nodes[j].shells.Add(shell);
        }
        shell.GenerateModelObjects();
        return shellId;
    }

    private static void QuickRemoveDysonNode(this DysonSphereLayer layer, int nodeId)
    {
        var node = layer.nodePool[nodeId];
        if (node == null || node.id != nodeId) return;
        var dysonSphere = layer.dysonSphere;
        dysonSphere.swarm.OnNodeRemove(layer.id, nodeId);
        dysonSphere.RemoveAutoNode(node);
        dysonSphere.RemoveNodeRocket(node);
        dysonSphere.RemoveDysonNodeRData(node);
        node.Free();
        layer.nodePool[nodeId] = null;
        int recycleIndex = layer.nodeRecycleCursor;
        layer.nodeRecycleCursor = recycleIndex + 1;
        layer.nodeRecycle[recycleIndex] = nodeId;
    }

    private static void QuickRemoveDysonFrame(this DysonSphereLayer layer, int frameId)
    {
        var frame = layer.framePool[frameId];
        frame.nodeA.frames.Remove(frame);
        frame.nodeB.frames.Remove(frame);
        frame.Free();
        layer.framePool[frameId] = null;
        int recycleIndex = layer.frameRecycleCursor;
        layer.frameRecycleCursor = recycleIndex + 1;
        layer.frameRecycle[recycleIndex] = frameId;
    }

    public static void DuplicateShellsWithHighestProduction()
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
                UIMessageBox.Show("CheatEnabler".Translate(), "You are not in any system.".Translate(), "确定".Translate(), UIMessageBox.ERROR, null);
                return;
            }
        }
        var dysonSphere = GameMain.data?.dysonSpheres[star.index];
        if (dysonSphere == null || dysonSphere.layerCount == 0)
        {
            UIMessageBox.Show("CheatEnabler".Translate(), string.Format("There is no Dyson Sphere shell on \"{0}\".".Translate(), star.displayName), "确定".Translate(), UIMessageBox.ERROR, null);
            return;
        }
        DysonShell.s_vmap ??= new Dictionary<int, Vector3>(16384);
        DysonShell.s_outvmap ??= new Dictionary<int, Vector3>(16384);
        DysonShell.s_ivmap ??= new Dictionary<int, int>(16384);
        DysonSphereLayer layer = null;
        var nodePos = new List<Vector3>();
        var isEuler = new List<bool>();
        DysonShell shell = null;
        for (var i = 1; i < dysonSphere.layersIdBased.Length; i++)
        {
            layer = dysonSphere.layersIdBased[i];
            if (layer == null || layer.id != i) continue;
            for (var j = 1; j < layer.shellCursor; j++)
            {
                shell = layer.shellPool[j];
                if (shell == null) continue;
                if (shell.id != j)
                {
                    shell = null;
                    continue;
                }
                nodePos.AddRange(shell.nodes.Select(node => node.pos));
                isEuler.AddRange(shell.frames.Select(frame => frame.euler));
                break;
            }
            if (nodePos.Count > 0) break;
        }
        if (nodePos.Count == 0)
        {
            UIMessageBox.Show("CheatEnabler".Translate(), string.Format("There is no Dyson Sphere shell on \"{0}\".".Translate(), star.displayName), "确定".Translate(), UIMessageBox.ERROR, null);
            return;
        }
        var currentShellCount = layer.shellCount;
        var keepCount = ShellsCountForFunctions.Value;
        if (currentShellCount >= keepCount) return;
        CheatEnabler.Logger.LogDebug($"NodePositions: {nodePos[0]}, {nodePos[1]}, {nodePos[2]}");
        var nodeCount = nodePos.Count;
        DysonNode[] nodes = [.. shell.nodes];
        DysonFrame[] frames = [.. shell.frames];
        var cpMax = new long[nodeCount];
        for (var i = 0; i < nodeCount; i++)
        {
            cpMax[i] = (shell.vertsqOffset[i + 1] - shell.vertsqOffset[i]) * shell.cpPerVertex;
        }
        long[] totalCpMax = [.. nodes.Select(node => node.totalCpMax)];
        var dirtyFrames = new HashSet<int>();
        for (var i = currentShellCount; i < keepCount; i++)
        {
            dirtyFrames.Clear();
            for (var j = 0; j < nodeCount; j++)
            {
                totalCpMax[j] += cpMax[j];
                if (totalCpMax[j] > int.MaxValue)
                {
                    totalCpMax[j] = cpMax[j];
                    dirtyFrames.Add(j > 0 ? j - 1 : nodeCount - 1);
                    dirtyFrames.Add(j);
                    nodes[j] = layer.QuickAddDysonNode(0, nodePos[j]);
                }
            }
            foreach (var frameId in dirtyFrames)
            {
                frames[frameId] = layer.QuickAddDysonFrame(0, nodes[frameId], nodes[(frameId + 1) % nodeCount], isEuler[frameId]);
            }
            layer.QuickAddDysonShell(0, nodes, frames, false);
        }
        foreach (var node in nodes)
        {
            node.RecalcSpReq();
            node.RecalcCpReq();
        }
        dysonSphere.CheckAutoNodes();
        if (dysonSphere.autoNodeCount <= 0)
        {
            dysonSphere.PickAutoNode();
        }
        dysonSphere.modelRenderer.RebuildModels();
        GameMain.gameScenario.NotifyOnPlanDysonShell();
        dysonSphere.inEditorRenderMaskS = 0;
        dysonSphere.inEditorRenderMaskL = 0;
        dysonSphere.inGameRenderMaskS = 0;
        dysonSphere.inGameRenderMaskL = 0;
    }

    public static void KeepMaxProductionShells()
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
                UIMessageBox.Show("CheatEnabler".Translate(), "You are not in any system.".Translate(), "确定".Translate(), UIMessageBox.ERROR, null);
                return;
            }
        }
        var dysonSphere = GameMain.data?.dysonSpheres[star.index];
        if (dysonSphere == null || dysonSphere.layerCount == 0)
        {
            UIMessageBox.Show("CheatEnabler".Translate(), string.Format("There is no Dyson Sphere shell on \"{0}\".".Translate(), star.displayName), "确定".Translate(), UIMessageBox.ERROR, null);
            return;
        }
        int retainCount = ShellsCountForFunctions.Value;
        for (var i = 1; i < dysonSphere.layersIdBased.Length; i++)
        {
            var layer = dysonSphere.layersIdBased[i];
            if (layer == null || layer.id != i) continue;
            var shells = layer.shellPool.Where(shell => shell != null).OrderByDescending(shell => shell.vertexCount).ToArray();
            if (shells.Length < 1) continue;
            for (var j = retainCount; j < shells.Length; j++)
            {
                var shell = shells[j];
                var id = shell.id;
                layer.shellPool[id] = null;
                for (var k = 0; k < shell.nodes.Count; k++)
                {
                    shell.nodes[k].shells.Remove(shell);
                }
                shell.Free();
                shells[j] = null;
            }
            var poolCapacity = AlignUpToPowerOfTwo(retainCount + 1);
            if (poolCapacity < 64) poolCapacity = 64;
            layer.shellPool = new DysonShell[poolCapacity];
            layer.shellRecycle = new int[poolCapacity];
            layer.shellRecycleCursor = 0;
            layer.shellCapacity = poolCapacity;
            layer.shellCursor = retainCount + 1;
            HashSet<int> retainNodes = [];
            HashSet<(int, int)> retainFrames = [];
            for (var j = 0; j < retainCount; j++)
            {
                var shell = shells[j];
                retainNodes.UnionWith(shell.nodes.Select(node => node.id));
                layer.shellPool[1] = shell;
                shell.id = 1;
                for (var k = 0; k < shell.nodes.Count; k++)
                {
                    var idA = shell.nodes[k].id;
                    var idB = shell.nodes[(k + 1) % shell.nodes.Count].id;
                    retainFrames.Add((idA, idB));
                    retainFrames.Add((idB, idA));
                }
            }
            var nodes = layer.nodePool.Where(node => node != null && retainNodes.Contains(node.id)).ToArray();
            var frames = layer.framePool.Where(frame => frame != null && retainFrames.Contains((frame.nodeA.id, frame.nodeB.id))).ToArray();
            poolCapacity = AlignUpToPowerOfTwo(frames.Length + 1);
            if (poolCapacity < 64) poolCapacity = 64;
            layer.framePool = new DysonFrame[poolCapacity];
            layer.frameRecycle = new int[poolCapacity];
            layer.frameRecycleCursor = 0;
            layer.frameCapacity = poolCapacity;
            layer.frameCursor = frames.Length + 1;
            for (var j = 0; j < frames.Length; j++)
            {
                int id = j + 1;
                layer.framePool[id] = frames[j];
                frames[j].id = id;
            }
            foreach (var node in nodes)
            {
                if (node != null && node.id > 0)
                {
                    dysonSphere.RemoveDysonNodeRData(node);
                }
            }
            poolCapacity = AlignUpToPowerOfTwo(nodes.Length + 1);
            if (poolCapacity < 64) poolCapacity = 64;
            layer.nodePool = new DysonNode[poolCapacity];
            layer.nodeRecycle = new int[poolCapacity];
            layer.nodeRecycleCursor = 0;
            layer.nodeCapacity = poolCapacity;
            layer.nodeCursor = nodes.Length + 1;
            for (var j = 0; j < nodes.Length; j++)
            {
                int id = j + 1;
                layer.nodePool[id] = nodes[j];
                nodes[j].id = id;
            }
            for (var j = 1; j < layer.shellCursor; j++)
            {
                var shell = layer.shellPool[j];
                if (shell == null || shell.id != j) continue;
                shell.nodeIndexMap.Clear();
                for (var k = 0; k < shell.nodes.Count; k++)
                {
                    shell.nodeIndexMap[shell.nodes[k].id] = k;
                }
            }
            for (var j = 1; j < layer.nodeCursor; j++)
            {
                var node = layer.nodePool[j];
                if (node == null || node.id != j) continue;
                dysonSphere.AddDysonNodeRData(node, true);
                node.RecalcSpReq();
                node.RecalcCpReq();
            }
        }
        dysonSphere.CheckAutoNodes();
        if (dysonSphere.autoNodeCount <= 0)
        {
            dysonSphere.PickAutoNode();
        }
        dysonSphere.modelRenderer.RebuildModels();
    }

    public static void CreateIllegalDysonShellWithMaxOutput()
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
                UIMessageBox.Show("CheatEnabler".Translate(), "You are not in any system.".Translate(), "确定".Translate(), UIMessageBox.ERROR, null);
                return;
            }
        }
        UXAssist.Functions.DysonSphereFunctions.InitCurrentDysonLayer(star, 0);
        var dysonSphere = GameMain.data?.dysonSpheres[star.index];
        if (dysonSphere == null)
        {
            UIMessageBox.Show("CheatEnabler".Translate(), string.Format("There is no Dyson Sphere data on \"{0}\".".Translate(), star.displayName), "确定".Translate(), UIMessageBox.ERROR, null);
            return;
        }
        DysonShell.s_vmap ??= new Dictionary<int, Vector3>(16384);
        DysonShell.s_outvmap ??= new Dictionary<int, Vector3>(16384);
        DysonShell.s_ivmap ??= new Dictionary<int, int>(16384);
        var shellsChanged = false;
        var mutex = new object();
        Dictionary<(int, int), int> availableFrames = [];
        HashSet<int> unusedFrameIds = [];
        var layer = dysonSphere.layersIdBased[1];
        if (layer != null)
        {
            dysonSphere.RemoveLayer(1);
        }
        var maxOrbitRadius = Patches.DysonSpherePatch.UnlockMaxOrbitRadiusEnabled.Value ? Patches.DysonSpherePatch.UnlockMaxOrbitRadiusValue.Value : dysonSphere.maxOrbitRadius;
        layer = dysonSphere.AddLayerOnId(1, maxOrbitRadius, Quaternion.Euler(0f, 0f, 0f), Mathf.Sqrt(dysonSphere.gravity / maxOrbitRadius) / maxOrbitRadius * 57.2957802f);
        if (layer == null) return;

        var supposedShells = new List<SupposedShell>(60 * 59 * 58);
        VectorLF3[] nodePos = new VectorLF3[60];
        for (var i = 0; i < 60; i++)
        {
            nodePos[i] = new VectorLF3(Math.Sin(Math.PI * 2 * i / 60), 0, Math.Cos(Math.PI * 2 * i / 60)) * layer.orbitRadius;
        }
        for (var i = 0; i < 58; i++)
        {
            for (var j = i + 1; j < 59; j++)
            {
                for (var k = j + 1; k < 60; k++)
                {
                    var area = Vector3.Cross(nodePos[j] - nodePos[i], nodePos[k] - nodePos[i]).sqrMagnitude;
                    supposedShells.Add(new SupposedShell { posA = nodePos[i], posB = nodePos[j], posC = nodePos[k], area = area });
                }
            }
        }
        supposedShells.Sort((a, b) => b.area.CompareTo(a.area));
        CheatEnabler.Logger.LogDebug($"Finished Area Sort");
        var maxVertCount = -1;
        var maxJ = -1;
        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 };

        var gridScale = (int)(Math.Pow(maxOrbitRadius / 4000.0, 0.75) + 0.5);
        gridScale = (gridScale < 1) ? 1 : gridScale;
        var cpPerVertex = gridScale * gridScale * 2;
        var barrier = 0x7FFFFFFF / cpPerVertex;
        if (barrier > 32767) barrier = 32767;
        var truncValue = barrier / 1000 * 1000;
        CheatEnabler.Logger.LogDebug($"cpPerVertex: {cpPerVertex}, Barrier: {barrier}, TruncValue: {truncValue}");

        Parallel.For(0, supposedShells.Count, options, (j, loopState) =>
        {
            var sshell = supposedShells[j];
            var vertCount = CalculateTriangleVertCount([sshell.posA, sshell.posB, sshell.posC]);
            if (vertCount <= barrier)
            {
                lock (mutex)
                {
                    if (loopState.ShouldExitCurrentIteration) return;
                    if (vertCount > maxVertCount)
                    {
                        maxVertCount = vertCount;
                        maxJ = j;
                        if (maxVertCount >= truncValue)
                        {
                            CheatEnabler.Logger.LogDebug($"!!STOP!! Triangle {j}[{sshell.posA:F2} {sshell.posB:F2} {sshell.posC:F2}] has {vertCount} vertices");
                            loopState.Stop();
                            return;
                        }
                        CheatEnabler.Logger.LogDebug($"Triangle {j}[{sshell.posA:F2} {sshell.posB:F2} {sshell.posC:F2}] has {vertCount} vertices");
                    }
                }
            }
        });
        if (maxJ >= 0)
        {
            layer.nodePool = new DysonNode[64];
            layer.nodeRecycle = new int[64];
            layer.nodeRecycleCursor = 0;
            layer.nodeCapacity = 64;
            layer.nodeCursor = 1;
            layer.framePool = new DysonFrame[64];
            layer.frameRecycle = new int[64];
            layer.frameRecycleCursor = 0;
            layer.frameCapacity = 64;
            layer.frameCursor = 1;
            layer.shellPool = new DysonShell[64];
            layer.shellRecycle = new int[64];
            layer.shellRecycleCursor = 0;
            layer.shellCapacity = 64;
            layer.shellCursor = 1;
            var sshell = supposedShells[maxJ];
            DysonNode[] newNodes = [layer.QuickAddDysonNode(0, sshell.posA), layer.QuickAddDysonNode(0, sshell.posB), layer.QuickAddDysonNode(0, sshell.posC)];
            DysonFrame[] newFrames = [layer.QuickAddDysonFrame(0, newNodes[0], newNodes[1], false), layer.QuickAddDysonFrame(0, newNodes[1], newNodes[2], false), layer.QuickAddDysonFrame(0, newNodes[2], newNodes[0], false)];
            layer.QuickAddDysonShell(0, newNodes, newFrames, false);
            foreach (var node in newNodes)
            {
                node.RecalcSpReq();
                node.RecalcCpReq();
            }
            shellsChanged = true;
        }

        dysonSphere.CheckAutoNodes();
        if (dysonSphere.autoNodeCount <= 0) dysonSphere.PickAutoNode();
        dysonSphere.modelRenderer.RebuildModels();
        if (shellsChanged) GameMain.gameScenario.NotifyOnPlanDysonShell();
        dysonSphere.inEditorRenderMaskS = 0;
        dysonSphere.inEditorRenderMaskL = 0;
        dysonSphere.inGameRenderMaskS = 0;
        dysonSphere.inGameRenderMaskL = 0;
    }

    private static int AlignUpToPowerOfTwo(int value)
    {
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    private class SupposedShell
    {
        public VectorLF3 posA;
        public VectorLF3 posB;
        public VectorLF3 posC;

        public float area;
    }
}
