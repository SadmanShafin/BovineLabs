using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wfc
{
    public struct WfcState
    {
        public Grid2D Grid;
        public int PatternCount;
        public NativeArray<ulong> PossibleBits;
        public NativeArray<int> Entropy;
        public NativeArray<ulong> Compatibility;
        public UnsafeQueue<int> Queue;
        public NativeArray<byte> Dirty;

        public MinHeap ObserveHeap;
        public byte WfcComplete;
    }

    [BurstCompile]
    public static unsafe class WfcApi
    {
        public static bool TryCreate(int width, int height, int patternCount, Allocator a, out WfcState s)
        {
            s = default;
            if (patternCount > 64 || patternCount < 1) return false;
            if (!Grid2D.TryCreate(width, height, out var g)) return false;
            if (!MinHeap.TryCreate(g.Length, a, out var heap)) return false;
            s = new WfcState
            {
                Grid = g,
                PatternCount = patternCount,
                PossibleBits = new NativeArray<ulong>(g.Length, a),
                Entropy = new NativeArray<int>(g.Length, a),
                Compatibility = new NativeArray<ulong>(patternCount * 4, a),
                Queue = new UnsafeQueue<int>(a),
                Dirty = new NativeArray<byte>(g.Length, a),
                ObserveHeap = heap,
                WfcComplete = 0
            };
            return true;
        }

        [BurstCompile]
        public static bool TryInitializeAllPossible(ref WfcState s)
        {
            var all = 0UL;
            if (s.PatternCount == 64) all = ulong.MaxValue;
            else all = (1UL << s.PatternCount) - 1;

            var len = s.Grid.Length;
            var possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();
            var entropyPtr = (int*)s.Entropy.GetUnsafePtr();

            for (var i = 0; i < len; i++)
            {
                possiblePtr[i] = all;
                entropyPtr[i] = s.PatternCount;
            }

            s.Queue.Clear();
            s.Dirty.Fill((byte)0);
            return true;
        }

        [BurstCompile]
        public static bool TryLearnAdjacency(ref WfcState s, in NativeArray<int> sample, int sampleWidth,
            int sampleHeight)
        {
            s.Compatibility.Fill(0UL);
            var samplePtr = (int*)sample.GetUnsafeReadOnlyPtr();
            var compatibilityPtr = (ulong*)s.Compatibility.GetUnsafePtr();

            for (var y = 0; y < sampleHeight; y++)
            for (var x = 0; x < sampleWidth; x++)
            {
                var pattern = samplePtr[y * sampleWidth + x];
                if (Hint.Unlikely(pattern < 0 || pattern >= s.PatternCount)) continue;

                for (var d = 0; d < 4; d++)
                {
                    var offset = Grid2D.Dir4(d);
                    var nx = x + offset.x;
                    var ny = y + offset.y;
                    if (Hint.Unlikely(nx < 0 || ny < 0 || nx >= sampleWidth || ny >= sampleHeight)) continue;

                    var neighbor = samplePtr[ny * sampleWidth + nx];
                    if (Hint.Unlikely(neighbor < 0 || neighbor >= s.PatternCount)) continue;
                    compatibilityPtr[pattern * 4 + d] |= 1UL << neighbor;
                }
            }

            return true;
        }

        [BurstCompile]
        public static bool TryObserve(ref WfcState s, int cell, int chosenPattern)
        {
            if (Hint.Unlikely(!s.Grid.InBounds(cell) || (uint)chosenPattern >= (uint)s.PatternCount)) return false;
            var mask = 1UL << chosenPattern;
            s.PossibleBits[cell] = mask;
            s.Entropy[cell] = 1;
            s.Queue.Enqueue(cell);
            return true;
        }

        [BurstCompile]
        public static bool TryPropagate(ref WfcState s)
        {
            var width = s.Grid.Width;
            var height = s.Grid.Height;
            var possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();
            var entropyPtr = (int*)s.Entropy.GetUnsafePtr();
            var compatibilityPtr = (ulong*)s.Compatibility.GetUnsafeReadOnlyPtr();
            var dirtyPtr = (byte*)s.Dirty.GetUnsafePtr();

            while (s.Queue.TryDequeue(out var cell))
            {
                var y = cell / width;
                var x = cell % width;
                var cellPossible = possiblePtr[cell];

                for (var d = 0; d < 4; d++)
                {
                    var offset = Grid2D.Dir4(d);
                    var nx = x + offset.x;
                    var ny = y + offset.y;
                    if (Hint.Unlikely(nx < 0 || ny < 0 || nx >= width || ny >= height)) continue;

                    var ni = ny * width + nx;
                    var niPossible = possiblePtr[ni];

                    var unionPossible = 0UL;
                    var temp = cellPossible;
                    while (temp != 0)
                    {
                        var cp = math.tzcnt(temp);
                        unionPossible |= compatibilityPtr[cp * 4 + d];
                        temp &= ~(1UL << cp);
                    }

                    var restricted = niPossible & unionPossible;
                    if (Hint.Unlikely(restricted == 0UL)) return false;

                    if (restricted != niPossible)
                    {
                        possiblePtr[ni] = restricted;
                        entropyPtr[ni] = math.countbits(restricted);
                        dirtyPtr[ni] = 1;
                        s.Queue.Enqueue(ni);
                    }
                }
            }

            return true;
        }

        [BurstCompile]
        public static bool TryRun(ref WfcState s, ref NativeArray<int> output, ref Random rng)
        {
            if (!TryInitWfc(ref s)) return false;

            while (s.WfcComplete == 0)
                TryObserveStep(ref s, ref rng);

            if (s.WfcComplete == 2) return false;

            var len = s.Grid.Length;
            var outputPtr = (int*)output.GetUnsafePtr();
            var possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();
            for (var i = 0; i < len; i++)
            {
                var bits = possiblePtr[i];
                outputPtr[i] = bits == 0 ? -1 : math.tzcnt(bits);
            }

            return true;
        }

        [BurstCompile]
        public static bool TryInitWfc(ref WfcState s)
        {
            if (!TryInitializeAllPossible(ref s)) return false;

            s.ObserveHeap.Clear();
            s.WfcComplete = 0;

            var len = s.Grid.Length;
            var entropyPtr = (int*)s.Entropy.GetUnsafePtr();

            for (var i = 0; i < len; i++)
                if (entropyPtr[i] > 1)
                    if (!s.ObserveHeap.TryInsertOrDecrease(new HeapNode(i, entropyPtr[i])))
                        return false;

            return true;
        }

        [BurstCompile]
        public static bool TryObserveStep(ref WfcState s, ref Random rng)
        {
            if (s.WfcComplete != 0) return false;

            var len = s.Grid.Length;
            var entropyPtr = (int*)s.Entropy.GetUnsafePtr();
            var possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();

            while (!s.ObserveHeap.IsEmpty)
            {
                if (!s.ObserveHeap.TryPop(out var top))
                {
                    s.WfcComplete = 1;
                    return false;
                }

                var bestCell = top.Id;
                var e = entropyPtr[bestCell];
                if (e <= 1) continue;
                if (possiblePtr[bestCell] == 0UL)
                {
                    s.WfcComplete = 2;
                    return false;
                }

                var possible = possiblePtr[bestCell];
                var count = e;
                var chosen = rng.NextInt(0, count);
                var pattern = -1;
                var temp = possible;
                for (var i = 0; i <= chosen; i++)
                {
                    pattern = math.tzcnt(temp);
                    temp &= ~(1UL << pattern);
                }

                TryObserve(ref s, bestCell, pattern);
                if (!TryPropagate(ref s))
                {
                    s.WfcComplete = 2;
                    return false;
                }

                var dirtyPtr = (byte*)s.Dirty.GetUnsafePtr();
                for (var i = 0; i < len; i++)
                    if (dirtyPtr[i] != 0)
                    {
                        dirtyPtr[i] = 0;
                        if (entropyPtr[i] > 1)
                            if (!s.ObserveHeap.TryInsertOrDecrease(new HeapNode(i, entropyPtr[i])))
                                return false;
                    }

                return true;
            }

            s.WfcComplete = 1;
            return false;
        }

        [BurstCompile]
        public static bool TryExtractOutput(ref WfcState s, ref NativeArray<int> output)
        {
            if (s.WfcComplete != 1) return false;

            var len = s.Grid.Length;
            var possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();
            var outputPtr = (int*)output.GetUnsafePtr();
            for (var i = 0; i < len; i++)
            {
                var bits = possiblePtr[i];
                outputPtr[i] = bits == 0 ? -1 : math.tzcnt(bits);
            }

            return true;
        }

        public static void Dispose(ref WfcState s)
        {
            if (s.PossibleBits.IsCreated) s.PossibleBits.Dispose();
            if (s.Entropy.IsCreated) s.Entropy.Dispose();
            if (s.Compatibility.IsCreated) s.Compatibility.Dispose();
            if (s.Queue.IsCreated) s.Queue.Dispose();
            if (s.Dirty.IsCreated) s.Dirty.Dispose();
            if (s.ObserveHeap.IsCreated) s.ObserveHeap.Dispose();
        }
    }
}