using System;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid
{
    /// <summary>Min-heap for A*/Dijkstra-style priority queues. Supports decrease-key via position array.</summary>
    public struct MinHeap
    {
        public NativeList<HeapNode> Data;
        public NativeArray<int> Positions; // id -> index in Data, -1 if not present
        public bool IsCreated => Data.IsCreated;

        public static MinHeap Create(int maxId, Allocator allocator)
        {
            var h = new MinHeap
            {
                Data = new NativeList<HeapNode>(maxId, allocator),
                Positions = new NativeArray<int>(maxId, allocator),
            };
            h.Positions.Fill(-1);
            return h;
        }

        public int Length => Data.Length;

        public bool IsEmpty => Data.Length == 0;

        public void Clear()
        {
            Data.Clear();
            Positions.Fill(-1);
        }

        public void InsertOrDecrease(HeapNode node)
        {
            int pos = Positions[node.Id];
            if (pos >= 0)
            {
                // Decrease key if better
                if (Less(node, Data[pos]))
                {
                    Data[pos] = node;
                    SiftUp(pos);
                }
            }
            else
            {
                // Insert new
                int idx = Data.Length;
                Data.Add(node);
                Positions[node.Id] = idx;
                SiftUp(idx);
            }
        }

        public HeapNode Pop()
        {
            HeapNode root = Data[0];
            Positions[root.Id] = -1;

            int last = Data.Length - 1;
            if (last > 0)
            {
                Data[0] = Data[last];
                Positions[Data[0].Id] = 0;
                SiftDown(0);
            }

            Data.RemoveAt(last);
            return root;
        }

        public bool Contains(int id) => Positions[id] >= 0;

        public HeapNode Peek() => Data[0];

        public void Dispose()
        {
            if (Data.IsCreated) Data.Dispose();
            if (Positions.IsCreated) Positions.Dispose();
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (!Less(Data[i], Data[parent])) break;
                Swap(i, parent);
                i = parent;
            }
        }

        private void SiftDown(int i)
        {
            int n = Data.Length;
            while (true)
            {
                int left = (i << 1) + 1;
                int right = left + 1;
                int smallest = i;

                if (left < n && Less(Data[left], Data[smallest])) smallest = left;
                if (right < n && Less(Data[right], Data[smallest])) smallest = right;

                if (smallest == i) break;
                Swap(i, smallest);
                i = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            var tmp = Data[a];
            Data[a] = Data[b];
            Data[b] = tmp;
            Positions[Data[a].Id] = a;
            Positions[Data[b].Id] = b;
        }

        private static bool Less(HeapNode a, HeapNode b)
        {
            if (a.Key0 != b.Key0) return a.Key0 < b.Key0;
            return a.Key1 < b.Key1;
        }
    }

    public static class NativeArrayExtensions
    {
        public static void Fill<T>(this NativeArray<T> arr, T value) where T : unmanaged
        {
            for (int i = 0; i < arr.Length; i++)
                arr[i] = value;
        }
    }
}
