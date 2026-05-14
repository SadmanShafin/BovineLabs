using Unity.Burst;
using Unity.Collections;

namespace BovineLabs.Grid.MeshA
{
    [BurstCompile]
    public static class MeshGraphBuilder
    {
        public const int NumHeadings = 8;

        public static bool TryBuild(in PrimitiveSet primSet, Allocator allocator, out MeshGraphData result)
        {
            var mesh = new MeshGraphData(NumHeadings, NumHeadings, allocator);

            for (var theta = 0; theta < NumHeadings; theta++)
            {
                mesh.InitialConfigByTheta[theta] = theta;
                mesh.ThetaByInitialConfig[theta] = theta;
            }

            var tempLists = new NativeArray<NativeList<SuccessorTransition>>(NumHeadings, Allocator.Temp);
            for (var i = 0; i < NumHeadings; i++)
                tempLists[i] = new NativeList<SuccessorTransition>(4, Allocator.Temp);

            for (var theta = 0; theta < NumHeadings; theta++)
                if (primSet.PrimsByHeading.TryGetFirstValue(theta, out var primIdx, out var it))
                    do
                    {
                        var prim = primSet.Primitives[primIdx];
                        var nextConfig = prim.GoalTheta;
                        tempLists[theta].Add(new SuccessorTransition(
                            prim.GoalOffset.x, prim.GoalOffset.y, nextConfig, prim.Id));
                    } while (primSet.PrimsByHeading.TryGetNextValue(out primIdx, ref it));

            var total = 0;
            for (var i = 0; i < NumHeadings; i++) total += tempLists[i].Length;

            mesh.SuccessorsFlat = new NativeArray<SuccessorTransition>(total, allocator);
            var offset = 0;
            for (var i = 0; i < NumHeadings; i++)
            {
                mesh.SuccOffsets[i] = offset;
                mesh.SuccCounts[i] = tempLists[i].Length;
                for (var j = 0; j < tempLists[i].Length; j++)
                    mesh.SuccessorsFlat[offset + j] = tempLists[i][j];
                offset += tempLists[i].Length;
                tempLists[i].Dispose();
            }

            tempLists.Dispose();

            result = mesh;
            return true;
        }
    }
}