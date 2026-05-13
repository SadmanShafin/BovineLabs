using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.GraphCut;

namespace BovineLabs.Grid.Domino
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DominoState
    {
        public Grid2D Grid;
        public NativeArray<byte> Region;
        public NativeArray<int> Height;
        public NativeArray<byte> MatchingDir;
    }

    [BurstCompile]
    public unsafe static class DominoApi
    {
        public static bool TryCreate(int width, int height, Allocator a, out DominoState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new DominoState
            {
                Grid = g,
                Region = new NativeArray<byte>(g.Length, a),
                Height = new NativeArray<int>(g.Length, a),
                MatchingDir = new NativeArray<byte>(g.Length, a),
            };
            return true;
        }

        [BurstCompile]
        public static void SetRegion(ref DominoState s, in NativeArray<byte> region)
        {
            UnsafeUtility.MemCpy(s.Region.GetUnsafePtr(), region.GetUnsafeReadOnlyPtr(), s.Grid.Length);
        }

        [BurstCompile]
        public static bool CheckTileableByParity(ref DominoState s)
        {
            byte* region = (byte*)s.Region.GetUnsafePtr();
            int w = s.Grid.Width;
            int len = s.Grid.Length;
            int black = 0, white = 0;
            for (int i = 0; i < len; i++)
            {
                if (region[i] == 0) continue;
                int x = i % w;
                int y = i / w;
                if ((x + y) % 2 == 0) black++;
                else white++;
            }
            return black == white;
        }

        [BurstCompile]
        public static bool TryBuildTilingByMatching(ref DominoState s)
        {
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;
            byte* region = (byte*)s.Region.GetUnsafePtr();
            byte* matchDir = (byte*)s.MatchingDir.GetUnsafePtr();
            UnsafeUtility.MemSet(matchDir, 0, len);

            if (!GraphCutApi.TryCreate(w, h, len * 10, Allocator.Temp, out var cut)) return false;

            cut.EdgeTo.Clear();
            cut.EdgeCap.Clear();
            cut.EdgeFlow.Clear();
            cut.EdgeRev.Clear();
            cut.EdgeNext.Clear();

            int sourceIdx = len;
            int sinkIdx = len + 1;
            int blackCount = 0, whiteCount = 0;

            for (int i = 0; i < len; i++)
            {
                if (region[i] == 0) continue;
                int x = i % w;
                int y = i / w;
                if ((x + y) % 2 == 0) blackCount++;
                else whiteCount++;
            }

            if (blackCount != whiteCount) { GraphCutApi.Dispose(ref cut); return false; }

            for (int i = 0; i < len + 2; i++) cut.EdgeHead[i] = -1;

            for (int i = 0; i < len; i++)
            {
                if (region[i] == 0) continue;
                int x = i % w;
                int y = i / w;

                if ((x + y) % 2 == 0)
                {
                    GraphCutApi.AddEdgeInternal(ref cut, sourceIdx, i, 1);
                    if (x + 1 < w && region[i + 1] != 0)
                        GraphCutApi.AddEdgeInternal(ref cut, i, i + 1, 1);
                    if (x > 0 && region[i - 1] != 0)
                        GraphCutApi.AddEdgeInternal(ref cut, i, i - 1, 1);
                    if (y + 1 < h && region[i + w] != 0)
                        GraphCutApi.AddEdgeInternal(ref cut, i, i + w, 1);
                    if (y > 0 && region[i - w] != 0)
                        GraphCutApi.AddEdgeInternal(ref cut, i, i - w, 1);
                }
                else
                {
                    GraphCutApi.AddEdgeInternal(ref cut, i, sinkIdx, 1);
                }
            }

            if (!GraphCutApi.TryMinCut(ref cut)) { GraphCutApi.Dispose(ref cut); return false; }

            int* head = (int*)cut.EdgeHead.GetUnsafePtr();
            int* to = cut.EdgeTo.Ptr;
            int* flow = cut.EdgeFlow.Ptr;
            int* next = cut.EdgeNext.Ptr;

            int matchedCount = 0;
            for (int e = head[sourceIdx]; e != -1; e = next[e])
            {
                if (flow[e] > 0)
                {
                    int u = to[e];
                    for (int ee = head[u]; ee != -1; ee = next[ee])
                    {
                        if (flow[ee] > 0 && to[ee] < len)
                        {
                            int v = to[ee];
                            matchedCount++;
                            int diff = v - u;
                            if (diff == 1) matchDir[u] = 1;
                            else if (diff == w) matchDir[u] = 2;
                            else if (diff == -1) matchDir[v] = 1;
                            else if (diff == -w) matchDir[v] = 2;
                        }
                    }
                }
            }

            GraphCutApi.Dispose(ref cut);
            return matchedCount == blackCount;
        }

        [BurstCompile]
        public static bool TryBuildHeightFunction(ref DominoState s)
        {
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;
            byte* region = (byte*)s.Region.GetUnsafePtr();
            int* height = (int*)s.Height.GetUnsafePtr();

            var vis = new NativeArray<byte>(len, Allocator.Temp);
            byte* vPtr = (byte*)vis.GetUnsafePtr();
            UnsafeUtility.MemSet(vPtr, 0, len);
            UnsafeUtility.MemSet(height, 0, len * 4);

            var queue = new UnsafeQueue<int>(Allocator.Temp);

            for (int i = 0; i < len; i++)
            {
                if (region[i] == 1) { queue.Enqueue(i); vPtr[i] = 1; break; }
            }

            while (queue.TryDequeue(out int cell))
            {
                int cx = cell % w;
                int cy = cell / w;

                if (cx + 1 < w)
                {
                    int ni = cell + 1;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    { height[ni] = height[cell] + 1; vPtr[ni] = 1; queue.Enqueue(ni); }
                }
                if (cy + 1 < h)
                {
                    int ni = cell + w;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    { height[ni] = height[cell] + 1; vPtr[ni] = 1; queue.Enqueue(ni); }
                }
                if (cx > 0)
                {
                    int ni = cell - 1;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    { height[ni] = height[cell] - 1; vPtr[ni] = 1; queue.Enqueue(ni); }
                }
                if (cy > 0)
                {
                    int ni = cell - w;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    { height[ni] = height[cell] - 1; vPtr[ni] = 1; queue.Enqueue(ni); }
                }
            }

            vis.Dispose();
            queue.Dispose();
            return true;
        }

        public static bool TryFlipAt(ref DominoState s, int cell)
        {
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            if (cell < 0 || cell >= s.MatchingDir.Length || s.MatchingDir[cell] == 0) return false;

            int cx = cell % w;
            int cy = cell / w;

            if (s.MatchingDir[cell] == 1)
            {
                if (cy + 1 >= h || cx + 1 >= w) return false;
                int n1 = cell + 1;
                int n2 = cell + w;
                int n3 = cell + w + 1;
                if (s.MatchingDir[n2] == 1 && s.MatchingDir[n1] == 0 && s.MatchingDir[n3] == 0)
                {
                    s.MatchingDir[cell] = 2;
                    s.MatchingDir[n1] = 2;
                    s.MatchingDir[n2] = 0;
                    return true;
                }
            }
            else if (s.MatchingDir[cell] == 2)
            {
                if (cy + 1 >= h || cx + 1 >= w) return false;
                int n1 = cell + 1;
                int n2 = cell + w;
                int n3 = cell + w + 1;
                if (s.MatchingDir[n1] == 2 && s.MatchingDir[n2] == 0 && s.MatchingDir[n3] == 0)
                {
                    s.MatchingDir[cell] = 1;
                    s.MatchingDir[n2] = 1;
                    s.MatchingDir[n1] = 0;
                    return true;
                }
            }

            return false;
        }

        public static void Dispose(ref DominoState s)
        {
            if (s.Region.IsCreated) s.Region.Dispose();
            if (s.Height.IsCreated) s.Height.Dispose();
            if (s.MatchingDir.IsCreated) s.MatchingDir.Dispose();
        }
    }
}
