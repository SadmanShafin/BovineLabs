using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BovineLabs.Grid.Sandpile
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SandpileState
    {
        public Grid2D Grid;
        public NativeArray<int> Grains;
        public UnsafeQueue<int> Queue;
        public NativeArray<byte> InQueue;
    }

    [BurstCompile]
    public static unsafe class SandpileApi
    {
        public static bool TryCreate(int width, int height, Allocator allocator, out SandpileState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new SandpileState
            {
                Grid = g,
                Grains = new NativeArray<int>(g.Length, allocator),
                Queue = new UnsafeQueue<int>(allocator),
                InQueue = new NativeArray<byte>(g.Length, allocator)
            };
            return true;
        }

        [BurstCompile]
        public static void Clear(ref SandpileState s)
        {
            var grains = (int*)s.Grains.GetUnsafePtr();
            var inQueue = (byte*)s.InQueue.GetUnsafePtr();
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++)
            {
                grains[i] = 0;
                inQueue[i] = 0;
            }

            s.Queue.Clear();
        }

        [BurstCompile]
        public static void AddGrains(ref SandpileState s, int cell, int amount)
        {
            var grains = (int*)s.Grains.GetUnsafePtr();
            var inQueue = (byte*)s.InQueue.GetUnsafePtr();
            grains[cell] += amount;
            if (Hint.Likely(grains[cell] >= 4) && inQueue[cell] == 0)
            {
                s.Queue.Enqueue(cell);
                inQueue[cell] = 1;
            }
        }

        [BurstCompile]
        public static bool TryRelaxStep(ref SandpileState s)
        {
            if (!s.Queue.TryDequeue(out var cell)) return false;

            var grains = (int*)s.Grains.GetUnsafePtr();
            var inQueue = (byte*)s.InQueue.GetUnsafePtr();
            inQueue[cell] = 0;

            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var cp = s.Grid.ToCoord(cell);
            var toppleCount = grains[cell] / 4;
            grains[cell] %= 4;

            if (Hint.Likely(cp.x + 1 < w))
            {
                var ni = cell + 1;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0)
                {
                    s.Queue.Enqueue(ni);
                    inQueue[ni] = 1;
                }
            }

            if (Hint.Likely(cp.y + 1 < h))
            {
                var ni = cell + w;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0)
                {
                    s.Queue.Enqueue(ni);
                    inQueue[ni] = 1;
                }
            }

            if (Hint.Likely(cp.x > 0))
            {
                var ni = cell - 1;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0)
                {
                    s.Queue.Enqueue(ni);
                    inQueue[ni] = 1;
                }
            }

            if (Hint.Likely(cp.y > 0))
            {
                var ni = cell - w;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0)
                {
                    s.Queue.Enqueue(ni);
                    inQueue[ni] = 1;
                }
            }

            return s.Queue.Count > 0;
        }

        [BurstCompile]
        public static void RelaxAll(ref SandpileState s)
        {
            TryRelaxAll(ref s);
        }

        [BurstCompile]
        public static bool TryRelaxAll(ref SandpileState s)
        {
            while (TryRelaxStep(ref s))
            {
            }

            return true;
        }

        [BurstCompile]
        public static bool IsStable(ref SandpileState s)
        {
            var grains = (int*)s.Grains.GetUnsafePtr();
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++)
                if (grains[i] >= 4)
                    return false;
            return true;
        }

        public static void Dispose(ref SandpileState s)
        {
            if (s.Grains.IsCreated) s.Grains.Dispose();
            if (s.Queue.IsCreated) s.Queue.Dispose();
            if (s.InQueue.IsCreated) s.InQueue.Dispose();
        }
    }
}