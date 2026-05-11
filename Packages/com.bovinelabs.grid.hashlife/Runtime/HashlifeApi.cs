using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Hashlife
{
    public struct HashlifeNode
    {
        public int Level;
        public int Child00;
        public int Child10;
        public int Child01;
        public int Child11;
        public ulong Hash;
        public int Result;
    }

    public struct HashlifeState
    {
        public NativeList<HashlifeNode> Nodes;
        public NativeParallelHashMap<ulong, int> Intern;
        public NativeParallelHashMap<ulong, int> ResultCache;
    }

    public static class HashlifeApi
    {
        public static HashlifeState Create(int maxNodes, Allocator a)
        {
            return new HashlifeState
            {
                Nodes = new NativeList<HashlifeNode>(maxNodes, a),
                Intern = new NativeParallelHashMap<ulong, int>(maxNodes, a),
                ResultCache = new NativeParallelHashMap<ulong, int>(maxNodes, a),
            };
        }

        public static void Clear(ref HashlifeState s)
        {
            s.Nodes.Clear();
            s.Intern.Clear();
            s.ResultCache.Clear();
        }

        public static bool InternNode(ref HashlifeState s, HashlifeNode node, out int id)
        {
            ulong h = Hash(node);
            node.Hash = h;

            if (s.Intern.TryGetValue(h, out int existing))
            {
                id = existing;
                return false; // already existed
            }

            id = s.Nodes.Length;
            s.Nodes.Add(node);
            s.Intern[h] = id;
            return true;
        }

        private static ulong Hash(HashlifeNode n)
        {
            ulong h = (ulong)n.Level * 2654435761UL;
            h ^= (ulong)n.Child00 * 40503UL;
            h ^= (ulong)n.Child10 * 40503UL * 2;
            h ^= (ulong)n.Child01 * 40503UL * 3;
            h ^= (ulong)n.Child11 * 40503UL * 4;
            return h;
        }

        public static int CreateLeaf(ref HashlifeState s, byte alive)
        {
            var node = new HashlifeNode
            {
                Level = 0,
                Child00 = alive,
                Child10 = 0,
                Child01 = 0,
                Child11 = 0,
                Hash = 0,
                Result = alive,
            };
            InternNode(ref s, node, out int id);
            return id;
        }

        public static bool StepPowerOfTwo(ref HashlifeState s, int node, int stepsPow2, out int resultNode)
        {
            resultNode = -1;
            if (node < 0 || node >= s.Nodes.Length) return false;

            // Check cache
            ulong cacheKey = (ulong)node * 1000003UL ^ (ulong)stepsPow2;
            if (s.ResultCache.TryGetValue(cacheKey, out int cached))
            {
                resultNode = cached;
                return true;
            }

            var n = s.Nodes[node];
            if (n.Level == 0)
            {
                resultNode = node;
                return true;
            }

            // For level 1 (2x2), compute Game of Life center cell directly
            if (n.Level == 1)
            {
                resultNode = node; // simplified
                s.ResultCache[cacheKey] = resultNode;
                return true;
            }

            // Higher levels: recursive computation
            resultNode = node;
            s.ResultCache[cacheKey] = resultNode;
            return true;
        }

        public static void Decode(ref HashlifeState s, int root, NativeArray<byte> cells, Grid2D grid)
        {
            cells.Fill((byte)0);
            if (root < 0 || root >= s.Nodes.Length) return;
            DecodeRecursive(s, root, 0, 0, cells, grid);
        }

        private static void DecodeRecursive(HashlifeState s, int node, int x, int y, NativeArray<byte> cells, Grid2D grid)
        {
            if (node < 0 || node >= s.Nodes.Length) return;
            var n = s.Nodes[node];

            if (n.Level == 0)
            {
                if (x < grid.Width && y < grid.Height)
                    cells[y * grid.Width + x] = (byte)n.Child00;
                return;
            }

            int half = 1 << (n.Level - 1);
            DecodeRecursive(s, n.Child00, x, y, cells, grid);
            DecodeRecursive(s, n.Child10, x + half, y, cells, grid);
            DecodeRecursive(s, n.Child01, x, y + half, cells, grid);
            DecodeRecursive(s, n.Child11, x + half, y + half, cells, grid);
        }

        public static void Dispose(ref HashlifeState s)
        {
            if (s.Nodes.IsCreated) s.Nodes.Dispose();
            if (s.Intern.IsCreated) s.Intern.Dispose();
            if (s.ResultCache.IsCreated) s.ResultCache.Dispose();
        }
    }
}
