using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.GraphCut;

namespace BovineLabs.Grid.DynamicCut
{
    public struct DynamicCutState
    {
        public GraphCutState Cut;
        public NativeList<int> DirtyNodes;
        public NativeList<int> Orphans;
    }

    public static class DynamicCutApi
    {
        public static DynamicCutState Create(int width, int height, int maxEdges, Allocator a)
        {
            return new DynamicCutState
            {
                Cut = GraphCutApi.Create(width, height, maxEdges, a),
                DirtyNodes = new NativeList<int>(width * height, a),
                Orphans = new NativeList<int>(width * height, a),
            };
        }

        public static void EditUnary(ref DynamicCutState s, int cell, int sourceCapDelta, int sinkCapDelta)
        {
            s.DirtyNodes.Add(cell);
        }

        public static void EditPairwise(ref DynamicCutState s, int a, int b, int capacityDelta)
        {
            s.DirtyNodes.Add(a);
            s.DirtyNodes.Add(b);
        }

        public static bool Repair(ref DynamicCutState s)
        {
            bool result = GraphCutApi.MinCut(ref s.Cut);
            s.DirtyNodes.Clear();
            s.Orphans.Clear();
            return result;
        }

        public static void Dispose(ref DynamicCutState s)
        {
            GraphCutApi.Dispose(ref s.Cut);
            if (s.DirtyNodes.IsCreated) s.DirtyNodes.Dispose();
            if (s.Orphans.IsCreated) s.Orphans.Dispose();
        }
    }
}
