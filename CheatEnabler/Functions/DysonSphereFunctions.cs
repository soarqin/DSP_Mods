using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using HarmonyLib;
using UnityEngine;
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
                        productRegister[11902] += 0x40000000;
                        count -= 0x40000000;
                    }
                    if (count > 0L) productRegister[11902] += (int)count;
                    count = solarSailCount;
                    while (count > 0x40000000L)
                    {
                        productRegister[11901] += 0x40000000;
                        productRegister[11903] += 0x40000000;
                        count -= 0x40000000;
                    }
                    if (count > 0L)
                    {
                        productRegister[11901] += (int)count;
                        productRegister[11903] += (int)count;
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
                        consumeRegister[11901] += 0x40000000;
                        count -= 0x40000000;
                    }
                    if (count > 0L) consumeRegister[11901] += (int)count;
                }
            }
        });
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

    private static int QuickAddDysonShell(this DysonSphereLayer layer, int protoId, DysonNode[] nodes, DysonFrame[] frames)
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
			DysonNode dysonNode2 = nodes[(j + 1) % nodes.Length];
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
			shell.nodeIndexMap[nodes[j % nodes.Length].id] = shell.nodes.Count;
			shell.nodes.Add(dysonNode);
			shell.frames.Add(dysonFrame);
			if (!dysonNode.shells.Contains(shell))
			{
				dysonNode.shells.Add(shell);
			}
		}
		shell.GenerateGeometry();
		shell.GenerateModelObjects();
        CheatEnabler.Logger.LogInfo($"QuickAddDysonShell: {DysonShell.s_vmap.Count}");
		return shellId;
    }

    private struct SupposedShell
    {
        public DysonNode nodeA;
        public DysonNode nodeB;
        public DysonNode nodeC;

        public float area;
    }

    public static void CreatePossibleFramesAndShells()
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
        var framesChanged = false;
        var shellsChanged = false;
        for (var i = 1; i < dysonSphere.layersIdBased.Length; i++)
        {
            Dictionary<(int, int), int> availableFrames = [];
            HashSet<(int, int, int)> availableShells = [];
            HashSet<DysonNode> spDirtyNodes = [];
            HashSet<DysonNode> cpDirtyNodes = [];
            var layer = dysonSphere.layersIdBased[i];
            if (layer == null || layer.id != i) continue;
            for (var j = 1; j < layer.frameCursor; j++)
            {
                var frame = layer.framePool[j];
                if (frame == null || frame.id != j) continue;
                var idA = frame.nodeA.id;
                var idB = frame.nodeB.id;
                if (idA > idB)
                {
                    (idA, idB) = (idB, idA);
                }
                availableFrames[(idA, idB)] = j;
            }
            for (var j = 1; j < layer.shellCursor; j++)
            {
                var shell = layer.shellPool[j];
                if (shell == null || shell.id != j) continue;
                if (shell.nodes.Count != 3) continue;
                var ids = shell.nodes.Select(node => node.id).OrderBy(id => id).ToArray();
                availableShells.Add((ids[0], ids[1], ids[2]));
            }
            int nodeCount = layer.nodeCursor;

            List<SupposedShell> supposedShells = [];
            for (var j = 1; j < nodeCount; j++)
            {
                var nodeA = layer.nodePool[j];
                if (nodeA == null || nodeA.id != j) continue;
                for (var k = j + 1; k < nodeCount; k++)
                {
                    var nodeB = layer.nodePool[k];
                    if (nodeB == null || nodeB.id != k) continue;
                    for (var l = k + 1; l < nodeCount; l++)
                    {
                        var nodeC = layer.nodePool[l];
                        if (nodeC == null || nodeC.id != l) continue;
                        var area = Vector3.Cross(nodeB.pos - nodeA.pos, nodeC.pos - nodeA.pos).sqrMagnitude;
                        supposedShells.Add(new SupposedShell { nodeA = nodeA, nodeB = nodeB, nodeC = nodeC, area = area });
                    }
                }
            }
            supposedShells.Sort((a, b) => b.area.CompareTo(a.area));
            var count = Math.Min(supposedShells.Count, 1);
            for (var j = 0; j < count; j++)
            {
                var shell = supposedShells[j];
                if (availableShells.TryGetValue((shell.nodeA.id, shell.nodeB.id, shell.nodeC.id), out _)) continue;
                if (!availableFrames.TryGetValue((shell.nodeA.id, shell.nodeB.id), out _)) {
                    var frame = layer.QuickAddDysonFrame(0, shell.nodeA, shell.nodeB, false);
                    availableFrames[(shell.nodeA.id, shell.nodeB.id)] = frame.id;
                    spDirtyNodes.Add(shell.nodeA);
                    spDirtyNodes.Add(shell.nodeB);
                }
                if (!availableFrames.TryGetValue((shell.nodeA.id, shell.nodeC.id), out _)) {
                    var frame = layer.QuickAddDysonFrame(0, shell.nodeA, shell.nodeC, false);
                    availableFrames[(shell.nodeA.id, shell.nodeC.id)] = frame.id;
                    spDirtyNodes.Add(shell.nodeA);
                    spDirtyNodes.Add(shell.nodeC);
                }
                if (!availableFrames.TryGetValue((shell.nodeB.id, shell.nodeC.id), out _)) {
                    var frame = layer.QuickAddDysonFrame(0, shell.nodeB, shell.nodeC, false);
                    availableFrames[(shell.nodeB.id, shell.nodeC.id)] = frame.id;
                    spDirtyNodes.Add(shell.nodeB);
                    spDirtyNodes.Add(shell.nodeC);
                }
            }
            foreach (var node in spDirtyNodes)
            {
                node.RecalcSpReq();
            }
            framesChanged = framesChanged || spDirtyNodes.Count > 0;
            for (var j = 0; j < count; j++)
            {
                var shell = supposedShells[j];
                if (availableShells.TryGetValue((shell.nodeA.id, shell.nodeB.id, shell.nodeC.id), out _)) continue;
                DysonFrame[] frames = [layer.framePool[availableFrames[(shell.nodeA.id, shell.nodeB.id)]], layer.framePool[availableFrames[(shell.nodeA.id, shell.nodeC.id)]], layer.framePool[availableFrames[(shell.nodeB.id, shell.nodeC.id)]]];
                DysonNode[] nodes = [shell.nodeA, shell.nodeB, shell.nodeC];
                layer.QuickAddDysonShell(0, nodes, frames);
                cpDirtyNodes.Add(shell.nodeA);
                cpDirtyNodes.Add(shell.nodeB);
                cpDirtyNodes.Add(shell.nodeC);
                shellsChanged = true;
            }
            foreach (var node in cpDirtyNodes)
            {
                node.RecalcCpReq();
            }
        }
        if (framesChanged)
        {
            dysonSphere.CheckAutoNodes();
            if (dysonSphere.autoNodeCount <= 0)
            {
                dysonSphere.PickAutoNode();
            }
        }
        if (shellsChanged)
        {
            GameMain.gameScenario.NotifyOnPlanDysonShell();
        }
        if (framesChanged || shellsChanged)
        {
            dysonSphere.modelRenderer.RebuildModels();
        }
    }
}
