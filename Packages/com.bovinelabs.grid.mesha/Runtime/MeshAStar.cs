using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.MeshA
{
    [BurstCompile]
    public static class MeshAStar
    {
        public static PathResult FindPath(
            in NativeGrid2D grid,
            in PrimitiveSet primSet,
            in MeshGraphData meshGraph,
            int2 start,
            int2 goal,
            int startTheta = 0,
            float weight = 1.0f,
            Allocator allocator = Allocator.Temp)
        {
            TryFindPath(grid, primSet, meshGraph, start, goal, out var result, startTheta, weight, allocator);
            return result;
        }

        public static bool TryFindPath(
            in NativeGrid2D grid,
            in PrimitiveSet primSet,
            in MeshGraphData meshGraph,
            int2 start,
            int2 goal,
            out PathResult result,
            int startTheta = 0,
            float weight = 1.0f,
            Allocator allocator = Allocator.Temp)
        {
            result = new PathResult(allocator);
            var found = new NativeReference<bool>(allocator);
            var pathCost = new NativeReference<float>(allocator);
            var nodesExplored = new NativeReference<int>(allocator);

            var job = new MeshAStarJob
            {
                Grid = grid,
                MeshGraph = meshGraph,
                PrimSet = primSet,
                Start = start,
                Goal = goal,
                StartTheta = startTheta,
                Weight = weight,
                Path = result.Path,
                Found = found,
                PathCost = pathCost,
                NodesExplored = nodesExplored
            };

            job.Execute();

            result.Found = found.Value;
            result.PathCost = pathCost.Value;
            result.NodesExplored = nodesExplored.Value;

            var success = found.Value;

            found.Dispose();
            pathCost.Dispose();
            nodesExplored.Dispose();

            return success;
        }
    }
}