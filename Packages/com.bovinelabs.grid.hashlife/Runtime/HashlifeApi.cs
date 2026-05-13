using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Hashlife
{
    public struct HashlifeNode
    {
        public int Level;
        public int ChildNW;
        public int ChildNE;
        public int ChildSW;
        public int ChildSE;
        public ulong Hash;
    }

    public struct HashlifeState
    {
        public UnsafeList<HashlifeNode> Nodes;
        public NativeParallelHashMap<ulong, int> Intern;
        public NativeParallelHashMap<ulong, int> ResultCache;
    }

    [BurstCompile]
    public unsafe static class HashlifeApi
    {
        public static bool TryCreate(int maxNodes, Allocator a, out HashlifeState result)
        {
            result = new HashlifeState
            {
                Nodes = new UnsafeList<HashlifeNode>(maxNodes, a),
                Intern = new NativeParallelHashMap<ulong, int>(maxNodes, a),
                ResultCache = new NativeParallelHashMap<ulong, int>(maxNodes, a),
            };

            CreateLeaf(ref result, 0, out _);
            CreateLeaf(ref result, 1, out _);
            return true;
        }

        private static bool CreateLeaf(ref HashlifeState s, byte alive, out int id)
        {
            var node = new HashlifeNode
            {
                Level = 0,
                ChildNW = alive,
                ChildNE = 0,
                ChildSW = 0,
                ChildSE = 0,
                Hash = 0,
            };
            return TryInternNode(ref s, ref node, out id);
        }

        [BurstCompile]
        public static bool TryMakeNode(ref HashlifeState s, int nw, int ne, int sw, int se, out int id)
        {
            int level = s.Nodes[nw].Level + 1;
            var node = new HashlifeNode
            {
                Level = level,
                ChildNW = nw,
                ChildNE = ne,
                ChildSW = sw,
                ChildSE = se,
                Hash = 0,
            };
            return TryInternNode(ref s, ref node, out id);
        }

        [BurstCompile]
        private static bool TryInternNode(ref HashlifeState s, ref HashlifeNode node, out int id)
        {
            ulong h = Hash(node);
            node.Hash = h;

            if (s.Intern.TryGetValue(h, out int existing))
            {
                id = existing;
                return true;
            }

            id = s.Nodes.Length;
            s.Nodes.Add(node);
            s.Intern[h] = id;
            return true;
        }

        private static ulong Hash(HashlifeNode n)
        {
            ulong h = (ulong)n.Level * 2654435761UL;
            h ^= (ulong)n.ChildNW * 40503UL;
            h ^= (ulong)n.ChildNE * 40503UL * 2;
            h ^= (ulong)n.ChildSW * 40503UL * 3;
            h ^= (ulong)n.ChildSE * 40503UL * 4;
            return h;
        }

        [BurstCompile]
        public static bool TryGetResult(ref HashlifeState s, int nodeIdx, out int result)
        {
            if (s.ResultCache.TryGetValue((ulong)nodeIdx, out result))
                return true;

            HashlifeNode* nodes = s.Nodes.Ptr;
            HashlifeNode node = nodes[nodeIdx];

            if (node.Level == 2)
            {
                if (!ComputeLevel2(ref s, nodeIdx, out result)) return false;
                s.ResultCache[(ulong)nodeIdx] = result;
                return true;
            }

            HashlifeNode nNW = nodes[node.ChildNW], nNE = nodes[node.ChildNE], nSW = nodes[node.ChildSW], nSE = nodes[node.ChildSE];

            int m00 = node.ChildNW;
            TryMakeNode(ref s, nNW.ChildNE, nNE.ChildNW, nNW.ChildSE, nNE.ChildSW, out int m10);
            int m20 = node.ChildNE;
            TryMakeNode(ref s, nNW.ChildSW, nNW.ChildSE, nSW.ChildNW, nSW.ChildNE, out int m01);
            TryMakeNode(ref s, nNW.ChildSE, nNE.ChildSW, nSW.ChildNE, nSE.ChildNW, out int m11);
            TryMakeNode(ref s, nNE.ChildSW, nNE.ChildSE, nSE.ChildNW, nSE.ChildNE, out int m21);
            int m02 = node.ChildSW;
            TryMakeNode(ref s, nSW.ChildNE, nSE.ChildNW, nSW.ChildSE, nSE.ChildNE, out int m12);
            int m22 = node.ChildSE;

            TryGetResult(ref s, m00, out int c00);
            TryGetResult(ref s, m10, out int c10);
            TryGetResult(ref s, m20, out int c20);
            TryGetResult(ref s, m01, out int c01);
            TryGetResult(ref s, m11, out int c11);
            TryGetResult(ref s, m21, out int c21);
            TryGetResult(ref s, m02, out int c02);
            TryGetResult(ref s, m12, out int c12);
            TryGetResult(ref s, m22, out int c22);

            TryMakeNode(ref s, c00, c10, c01, c11, out int n00);
            TryMakeNode(ref s, c10, c20, c11, c21, out int n10);
            TryMakeNode(ref s, c01, c11, c02, c12, out int n01);
            TryMakeNode(ref s, c11, c21, c12, c22, out int n11);

            TryGetResult(ref s, n00, out int r00);
            TryGetResult(ref s, n10, out int r10);
            TryGetResult(ref s, n01, out int r01);
            TryGetResult(ref s, n11, out int r11);

            TryMakeNode(ref s, r00, r10, r01, r11, out result);

            s.ResultCache[(ulong)nodeIdx] = result;
            return true;
        }

        private static bool ComputeLevel2(ref HashlifeState s, int nodeIdx, out int result)
        {
            byte* grid = stackalloc byte[16];
            HashlifeNode* nodes = s.Nodes.Ptr;
            HashlifeNode n = nodes[nodeIdx];
            
            Extract2x2(nodes, n.ChildNW, grid, 0, 0, 4);
            Extract2x2(nodes, n.ChildNE, grid, 2, 0, 4);
            Extract2x2(nodes, n.ChildSW, grid, 0, 2, 4);
            Extract2x2(nodes, n.ChildSE, grid, 2, 2, 4);

            byte rNW = StepCell(grid, 1, 1, 4);
            byte rNE = StepCell(grid, 2, 1, 4);
            byte rSW = StepCell(grid, 1, 2, 4);
            byte rSE = StepCell(grid, 2, 2, 4);

            return TryMakeNode(ref s, rNW, rNE, rSW, rSE, out result);
        }

        private static void Extract2x2(HashlifeNode* nodes, int nodeIdx, byte* dst, int ox, int oy, int stride)
        {
            HashlifeNode n = nodes[nodeIdx];
            dst[oy * stride + ox] = (byte)nodes[n.ChildNW].ChildNW;
            dst[oy * stride + ox + 1] = (byte)nodes[n.ChildNE].ChildNW;
            dst[(oy + 1) * stride + ox] = (byte)nodes[n.ChildSW].ChildNW;
            dst[(oy + 1) * stride + ox + 1] = (byte)nodes[n.ChildSE].ChildNW;
        }

        private static byte StepCell(byte* grid, int x, int y, int stride)
        {
            int alive = 0;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (dx != 0 || dy != 0)
                        alive += grid[(y + dy) * stride + (x + dx)];

            byte current = grid[y * stride + x];
            if (current == 1) return (byte)(alive == 2 || alive == 3 ? 1 : 0);
            return (byte)(alive == 3 ? 1 : 0);
        }

        public static void Decode(ref HashlifeState s, int root, NativeArray<byte> cells, Grid2D grid)
        {
            cells.Fill((byte)0);
            if (root < 0 || root >= s.Nodes.Length) return;
            DecodeRecursive(s, root, 0, 0, (byte*)cells.GetUnsafePtr(), grid.Width, grid.Height);
        }

        private static void DecodeRecursive(HashlifeState s, int nodeIdx, int x, int y, byte* cells, int width, int height)
        {
            HashlifeNode n = s.Nodes[nodeIdx];
            if (n.Level == 0)
            {
                if (x < width && y < height)
                    cells[y * width + x] = (byte)n.ChildNW;
                return;
            }

            int half = 1 << (n.Level - 1);
            DecodeRecursive(s, n.ChildNW, x, y, cells, width, height);
            DecodeRecursive(s, n.ChildNE, x + half, y, cells, width, height);
            DecodeRecursive(s, n.ChildSW, x, y + half, cells, width, height);
            DecodeRecursive(s, n.ChildSE, x + half, y + half, cells, width, height);
        }

        public static void Dispose(ref HashlifeState s)
        {
            s.Nodes.Dispose();
            if (s.Intern.IsCreated) s.Intern.Dispose();
            if (s.ResultCache.IsCreated) s.ResultCache.Dispose();
        }
    }
}
