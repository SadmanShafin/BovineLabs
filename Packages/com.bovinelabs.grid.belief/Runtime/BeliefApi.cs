using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Belief
{
    public struct BeliefState
    {
        public Grid2D Grid;
        public int LabelCount;
        public NativeArray<float> Unary;
        public NativeArray<float> Messages;
        public NativeArray<float> MessagesNext;
        public NativeArray<float> Belief;
        public NativeArray<float> Scratch;
    }

    [BurstCompile]
    public unsafe static class BeliefApi
    {
        public static bool TryCreate(int width, int height, int labelCount, Allocator a, out BeliefState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g) || labelCount <= 0)
            {
                result = default;
                return false;
            }

            result = new BeliefState
            {
                Grid = g,
                LabelCount = labelCount,
                Unary = new NativeArray<float>(g.Length * labelCount, a),
                Messages = new NativeArray<float>(g.Length * 4 * labelCount, a),
                MessagesNext = new NativeArray<float>(g.Length * 4 * labelCount, a),
                Belief = new NativeArray<float>(g.Length * labelCount, a),
                Scratch = new NativeArray<float>(labelCount, a),
            };
            return true;
        }

        public static void ClearMessages(ref BeliefState s)
        {
            s.Messages.Fill(0f);
            s.MessagesNext.Fill(0f);
        }

        public static void SetUnary(ref BeliefState s, in NativeArray<float> unary)
        {
            NativeArray<float>.Copy(unary, s.Unary);
        }

        [BurstCompile]
        public static bool TryIterate(ref BeliefState s, in NativeArray<float> pairwise, int iterations)
        {
            if (!pairwise.IsCreated || iterations < 0) return false;

            int L = s.LabelCount;
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int cellCount = w * h;

            // Cache pointers once — no NativeArray indexing inside loops
            float* unaryPtr = (float*)s.Unary.GetUnsafeReadOnlyPtr();
            float* pairwisePtr = (float*)pairwise.GetUnsafeReadOnlyPtr();
            float* messagesPtr = (float*)s.Messages.GetUnsafePtr();
            float* nextMsgPtr = (float*)s.MessagesNext.GetUnsafePtr();
            float* scratchPtr = (float*)s.Scratch.GetUnsafePtr();

            // Message layout: cell * 4 * L + dir * L + label
            // Opposite direction: dir → (dir+2)&3
            // Precomputed opposite direction offsets into the 4-direction array

            for (int iter = 0; iter < iterations; iter++)
            {
                // Clear next messages buffer
                UnsafeUtility.MemSet(nextMsgPtr, 0, (long)cellCount * 4 * L * sizeof(float));

                // Running index for cell iteration
                int cellIdx = 0;
                for (int cy = 0; cy < h; cy++)
                {
                    for (int cx = 0; cx < w; cx++)
                    {
                        // Bounds info: precomputed per cell
                        // Grid2D.Dir4: 0=(1,0), 1=(0,1), 2=(-1,0), 3=(0,-1)
                        bool canDir0 = cx < w - 1;          // (x+1, y)
                        bool canDir1 = cy < h - 1;          // (x, y+1)
                        bool canDir2 = cx > 0;              // (x-1, y)
                        bool canDir3 = cy > 0;              // (x, y-1)

                        // Neighbor cell indices
                        int ni0 = cellIdx + 1;  // dir 0: (x+1, y)
                        int ni1 = cellIdx + w;  // dir 1: (x, y+1)
                        int ni2 = cellIdx - 1;  // dir 2: (x-1, y)
                        int ni3 = cellIdx - w;  // dir 3: (x, y-1)

                        for (int dir = 0; dir < 4; dir++)
                        {
                            int ni;
                            int oppDir = (dir + 2) & 3;

                            if (dir == 0) { if (!canDir0) continue; ni = ni0; }
                            else if (dir == 1) { if (!canDir1) continue; ni = ni1; }
                            else if (dir == 2) { if (!canDir2) continue; ni = ni2; }
                            else { if (!canDir3) continue; ni = ni3; }

                            // For each label v in neighbor, compute min over u of:
                            // unary[cell,u] + sum(messages from dirs != oppDir, label u) + pairwise[u,v]
                            int msgBase = cellIdx * 4 * L;
                            int unaryBase = cellIdx * L;

                            for (int lv = 0; lv < L; lv++)
                            {
                                float best = float.MaxValue;
                                float pairBase = lv; // offset into pairwise[u*L + lv]

                                for (int lu = 0; lu < L; lu++)
                                {
                                    // unary cost
                                    float cost = unaryPtr[unaryBase + lu];

                                    // Sum messages from all directions except oppDir
                                    // Unrolled for 4 directions
                                    if (oppDir != 0) cost += messagesPtr[msgBase + 0 * L + lu];
                                    if (oppDir != 1) cost += messagesPtr[msgBase + 1 * L + lu];
                                    if (oppDir != 2) cost += messagesPtr[msgBase + 2 * L + lu];
                                    if (oppDir != 3) cost += messagesPtr[msgBase + 3 * L + lu];

                                    // Pairwise cost
                                    cost += pairwisePtr[lu * L + pairBase];

                                    if (cost < best) best = cost;
                                }
                                scratchPtr[lv] = best;
                            }

                            // Normalize: subtract min to prevent overflow
                            float minVal = float.MaxValue;
                            for (int lv = 0; lv < L; lv++)
                                if (scratchPtr[lv] < minVal) minVal = scratchPtr[lv];

                            // Write to nextMessages for neighbor ni, direction oppDir
                            int writeIdx = ni * 4 * L + oppDir * L;
                            for (int lv = 0; lv < L; lv++)
                                nextMsgPtr[writeIdx + lv] = scratchPtr[lv] - minVal;
                        }

                        cellIdx++;
                    }
                }

                // Swap buffers (just swap the NativeArray handles)
                var tmp = s.Messages;
                s.Messages = s.MessagesNext;
                s.MessagesNext = tmp;

                messagesPtr = (float*)s.Messages.GetUnsafePtr();
                nextMsgPtr = (float*)s.MessagesNext.GetUnsafePtr();
            }
            return true;
        }

        [BurstCompile]
        public static bool TryDecodeMap(ref BeliefState s, ref NativeArray<int> labels)
        {
            if (!labels.IsCreated || labels.Length < s.Grid.Length) return false;

            int L = s.LabelCount;
            int cellCount = s.Grid.Length;
            float* unaryPtr = (float*)s.Unary.GetUnsafeReadOnlyPtr();
            float* messagesPtr = (float*)s.Messages.GetUnsafeReadOnlyPtr();
            float* beliefPtr = (float*)s.Belief.GetUnsafePtr();
            int* labelsPtr = (int*)labels.GetUnsafePtr();

            for (int cell = 0; cell < cellCount; cell++)
            {
                float bestCost = float.MaxValue;
                int bestLabel = 0;
                int unaryBase = cell * L;
                int msgBase = cell * 4 * L;

                for (int l = 0; l < L; l++)
                {
                    float cost = unaryPtr[unaryBase + l]
                        + messagesPtr[msgBase + 0 * L + l]
                        + messagesPtr[msgBase + 1 * L + l]
                        + messagesPtr[msgBase + 2 * L + l]
                        + messagesPtr[msgBase + 3 * L + l];

                    beliefPtr[unaryBase + l] = cost;
                    if (cost < bestCost) { bestCost = cost; bestLabel = l; }
                }
                labelsPtr[cell] = bestLabel;
            }
            return true;
        }

        public static void Dispose(ref BeliefState s)
        {
            if (s.Unary.IsCreated) s.Unary.Dispose();
            if (s.Messages.IsCreated) s.Messages.Dispose();
            if (s.MessagesNext.IsCreated) s.MessagesNext.Dispose();
            if (s.Belief.IsCreated) s.Belief.Dispose();
            if (s.Scratch.IsCreated) s.Scratch.Dispose();
        }
    }
}
