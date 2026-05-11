using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Cbs
{
    public struct AgentTask { public int Start; public int Goal; }

    public struct CbsConstraint { public int Agent; public int Cell; public int CellB; public int Time; public byte IsEdge; }

    public struct CbsNode { public int ConstraintOffset; public int ConstraintCount; public int PathOffset; public int PathCount; public int Cost; public int Parent; }

    public struct CbsState
    {
        public Grid2D Grid;
        public NativeList<CbsNode> Nodes;
        public NativeList<CbsConstraint> Constraints;
        public NativeList<int> FlatPaths;
        public NativeList<RangeI> AgentPathRanges;
        public MinHeap Heap;
    }

    public static class CbsApi
    {
        private const int MaxConstraints = 1000;

        public static CbsState Create(int width, int height, int maxNodes, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new CbsState
            {
                Grid = g,
                Nodes = new NativeList<CbsNode>(maxNodes, a),
                Constraints = new NativeList<CbsConstraint>(MaxConstraints, a),
                FlatPaths = new NativeList<int>(maxNodes * 20, a),
                AgentPathRanges = new NativeList<RangeI>(maxNodes, a),
                Heap = MinHeap.Create(maxNodes, a),
            };
        }



        public static bool Solve(
            ref CbsState s,
            NativeArray<byte> blocked,
            NativeArray<AgentTask> agents,
            NativeList<int> solutionFlatPaths,
            NativeList<RangeI> solutionRanges)
        {
            s.Nodes.Clear();
            s.Constraints.Clear();
            s.FlatPaths.Clear();
            s.AgentPathRanges.Clear();
            s.Heap.Clear();
            solutionFlatPaths.Clear();
            solutionRanges.Clear();

            // Plan each agent independently with A*
            int agentCount = agents.Length;
            var pathLens = new NativeArray<int>(agentCount, Allocator.Temp);

            int totalCost = 0;
            for (int a = 0; a < agentCount; a++)
            {
                int pathStart = s.FlatPaths.Length;
                if (AStar(ref s, blocked, agents[a].Start, agents[a].Goal))
                {
                    // Path was written to a temp, for now just store start/goal
                    s.FlatPaths.Add(agents[a].Start);
                    s.FlatPaths.Add(agents[a].Goal);
                    pathLens[a] = 2;
                    totalCost += 2;
                }
                else
                {
                    pathLens.Dispose();
                    return false;
                }

                s.AgentPathRanges.Add(new RangeI(pathStart, pathLens[a]));
            }

            // Check for conflicts
            // Simplified: no conflict detection in this stub, return direct paths
            for (int i = 0; i < s.FlatPaths.Length; i++)
                solutionFlatPaths.Add(s.FlatPaths[i]);
            for (int i = 0; i < s.AgentPathRanges.Length; i++)
                solutionRanges.Add(s.AgentPathRanges[i]);

            pathLens.Dispose();
            return true;
        }

        private static bool AStar(ref CbsState s, NativeArray<byte> blocked, int start, int goal)
        {
            if (blocked[start] != 0 || blocked[goal] != 0) return false;
            return true; // simplified - actual A* would go here
        }

        public static bool FindFirstConflict(
            NativeList<int> flatPaths,
            NativeList<RangeI> ranges,
            out CbsConstraint conflictA,
            out CbsConstraint conflictB)
        {
            conflictA = default;
            conflictB = default;
            return false;
        }

        public static void Dispose(ref CbsState s)
        {
            if (s.Nodes.IsCreated) s.Nodes.Dispose();
            if (s.Constraints.IsCreated) s.Constraints.Dispose();
            if (s.FlatPaths.IsCreated) s.FlatPaths.Dispose();
            if (s.AgentPathRanges.IsCreated) s.AgentPathRanges.Dispose();
            if (s.Heap.IsCreated) s.Heap.Dispose();
        }
    }
}
