using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CheatEnabler.Functions;
using static CheatEnabler.Functions.DysonSphere.GeometryHelpers;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.GameConstants;
using UXAssist.Common.ModFeatures;
using CheatEnabler;

namespace CheatEnabler.Functions.DysonSphere;

[ModFeature("IllegalShellFunctions")]
public static class IllegalShellFunctions
{
    private static void EnsureDysonShellMaps()
    {
        DysonShell.s_vmap ??= new Dictionary<int, Vector3>(16384);
        DysonShell.s_outvmap ??= new Dictionary<int, Vector3>(16384);
        DysonShell.s_ivmap ??= new Dictionary<int, int>(16384);
    }

    private static void ResetLayerPools(DysonSphereLayer layer)
    {
        layer.nodePool = new DysonNode[DysonSphereConstants.DefaultLayerPoolCapacity];
        layer.nodeRecycle = new int[DysonSphereConstants.DefaultLayerPoolCapacity];
        layer.nodeRecycleCursor = 0;
        layer.nodeCapacity = DysonSphereConstants.DefaultLayerPoolCapacity;
        layer.nodeCursor = 1;
        layer.framePool = new DysonFrame[DysonSphereConstants.DefaultLayerPoolCapacity];
        layer.frameRecycle = new int[DysonSphereConstants.DefaultLayerPoolCapacity];
        layer.frameRecycleCursor = 0;
        layer.frameCapacity = DysonSphereConstants.DefaultLayerPoolCapacity;
        layer.frameCursor = 1;
        layer.shellPool = new DysonShell[DysonSphereConstants.DefaultLayerPoolCapacity];
        layer.shellRecycle = new int[DysonSphereConstants.DefaultLayerPoolCapacity];
        layer.shellRecycleCursor = 0;
        layer.shellCapacity = DysonSphereConstants.DefaultLayerPoolCapacity;
        layer.shellCursor = 1;
    }

    private static void FinalizeDysonSphereChanges(global::DysonSphere sphere, DysonSphereLayer layer, bool notify = false, bool resetRenderMasks = false)
    {
        if (sphere == null) return;
        sphere.CheckAutoNodes();
        if (sphere.autoNodeCount <= 0) sphere.PickAutoNode();
        sphere.modelRenderer.RebuildModels();
        if (notify) GameMain.gameScenario?.NotifyOnPlanDysonShell();
        if (resetRenderMasks)
        {
            sphere.inEditorRenderMaskS = 0;
            sphere.inEditorRenderMaskL = 0;
            sphere.inGameRenderMaskS = 0;
            sphere.inGameRenderMaskL = 0;
        }
    }

