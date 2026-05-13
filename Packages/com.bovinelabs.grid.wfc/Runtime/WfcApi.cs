using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

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

        // Steppable state
        public MinHeap ObserveHeap;
        public byte WfcComplete;
    }

    [BurstCompile]
    public unsafe static class WfcApi
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
                WfcComplete = 0,
            };
            return true;
        }

        [BurstCompile]
        public static bool TryInitializeAllPossible(ref WfcState s)
        {
            ulong all = 0UL;
            if (s.PatternCount == 64) all = ulong.MaxValue;
            else all = (1UL << s.PatternCount) - 1;

            int len = s.Grid.Length;
            ulong* possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();
            int* entropyPtr = (int*)s.Entropy.GetUnsafePtr();

            for (int i = 0; i < len; i++)
            {
                possiblePtr[i] = all;
                entropyPtr[i] = s.PatternCount;
            }
            s.Queue.Clear();
            s.Dirty.Fill((byte)0);
            return true;
        }

        [BurstCompile]
        public static bool TryLearnAdjacency(ref WfcState s, in NativeArray<int> sample, int sampleWidth, int sampleHeight)
        {
            s.Compatibility.Fill(0UL);
            int* samplePtr = (int*)sample.GetUnsafeReadOnlyPtr();
            ulong* compatibilityPtr = (ulong*)s.Compatibility.GetUnsafePtr();

            for (int y = 0; y < sampleHeight; y++)
            {
                for (int x = 0; x < sampleWidth; x++)
                {
                    int pattern = samplePtr[y * sampleWidth + x];
                    if (Hint.Unlikely(pattern < 0 || pattern >= s.PatternCount)) continue;

                    for (int d = 0; d < 4; d++)
                    {
                        int2 offset = Grid2D.Dir4(d);
                        int nx = x + offset.x;
                        int ny = y + offset.y;
                        if (Hint.Unlikely(nx < 0 || ny < 0 || nx >= sampleWidth || ny >= sampleHeight)) continue;
                        
                        int neighbor = samplePtr[ny * sampleWidth + nx];
                        if (Hint.Unlikely(neighbor < 0 || neighbor >= s.PatternCount)) continue;
                        compatibilityPtr[pattern * 4 + d] |= 1UL << neighbor;
                    }
                }
            }
            return true;
        }

        [BurstCompile]
        public static bool TryObserve(ref WfcState s, int cell, int chosenPattern)
        {
            if (Hint.Unlikely(!s.Grid.InBounds(cell) || (uint)chosenPattern >= (uint)s.PatternCount)) return false;
            ulong mask = 1UL << chosenPattern;
            s.PossibleBits[cell] = mask;
            s.Entropy[cell] = 1;
            s.Queue.Enqueue(cell);
            return true;
        }

        [BurstCompile]
        public static bool TryPropagate(ref WfcState s)
        {
            int width = s.Grid.Width;
            int height = s.Grid.Height;
            ulong* possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();
            int* entropyPtr = (int*)s.Entropy.GetUnsafePtr();
            ulong* compatibilityPtr = (ulong*)s.Compatibility.GetUnsafeReadOnlyPtr();
            byte* dirtyPtr = (byte*)s.Dirty.GetUnsafePtr();

            while (s.Queue.TryDequeue(out int cell))
            {
                int y = cell / width;
                int x = cell % width;
                ulong cellPossible = possiblePtr[cell];

                for (int d = 0; d < 4; d++)
                {
                    int2 offset = Grid2D.Dir4(d);
                    int nx = x + offset.x;
                    int ny = y + offset.y;
                    if (Hint.Unlikely(nx < 0 || ny < 0 || nx >= width || ny >= height)) continue;
                    
                    int ni = ny * width + nx;
                    ulong niPossible = possiblePtr[ni];

                    ulong unionPossible = 0UL;
                    ulong temp = cellPossible;
                    while (temp != 0)
                    {
                        int cp = math.tzcnt(temp);
                        unionPossible |= compatibilityPtr[cp * 4 + d];
                        temp &= ~(1UL << cp);
                    }

                    ulong restricted = niPossible & unionPossible;
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

        /// <summary>
        /// Monolithic run — calls Init then steps until done.
        /// Preserved for backward compatibility with existing tests.
        /// </summary>
        [BurstCompile]
        public static bool TryRun(ref WfcState s, ref NativeArray<int> output, ref Unity.Mathematics.Random rng)
        {
            if (!TryInitWfc(ref s)) return false;

            while (s.WfcComplete == 0)
                TryObserveStep(ref s, ref rng);

            if (s.WfcComplete == 2) return false;

            int len = s.Grid.Length;
            int* outputPtr = (int*)output.GetUnsafePtr();
            ulong* possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();
            for (int i = 0; i < len; i++)
            {
                ulong bits = possiblePtr[i];
                outputPtr[i] = bits == 0 ? -1 : math.tzcnt(bits);
            }
            return true;
        }

        /// <summary>
        /// Initialize WFC: set all cells to all-possible, build observe heap.
        /// After calling this, use TryObserveStep for frame-by-frame execution.
        /// </summary>
        [BurstCompile]
        public static bool TryInitWfc(ref WfcState s)
        {
            if (!TryInitializeAllPossible(ref s)) return false;

            s.ObserveHeap.Clear();
            s.WfcComplete = 0;

            int len = s.Grid.Length;
            int* entropyPtr = (int*)s.Entropy.GetUnsafePtr();

            for (int i = 0; i < len; i++)
                if (entropyPtr[i] > 1)
                    if (!s.ObserveHeap.TryInsertOrDecrease(new HeapNode(i, entropyPtr[i]))) return false;

            return true;
        }

        /// <summary>
        /// Perform one observe-propagate step:
        /// 1. Pop lowest-entropy cell from heap
        /// 2. Choose a random pattern for it
        /// 3. Observe (collapse) that cell
        /// 4. Propagate constraints
        /// Returns true if a step was performed, false if complete or contradiction.
        /// Check s.WfcComplete after: 0=still running, 1=all collapsed, 2=contradiction.
        /// </summary>
        [BurstCompile]
        public static bool TryObserveStep(ref WfcState s, ref Unity.Mathematics.Random rng)
        {
            if (s.WfcComplete != 0) return false;

            int len = s.Grid.Length;
            int* entropyPtr = (int*)s.Entropy.GetUnsafePtr();
            ulong* possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();

            // Find next uncollapsed cell
            while (!s.ObserveHeap.IsEmpty)
            {
                if (!s.ObserveHeap.TryPop(out var top)) { s.WfcComplete = 1; return false; }
                int bestCell = top.Id;
                int e = entropyPtr[bestCell];
                if (e <= 1) continue;
                if (possiblePtr[bestCell] == 0UL) { s.WfcComplete = 2; return false; }

                // Choose a random pattern
                ulong possible = possiblePtr[bestCell];
                int count = e;
                int chosen = rng.NextInt(0, count);
                int pattern = -1;
                ulong temp = possible;
                for (int i = 0; i <= chosen; i++)
                {
                    pattern = math.tzcnt(temp);
                    temp &= ~(1UL << pattern);
                }

                TryObserve(ref s, bestCell, pattern);
                if (!TryPropagate(ref s)) { s.WfcComplete = 2; return false; }

                // Re-enqueue dirty cells
                byte* dirtyPtr = (byte*)s.Dirty.GetUnsafePtr();
                for (int i = 0; i < len; i++)
                {
                    if (dirtyPtr[i] != 0)
                    {
                        dirtyPtr[i] = 0;
                        if (entropyPtr[i] > 1)
                            if (!s.ObserveHeap.TryInsertOrDecrease(new HeapNode(i, entropyPtr[i]))) return false;
                    }
                }

                return true;
            }

            // Heap empty — all cells collapsed
            s.WfcComplete = 1;
            return false;
        }

        /// <summary>
        /// After WFC is complete (WfcComplete == 1), extract the output map.
        /// </summary>
        [BurstCompile]
        public static bool TryExtractOutput(ref WfcState s, ref NativeArray<int> output)
        {
            if (s.WfcComplete != 1) return false;

            int len = s.Grid.Length;
            ulong* possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();
            int* outputPtr = (int*)output.GetUnsafePtr();
            for (int i = 0; i < len; i++)
            {
                ulong bits = possiblePtr[i];
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
