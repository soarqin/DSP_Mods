using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UXAssist.Common;

namespace CheatEnabler.Functions.DysonSphere;

public static class GeometryHelpers
{
    const ulong rawNum0 = 0x3FED4D1BA3920BFAUL; // cosr
    const ulong rawNum1 = 0x3FD9B9832ADBFC16UL; // sinr
    const ulong rawNum2 = 0x3FE279A74590331DUL;
    const ulong rawNum3 = 0x3FEBB67AE8584CAAUL; // cos30
    private static readonly double factor0 = RawToDouble(rawNum0);
    private static readonly double factor1 = RawToDouble(rawNum1);
    private static readonly double factor2 = RawToDouble(rawNum2);
    private static readonly double factor3 = RawToDouble(rawNum3);
    public static readonly PrecalculatedTriangle[] PrecalculatedTriangles = [
        new PrecalculatedTriangle() { MaxOrbitRadius = 6869, PosA = new Vector3() { x = RawToFloat(0x00000000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x45D66000U)}, PosB = new Vector3() { x = RawToFloat(0x2C88BEC4U), y = RawToFloat(0x00000000U), z = RawToFloat(0xC5D66000U)}, PosC = new Vector3() { x = RawToFloat(0xC5D66000U), y = RawToFloat(0x00000000U), z = RawToFloat(0xABB1589BU)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 13573, PosA = new Vector3() { x = RawToFloat(0x46540800U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2C87400AU)}, PosB = new Vector3() { x = RawToFloat(0x4652DEA6U), y = RawToFloat(0x00000000U), z = RawToFloat(0xC4B14E71U)}, PosC = new Vector3() { x = RawToFloat(0xC6540800U), y = RawToFloat(0x00000000U), z = RawToFloat(0xAC2F683EU)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 21257, PosA = new Vector3() { x = RawToFloat(0x46A60400U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2CD3CBAAU)}, PosB = new Vector3() { x = RawToFloat(0xC6070C9DU), y = RawToFloat(0x00000000U), z = RawToFloat(0xC697A9AEU)}, PosC = new Vector3() { x = RawToFloat(0xC6A60400U), y = RawToFloat(0x00000000U), z = RawToFloat(0xAC8956FFU)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 29718, PosA = new Vector3() { x = RawToFloat(0x469B4FBEU), y = RawToFloat(0x00000000U), z = RawToFloat(0x46AC7DAAU)}, PosB = new Vector3() { x = RawToFloat(0x46E81C00U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2D140EBCU)}, PosC = new Vector3() { x = RawToFloat(0xC6E81C00U), y = RawToFloat(0x00000000U), z = RawToFloat(0xACC0046AU)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 38834, PosA = new Vector3() { x = RawToFloat(0x4717AE00U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2D4181A3U)}, PosB = new Vector3() { x = RawToFloat(0x45FC49B0U), y = RawToFloat(0x00000000U), z = RawToFloat(0xC7145D79U)}, PosC = new Vector3() { x = RawToFloat(0xC717AE00U), y = RawToFloat(0x00000000U), z = RawToFloat(0xACFAF5D4U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 48523, PosA = new Vector3() { x = RawToFloat(0x473D8800U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2D71CBB9U)}, PosB = new Vector3() { x = RawToFloat(0xC73D8800U), y = RawToFloat(0x00000000U), z = RawToFloat(0xAD1CCB2AU)}, PosC = new Vector3() { x = RawToFloat(0xC72D2539U), y = RawToFloat(0x00000000U), z = RawToFloat(0x469A2DB9U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 58724, PosA = new Vector3() { x = RawToFloat(0x47656000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2D925038U)}, PosB = new Vector3() { x = RawToFloat(0xC7656000U), y = RawToFloat(0x00000000U), z = RawToFloat(0xAD3DC153U)}, PosC = new Vector3() { x = RawToFloat(0xC7518B63U), y = RawToFloat(0x00000000U), z = RawToFloat(0x46BA9727U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 69389, PosA = new Vector3() { x = RawToFloat(0x47878200U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2DACE001U)}, PosB = new Vector3() { x = RawToFloat(0xC7878200U), y = RawToFloat(0x00000000U), z = RawToFloat(0xAD603407U)}, PosC = new Vector3() { x = RawToFloat(0xC7078200U), y = RawToFloat(0x00000000U), z = RawToFloat(0x476AB4D7U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 80481, PosA = new Vector3() { x = RawToFloat(0x479D3000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2DC88874U)}, PosB = new Vector3() { x = RawToFloat(0xC79D3000U), y = RawToFloat(0x00000000U), z = RawToFloat(0xAD82095EU)}, PosC = new Vector3() { x = RawToFloat(0xC71D3000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x478820DDU)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 91970, PosA = new Vector3() { x = RawToFloat(0x47B2A01EU), y = RawToFloat(0x00000000U), z = RawToFloat(0x461631C0U)}, PosB = new Vector3() { x = RawToFloat(0x47B39C00U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2DE5234DU)}, PosC = new Vector3() { x = RawToFloat(0xC7B39C00U), y = RawToFloat(0x00000000U), z = RawToFloat(0xAD9495E6U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 103831, PosA = new Vector3() { x = RawToFloat(0x476E65BEU), y = RawToFloat(0x00000000U), z = RawToFloat(0x47A4101EU)}, PosB = new Vector3() { x = RawToFloat(0x47CACB00U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2E015B75U)}, PosC = new Vector3() { x = RawToFloat(0xC7CACB00U), y = RawToFloat(0x00000000U), z = RawToFloat(0xADA7C3C0U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 116040, PosA = new Vector3() { x = RawToFloat(0x47E29F00U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2E108E84U)}, PosB = new Vector3() { x = RawToFloat(0xC7E29F00U), y = RawToFloat(0x00000000U), z = RawToFloat(0xADBB7A19U)}, PosC = new Vector3() { x = RawToFloat(0xC70C0F3EU), y = RawToFloat(0x00000000U), z = RawToFloat(0x47D7878CU)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 128580, PosA = new Vector3() { x = RawToFloat(0x00000000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x47FB1D00U)}, PosB = new Vector3() { x = RawToFloat(0x47A80710U), y = RawToFloat(0x00000000U), z = RawToFloat(0x47BA9D10U)}, PosC = new Vector3() { x = RawToFloat(0x2EA02E04U), y = RawToFloat(0x00000000U), z = RawToFloat(0xC7FB1D00U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 141433, PosA = new Vector3() { x = RawToFloat(0x00000000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x480A1D80U)}, PosB = new Vector3() { x = RawToFloat(0x47A25D3CU), y = RawToFloat(0x00000000U), z = RawToFloat(0x47DF79A3U)}, PosC = new Vector3() { x = RawToFloat(0x2EB03393U), y = RawToFloat(0x00000000U), z = RawToFloat(0xC80A1D80U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 154586, PosA = new Vector3() { x = RawToFloat(0x00000000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x4816F500U)}, PosB = new Vector3() { x = RawToFloat(0x47B175ECU), y = RawToFloat(0x00000000U), z = RawToFloat(0x47F440EDU)}, PosC = new Vector3() { x = RawToFloat(0x2EC0959FU), y = RawToFloat(0x00000000U), z = RawToFloat(0xC816F500U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 168025, PosA = new Vector3() { x = RawToFloat(0x00000000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x48241500U)}, PosB = new Vector3() { x = RawToFloat(0x48241500U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2E51542AU)}, PosC = new Vector3() { x = RawToFloat(0xC8241500U), y = RawToFloat(0x00000000U), z = RawToFloat(0xAE07BD7FU)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 181738, PosA = new Vector3() { x = RawToFloat(0x00000000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x48317880U)}, PosB = new Vector3() { x = RawToFloat(0x48317880U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2E6268D3U)}, PosC = new Vector3() { x = RawToFloat(0xC8317880U), y = RawToFloat(0x00000000U), z = RawToFloat(0xAE12D0F8U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 195715, PosA = new Vector3() { x = RawToFloat(0x469FD288U), y = RawToFloat(0x00000000U), z = RawToFloat(0x483E1379U)}, PosB = new Vector3() { x = RawToFloat(0x483F1F80U), y = RawToFloat(0x00000000U), z = RawToFloat(0x2E73D398U)}, PosC = new Vector3() { x = RawToFloat(0xC83F1F80U), y = RawToFloat(0x00000000U), z = RawToFloat(0xAE1E1C47U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 209946, PosA = new Vector3() { x = RawToFloat(0x00000000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x484D0500U)}, PosB = new Vector3() { x = RawToFloat(0x48092F52U), y = RawToFloat(0x00000000U), z = RawToFloat(0x48185BF5U)}, PosC = new Vector3() { x = RawToFloat(0x2F02C70CU), y = RawToFloat(0x00000000U), z = RawToFloat(0xC84D0500U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 224422, PosA = new Vector3() { x = RawToFloat(0x00000000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x485B2900U)}, PosB = new Vector3() { x = RawToFloat(0x4812A593U), y = RawToFloat(0x00000000U), z = RawToFloat(0x4822DE24U)}, PosC = new Vector3() { x = RawToFloat(0x2F0BCC2BU), y = RawToFloat(0x00000000U), z = RawToFloat(0xC85B2900U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 239136, PosA = new Vector3() { x = RawToFloat(0x00000000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x48698680U)}, PosB = new Vector3() { x = RawToFloat(0x481C424DU), y = RawToFloat(0x00000000U), z = RawToFloat(0x482D8B0EU)}, PosC = new Vector3() { x = RawToFloat(0x2F14F5F7U), y = RawToFloat(0x00000000U), z = RawToFloat(0xC8698680U)} },
        new PrecalculatedTriangle() { MaxOrbitRadius = 250000, PosA = new Vector3() { x = RawToFloat(0x00000000U), y = RawToFloat(0x00000000U), z = RawToFloat(0x48742180U)}, PosB = new Vector3() { x = RawToFloat(0x48235AFEU), y = RawToFloat(0x00000000U), z = RawToFloat(0x48356CB1U)}, PosC = new Vector3() { x = RawToFloat(0x2F1BB9CEU), y = RawToFloat(0x00000000U), z = RawToFloat(0xC8742180U)} },
    ];

    public struct PrecalculatedTriangle
    {
        public int MaxOrbitRadius;
        public Vector3 PosA;
        public Vector3 PosB;
        public Vector3 PosC;
    }

    private static double RawToDouble(ulong value)
    {
        unsafe
        {
            return *(double*)&value;
        }
    }

    private static float RawToFloat(uint value)
    {
        unsafe
        {
            return *(float*)&value;
        }
    }

    public static DysonNode QuickAddDysonNode(this DysonSphereLayer layer, int protoId, Vector3 pos)
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
    public static DysonFrame QuickAddDysonFrame(this DysonSphereLayer layer, int protoId, DysonNode nodeA, DysonNode nodeB, bool euler)
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
    public static int CalculateTriangleVertCount(Vector3[] pos)
    {
        if (pos.Length != 3) return -1;
        VectorLF3[] polygon = [pos[0], pos[1], pos[2]];
        VectorLF3 sum = VectorLF3.zero;
        double num = 0.0;
        for (int i = 0; i < 3; i++)
        {
            double num2 = Vector3.Distance(pos[i], pos[(i + 1) % 3]);
            VectorLF3 vectorLF2 = ((VectorLF3)pos[i] + (VectorLF3)pos[(i + 1) % 3]) * 0.5;
            sum += vectorLF2 * num2;
            num += num2;
        }
        var radius = Math.Round(polygon[0].magnitude * 10.0) / 10.0;
        for (int j = 0; j < polygon.Length; j++)
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
        var gridSize = gridScale * 80f;
        var gridSizeDouble = (double)gridSize;
        var cpPerVertex = gridScale * gridScale * 2;

        var num5 = (int)((double)num3 / factor3 / gridSizeDouble + 2.5);
        var xaxis = VectorLF3.Cross(center, Vector3.up).normalized;
        if (xaxis.magnitude < 0.1)
        {
            xaxis = new VectorLF3(0f, 0f, 1f);
        }
        var yaxis = VectorLF3.Cross(xaxis, center).normalized;
        var raydir = xaxis * factor0 + yaxis * factor1;
        var w1axis = xaxis * (0.5 * gridSizeDouble) - yaxis * (factor3 * gridSizeDouble);
        var w2axis = xaxis * (0.5 * gridSizeDouble) + yaxis * (factor3 * gridSizeDouble);
        var w0axis = xaxis * gridSizeDouble;
        var t1axis = yaxis * (gridSizeDouble * factor2 * 0.5) - xaxis * (gridSizeDouble * 0.5);
        var t2axis = yaxis * (gridSizeDouble * factor2 * 0.5) + xaxis * (gridSizeDouble * 0.5);
        var t0axis = yaxis * (gridSizeDouble / factor3 * 0.5);
        var polyn = new VectorLF3[3];
        var polynu = new double[3];
        for (int l = 0; l < 3; l++)
        {
            polyn[l] = VectorLF3.Cross(polygon[l], polygon[(l + 1) % 3]).normalized;
            polynu[l] = polyn[l].x * raydir.x + polyn[l].y * raydir.y + polyn[l].z * raydir.z;
        }
        var vmap = _vmap.Value;
        vmap.Clear();
        double num7 = gridSizeDouble * 0.5;
        for (int m = -num5; m <= num5; m++)
        {
            for (int n = -num5; n <= num5; n++)
            {
                if (m - n <= num5 && m - n >= -num5)
                {
                    VectorLF3 vectorLF3;
                    vectorLF3.x = center.x + w0axis.x * m - w1axis.x * n;
                    vectorLF3.y = center.y + w0axis.y * m - w1axis.y * n;
                    vectorLF3.z = center.z + w0axis.z * m - w1axis.z * n;
                    double num8 = radius / vectorLF3.magnitude;
                    vectorLF3 *= num8;
                    int num9 = 0;
                    for (int num10 = 0; num10 < 3; num10++)
                    {
                        double num11 = -(polyn[num10].x * vectorLF3.x + polyn[num10].y * vectorLF3.y + polyn[num10].z * vectorLF3.z) / polynu[num10];
                        if (num11 >= 0.0)
                        {
                            VectorLF3 normalized2 = new VectorLF3(vectorLF3.x + num11 * raydir.x, vectorLF3.y + num11 * raydir.y, vectorLF3.z + num11 * raydir.z).normalized;
                            normalized2 *= radius;
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
    public static bool MyGenerateGeometry(this DysonShell shell)
    {
        VectorLF3 sum = VectorLF3.zero;
        double num = 0.0;
        for (int i = 0; i < shell.frames.Count; i++)
        {
            double num2 = Vector3.Distance(shell.frames[i].nodeA.pos, shell.frames[i].nodeB.pos);
            VectorLF3 vectorLF2 = ((VectorLF3)shell.frames[i].nodeA.pos + (VectorLF3)shell.frames[i].nodeB.pos) * 0.5;
            sum += vectorLF2 * num2;
            num += num2;
        }
        shell.radius = Math.Round(shell.polygon[0].magnitude * 10.0) / 10.0;
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
        shell.gridSize = shell.gridScale * 80f;
        var gridSizeDouble = (double)shell.gridSize;
        shell.cpPerVertex = shell.gridScale * shell.gridScale * 2;
        int num5 = (int)((double)num3 / factor3 / gridSizeDouble + 2.5);
        shell.xaxis = VectorLF3.Cross(normalized, Vector3.up).normalized;
        if (shell.xaxis.magnitude < 0.1)
        {
            shell.xaxis = new VectorLF3(0f, 0f, 1f);
        }
        shell.yaxis = VectorLF3.Cross(shell.xaxis, normalized).normalized;
        shell.raydir = shell.xaxis * factor0 + shell.yaxis * factor1;
        shell.w1axis = shell.xaxis * (0.5 * gridSizeDouble) - shell.yaxis * (factor3 * gridSizeDouble);
        shell.w2axis = shell.xaxis * (0.5 * gridSizeDouble) + shell.yaxis * (factor3 * gridSizeDouble);
        shell.w0axis = shell.xaxis * gridSizeDouble;
        shell.t1axis = shell.yaxis * (gridSizeDouble * factor2 * 0.5) - shell.xaxis * (gridSizeDouble * 0.5);
        shell.t2axis = shell.yaxis * (gridSizeDouble * factor2 * 0.5) + shell.xaxis * (gridSizeDouble * 0.5);
        shell.t0axis = shell.yaxis * (gridSizeDouble / factor3 * 0.5);
        int count = shell.polygon.Count;
        shell.polyn = new VectorLF3[count];
        shell.polynu = new double[count];
        for (int l = 0; l < count; l++)
        {
            shell.polyn[l] = VectorLF3.Cross(shell.polygon[l], shell.polygon[(l + 1) % count]).normalized;
            shell.polynu[l] = shell.polyn[l].x * shell.raydir.x + shell.polyn[l].y * shell.raydir.y + shell.polyn[l].z * shell.raydir.z;
        }
        DysonShell.s_vmap.Clear();
        DysonShell.s_outvmap.Clear();
        DysonShell.s_ivmap.Clear();
        double num7 = gridSizeDouble * 0.5;
        for (int m = -num5; m <= num5; m++)
        {
            for (int n = -num5; n <= num5; n++)
            {
                if (m - n <= num5 && m - n >= -num5)
                {
                    VectorLF3 vectorLF3;
                    vectorLF3.x = shell.center.x + shell.w0axis.x * m - shell.w1axis.x * n;
                    vectorLF3.y = shell.center.y + shell.w0axis.y * m - shell.w1axis.y * n;
                    vectorLF3.z = shell.center.z + shell.w0axis.z * m - shell.w1axis.z * n;
                    double num8 = shell.radius / vectorLF3.magnitude;
                    vectorLF3 *= num8;
                    int num9 = 0;
                    for (int num10 = 0; num10 < count; num10++)
                    {
                        double num11 = -(shell.polyn[num10].x * vectorLF3.x + shell.polyn[num10].y * vectorLF3.y + shell.polyn[num10].z * vectorLF3.z) / shell.polynu[num10];
                        if (num11 >= 0.0)
                        {
                            VectorLF3 normalized2 = new VectorLF3(vectorLF3.x + num11 * shell.raydir.x, vectorLF3.y + num11 * shell.raydir.y, vectorLF3.z + num11 * shell.raydir.z).normalized;
                            normalized2 *= shell.radius;
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
            shell.uv2s[num21].x = DysonShell.s_outvmap.ContainsKey(keyValuePair.Key) ? 0 : 1;
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
            shell.uv2s[num31].y = num33;
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
                num48 += num47;
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
                shell.uvs[num61].x = num53;
                shell.uvs[num61].y = (float)(num60 / num59);
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
                        shell.uvs[num62].x = num53;
                        shell.uvs[num62].y = (float)(num60 / num59);
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
    public static int QuickAddDysonShell(this DysonSphereLayer layer, int protoId, DysonNode[] nodes, DysonFrame[] frames, bool limit)
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
        // CheatEnabler.Logger.LogDebug($"Shell {shellId}   My VertCount: {DysonShell.s_vmap.Count}");
        // shell.GenerateGeometry();
        // CheatEnabler.Logger.LogDebug($"Shell {shellId} Orig VertCount: {DysonShell.s_vmap.Count}");
        // CheatEnabler.Logger.LogDebug($"Shell {shellId} Calc VertCount: {CalculateTriangleVertCount2([shell.nodes[0].pos, shell.nodes[1].pos, shell.nodes[2].pos])}");
        for (int j = 0; j < shell.nodes.Count; j++)
        {
            shell.nodes[j].shells.Add(shell);
        }
        shell.GenerateModelObjects();
        return shellId;
    }
    public static void QuickRemoveDysonNode(this DysonSphereLayer layer, int nodeId)
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

    public static void QuickRemoveDysonFrame(this DysonSphereLayer layer, int frameId)
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
    public static int AlignUpToPowerOfTwo(int value)
    {
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }
    public class SupposedShell
    {
        public Vector3 posA;
        public Vector3 posB;
        public Vector3 posC;

        public float area;
        public int vertCount;
    }
}