    public static void DuplicateShellsWithHighestProduction()
    {
        var resolved = DysonSphereResolver.ResolveEditorOrLocalSphere();
        if (resolved == null) return;
        var (dysonSphere, star) = resolved.Value;
        EnsureDysonShellMaps();
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
            UIMessageBox.Show(Localization.CheatEnabler.Translate(), string.Format(Localization.ThereIsNoDysonSphereShellOn0.Translate(), star.displayName), Localization.OK.Translate(), UIMessageBox.ERROR, null);
            return;
        }
        var currentShellCount = layer.shellCount;
        var keepCount = DysonSphereFunctions.ShellsCountForFunctions.Value;
        if (currentShellCount >= keepCount) return;
        CheatEnabler.Logger.LogDebug($"NodePositions: {nodePos[0]}, {nodePos[1]}, {nodePos[2]}");
        var nodeCount = nodePos.Count;
        DysonNode[] nodes = [.. shell.nodes];
        DysonFrame[] frames = [.. shell.frames];
        if (frames.Length == 0)
        {
            isEuler = new List<bool>(nodeCount);
            frames = new DysonFrame[nodeCount];
            for (var i = 0; i < nodeCount; i++)
            {
                isEuler.Add(false);
                frames[i] = layer.QuickAddDysonFrame(0, nodes[i], nodes[(i + 1) % nodeCount], false);
            }
        }
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
                if (totalCpMax[j] > DysonSphereConstants.TotalCpMaxCeiling)
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
        FinalizeDysonSphereChanges(dysonSphere, layer, true, true);
    }

    public static void KeepMaxProductionShells()
    {
        var resolved = DysonSphereResolver.ResolveEditorOrLocalSphere();
        if (resolved == null) return;
        var (dysonSphere, star) = resolved.Value;
        int retainCount = DysonSphereFunctions.ShellsCountForFunctions.Value;
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
            var poolCapacity = GeometryHelpers.AlignUpToPowerOfTwo(retainCount + 1);
            if (poolCapacity < DysonSphereConstants.DefaultLayerPoolCapacity) poolCapacity = DysonSphereConstants.DefaultLayerPoolCapacity;
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
                int shellId = j + 1;
                layer.shellPool[shellId] = shell;
                shell.id = shellId;
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
            poolCapacity = GeometryHelpers.AlignUpToPowerOfTwo(frames.Length + 1);
            if (poolCapacity < DysonSphereConstants.DefaultLayerPoolCapacity) poolCapacity = DysonSphereConstants.DefaultLayerPoolCapacity;
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
            poolCapacity = GeometryHelpers.AlignUpToPowerOfTwo(nodes.Length + 1);
            if (poolCapacity < DysonSphereConstants.DefaultLayerPoolCapacity) poolCapacity = DysonSphereConstants.DefaultLayerPoolCapacity;
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
        FinalizeDysonSphereChanges(dysonSphere, null);
    }

    public static void CreateIllegalDysonShellQuickly(int triangleCount)
    {
        var resolved = DysonSphereResolver.ResolveEditorOrLocalSphere(requireLayer: false);
        if (resolved == null) return;
        var (dysonSphere, _) = resolved.Value;

        EnsureDysonShellMaps();

        for (int i = 1; i <= 10; i++)
        {
            var layer = dysonSphere.layersIdBased[i];
            if (layer != null)
            {
                continue;
            }
            var radius = dysonSphere.maxOrbitRadius;
            for (; radius > DysonSphereConstants.MinOrbitRadiusSearch; radius -= DysonSphereConstants.OrbitRadiusSearchStep)
            {
                if (dysonSphere.CheckLayerRadius(radius) == 0)
                {
                    break;
                }
            }
            PrecalculatedTriangle triangle;
            try
            {
                triangle = GeometryHelpers.PrecalculatedTriangles.First(t => t.MaxOrbitRadius > radius);
            }
            catch (InvalidOperationException)
            {
                UIMessageBox.Show(Localization.CheatEnabler.Translate(), string.Format(Localization.NoPrecalculatedShellFoundForRadius0.Translate(), radius), Localization.OK.Translate(), UIMessageBox.ERROR, null);
                return;
            }
            layer = dysonSphere.AddLayerOnId(i, radius, Quaternion.Euler(0f, 0f, 0f), Mathf.Sqrt(dysonSphere.gravity / radius) / radius * DysonSphereConstants.RadiansToDegrees);
            if (layer == null) return;
            Vector3[] nodePos = [triangle.PosA.normalized * radius, triangle.PosB.normalized * radius, triangle.PosC.normalized * radius];
            DysonNode[] nodes = [layer.QuickAddDysonNode(0, nodePos[0]), layer.QuickAddDysonNode(0, nodePos[1]), layer.QuickAddDysonNode(0, nodePos[2])];
            DysonFrame[] frames = [layer.QuickAddDysonFrame(0, nodes[0], nodes[1], false), layer.QuickAddDysonFrame(0, nodes[1], nodes[2], false), layer.QuickAddDysonFrame(0, nodes[2], nodes[0], false)];
            var shellId = layer.QuickAddDysonShell(0, nodes, frames, false);
            if (shellId == 0) return;
            var shell = layer.shellPool[shellId];
            long[] cpMax = [.. nodes.Select(node => node.totalCpMax)];
            long[] totalCpMax = [.. cpMax];
            var dirtyFrames = new HashSet<int>();
            for (var j = 1; j < triangleCount; j++)
            {
                dirtyFrames.Clear();
                for (var k = 0; k < 3; k++)
                {
                    totalCpMax[k] += cpMax[k];
                    if (totalCpMax[k] > DysonSphereConstants.TotalCpMaxCeiling)
                    {
                        totalCpMax[k] = cpMax[k];
                        dirtyFrames.Add(k > 0 ? k - 1 : 2);
                        dirtyFrames.Add(k);
                        nodes[k] = layer.QuickAddDysonNode(0, nodePos[k]);
                    }
                }
                foreach (var frameId in dirtyFrames)
                {
                    frames[frameId] = layer.QuickAddDysonFrame(0, nodes[frameId], nodes[(frameId + 1) % 3], false);
                }
                layer.QuickAddDysonShell(0, nodes, frames, false);
            }
            foreach (var node in nodes)
            {
                node.RecalcSpReq();
                node.RecalcCpReq();
            }
            FinalizeDysonSphereChanges(dysonSphere, layer, true);
            return;
        }
    }

    private static bool CreateIllegalDysonShellWithMaxOutputForLayer(DysonSphereLayer layer)
    {
        EnsureDysonShellMaps();
        var shellsChanged = false;
        var mutex = new object();
        var supposedShells = new List<SupposedShell>(DysonSphereConstants.MaxShellSearchCombinations);
        Vector3[] nodePos = new Vector3[DysonSphereConstants.MaxShellSearchNodeCount];
        for (var i = 0; i < DysonSphereConstants.MaxShellSearchNodeCount; i++)
        {
            nodePos[i] = new Vector3((float)Math.Sin(Math.PI * 2 * i / DysonSphereConstants.MaxShellSearchNodeCount), 0, (float)Math.Cos(Math.PI * 2 * i / DysonSphereConstants.MaxShellSearchNodeCount)) * layer.orbitRadius;
        }
        for (var i = 0; i < DysonSphereConstants.MaxShellSearchNodeCount - 2; i++)
        {
            for (var j = i + 1; j < DysonSphereConstants.MaxShellSearchNodeCount - 1; j++)
            {
                for (var k = j + 1; k < DysonSphereConstants.MaxShellSearchNodeCount; k++)
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

        var gridScale = (int)(Math.Pow(layer.orbitRadius / DysonSphereConstants.GridScaleBaseRadius, 0.75) + 0.5);
        gridScale = (gridScale < 1) ? 1 : gridScale;
        var cpPerVertex = gridScale * gridScale * DysonSphereConstants.CpPerVertexFactor;
        var barrier = DysonSphereConstants.TotalCpMaxCeiling / cpPerVertex;
        if (barrier > DysonSphereConstants.MaxShellVertices) barrier = DysonSphereConstants.MaxShellVertices;
        var truncValue = barrier / 1000 * 1000;
        CheatEnabler.Logger.LogDebug($"cpPerVertex: {cpPerVertex}, Barrier: {barrier}, TruncValue: {truncValue}");

        Parallel.For(0, supposedShells.Count, options, (j, loopState) =>
        {
            var sshell = supposedShells[j];
            var vertCount = GeometryHelpers.CalculateTriangleVertCount([sshell.posA, sshell.posB, sshell.posC]);
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
            ResetLayerPools(layer);
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
        return shellsChanged;
    }

    public static void CreateIllegalDysonShellWithMaxOutput()
    {
        var resolved = DysonSphereResolver.ResolveEditorOrLocalSphere(requireLayer: false);
        if (resolved == null) return;
        var (dysonSphere, star) = resolved.Value;
        UXAssist.Functions.DysonSphereFunctions.InitCurrentDysonLayer(star, 0);
        var layer = dysonSphere.layersIdBased[1];
        if (layer != null)
        {
            dysonSphere.RemoveLayer(1);
        }
        var maxOrbitRadius = Patches.DysonSpherePatch.UnlockMaxOrbitRadiusEnabled.Value ? Patches.DysonSpherePatch.UnlockMaxOrbitRadiusValue.Value : dysonSphere.maxOrbitRadius;
        layer = dysonSphere.AddLayerOnId(1, maxOrbitRadius, Quaternion.Euler(0f, 0f, 0f), Mathf.Sqrt(dysonSphere.gravity / maxOrbitRadius) / maxOrbitRadius * DysonSphereConstants.RadiansToDegrees);
        if (layer == null) return;

        var shellsChanged = CreateIllegalDysonShellWithMaxOutputForLayer(layer);

        FinalizeDysonSphereChanges(dysonSphere, layer, shellsChanged, true);
    }

    public static void CreateIllegalDysonShellWithMaxOutputForAllLayers()
    {
        var resolved = DysonSphereResolver.ResolveEditorOrLocalSphere(requireLayer: false);
        if (resolved == null) return;
        var (dysonSphere, _) = resolved.Value;
        var shellsChanged = false;
        for (int i = dysonSphere.layersSorted.Length - 1; i >= 0; i--)
        {
            var layer = dysonSphere.layersSorted[i];
            if (layer == null) continue;
            shellsChanged = CreateIllegalDysonShellWithMaxOutputForLayer(layer) || shellsChanged;
        }

        FinalizeDysonSphereChanges(dysonSphere, dysonSphere.layersSorted.FirstOrDefault(l => l != null), shellsChanged, true);
    }

    public static void CreateIllegalDysonShellsSpecially()
    {
        var lastGridScale = 0;
        var radiusList = new List<int>();
        for (var r = DysonSphereConstants.MinOrbitRadiusSearch; r <= DysonSphereConstants.MaxOrbitRadius; r++)
        {
            var gridScale = (int)(Math.Pow(r / DysonSphereConstants.GridScaleBaseRadius, 0.75) + 0.5);
            gridScale = (gridScale < 1) ? 1 : gridScale;
            if (gridScale == lastGridScale) continue;
            lastGridScale = gridScale;
            radiusList.Add(r);
            CheatEnabler.Logger.LogDebug($"Grid Scale: {gridScale} from {r}");
        }
        radiusList.Add(DysonSphereConstants.MaxOrbitRadius);
        var resolved = DysonSphereResolver.ResolveEditorOrLocalSphere(requireLayer: false);
        if (resolved == null) return;
        var (dysonSphere, star) = resolved.Value;
        UXAssist.Functions.DysonSphereFunctions.InitCurrentDysonLayer(star, 0);

        EnsureDysonShellMaps();
        var shellsChanged = false;
        var mutex = new object();

        for (var idx = 1; idx <= 2; idx++)
        {
            Dictionary<(int, int), int> availableFrames = [];
            HashSet<int> unusedFrameIds = [];
            var layer = dysonSphere.layersIdBased[idx];
            if (layer != null)
            {
                dysonSphere.RemoveLayer(idx);
            }
            var orbitRadius = (radiusList[idx] - 1) / 10 * 10f;
            layer = dysonSphere.AddLayerOnId(idx, orbitRadius, Quaternion.Euler(0f, 0f, 0f), Mathf.Sqrt(dysonSphere.gravity / orbitRadius) / orbitRadius * DysonSphereConstants.RadiansToDegrees);
            if (layer == null) return;

            var supposedShells = new List<SupposedShell>(DysonSphereConstants.MaxShellSearchCombinations);
            Vector3[] nodePos = new Vector3[DysonSphereConstants.MaxShellSearchNodeCount];
            for (var i = 0; i < DysonSphereConstants.MaxShellSearchNodeCount; i++)
            {
                nodePos[i] = new Vector3((float)Math.Sin(Math.PI * 2 * i / DysonSphereConstants.MaxShellSearchNodeCount), 0, (float)Math.Cos(Math.PI * 2 * i / DysonSphereConstants.MaxShellSearchNodeCount));
            }
            for (var i = 0; i < DysonSphereConstants.MaxShellSearchNodeCount - 2; i++)
            {
                for (var j = i + 1; j < DysonSphereConstants.MaxShellSearchNodeCount - 1; j++)
                {
                    for (var k = j + 1; k < DysonSphereConstants.MaxShellSearchNodeCount; k++)
                    {
                        var area = Vector3.Cross(nodePos[j] - nodePos[i], nodePos[k] - nodePos[i]).sqrMagnitude;
                        supposedShells.Add(new SupposedShell { posA = nodePos[i], posB = nodePos[j], posC = nodePos[k], area = area });
                    }
                }
            }
            supposedShells.Sort((a, b) => b.area.CompareTo(a.area));

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 };

            var gridScale = (int)(Math.Pow(orbitRadius / DysonSphereConstants.GridScaleBaseRadius, 0.75) + 0.5);
            gridScale = (gridScale < 1) ? 1 : gridScale;
            var cpPerVertex = gridScale * gridScale * DysonSphereConstants.CpPerVertexFactor;
            var barrier = DysonSphereConstants.TotalCpMaxCeiling / cpPerVertex;
            if (barrier > 30000) barrier = 30000;

            Parallel.For(0, supposedShells.Count, options, (j, _) =>
            {
                var sshell = supposedShells[j];
                var vertCount = GeometryHelpers.CalculateTriangleVertCount([sshell.posA * orbitRadius, sshell.posB * orbitRadius, sshell.posC * orbitRadius]);
                if (vertCount > barrier)
                {
                    sshell.vertCount = -1;
                }
                else
                {
                    sshell.vertCount = vertCount;
                }
            });
            supposedShells.Sort((a, b) => b.vertCount.CompareTo(a.vertCount));
            for (var j = 0; j < supposedShells.Count && supposedShells[j].vertCount > 0; j++)
            {
                var sshell = supposedShells[j];
                CheatEnabler.Logger.LogDebug($"Checking Triangle {j}[{orbitRadius}] with {sshell.vertCount} vertices");
                var result = Parallel.For((radiusList[idx - 1] + 9) / 10, (radiusList[idx] + 9) / 10, options, (k, loopState) =>
                {
                    var orbitRadius = k * 10f;
                    var gridScale = (int)(Math.Pow(orbitRadius / DysonSphereConstants.GridScaleBaseRadius, 0.75) + 0.5);
                    gridScale = (gridScale < 1) ? 1 : gridScale;
                    var cpPerVertex = gridScale * gridScale * DysonSphereConstants.CpPerVertexFactor;
                    var barrier = DysonSphereConstants.TotalCpMaxCeiling / cpPerVertex;
                    if (barrier > 31000) barrier = 31000;
                    if (loopState.ShouldExitCurrentIteration) return;
                    var vertCount = GeometryHelpers.CalculateTriangleVertCount([sshell.posA * orbitRadius, sshell.posB * orbitRadius, sshell.posC * orbitRadius]);
                    lock (mutex)
                    {
                        if (loopState.ShouldExitCurrentIteration) return;
                        if (vertCount > barrier)
                        {
                            CheatEnabler.Logger.LogDebug($"EXCEEDED: Triangle {j}[{orbitRadius}] has {vertCount} vertices");
                            loopState.Stop();
                        }
                    }
                });
                if (!result.IsCompleted)
                {
                    continue;
                }
                ResetLayerPools(layer);
                DysonNode[] newNodes = [layer.QuickAddDysonNode(0, sshell.posA * orbitRadius), layer.QuickAddDysonNode(0, sshell.posB * orbitRadius), layer.QuickAddDysonNode(0, sshell.posC * orbitRadius)];
                DysonFrame[] newFrames = [layer.QuickAddDysonFrame(0, newNodes[0], newNodes[1], false), layer.QuickAddDysonFrame(0, newNodes[1], newNodes[2], false), layer.QuickAddDysonFrame(0, newNodes[2], newNodes[0], false)];
                layer.QuickAddDysonShell(0, newNodes, newFrames, false);
                foreach (var node in newNodes)
                {
                    node.RecalcSpReq();
                    node.RecalcCpReq();
                }
                shellsChanged = true;
                break;
            }
        }

        FinalizeDysonSphereChanges(dysonSphere, dysonSphere.layersIdBased[2], shellsChanged, true);
    }
}
