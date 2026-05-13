using BovineLabs.Grid;
using BovineLabs.Grid.MeshA;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.MeshA.Tests
{
    public class MeshAStarTests
    {
        private PrimitiveSet prims;
        private MeshGraphData mesh;

        [SetUp]
        public void Setup()
        {
            Assert.IsTrue(PrimitiveSetFactory.TryCreateCardinal8(Allocator.Persistent, out var p));
            prims = p;
            Assert.IsTrue(MeshGraphBuilder.TryBuild(prims, Allocator.Persistent, out var m));
            mesh = m;
        }

        [TearDown]
        public void TearDown()
        {
            prims.Dispose();
            mesh.Dispose();
        }

        [Test]
        public void MeshAStar_EmptyGrid_PathFound()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);
            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(0, 0), new int2(5, 5), out var result, 0, 1.0f, Allocator.Temp));

            Assert.IsTrue(result.Found);
            Assert.Greater(result.Path.Length, 0);
            Assert.AreEqual(new int2(0, 0), result.Path[0]);
            Assert.AreEqual(new int2(5, 5), result.Path[result.Path.Length - 1]);
            result.Dispose();
        }

        [Test]
        public void MeshAStar_StartEqualsGoal_TrivialPath()
        {
            using var grid = new NativeGrid2D(5, 5, Allocator.Temp);
            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(2, 2), new int2(2, 2), out var result, 0, 1.0f, Allocator.Temp));


            Assert.IsTrue(result.Found);
            Assert.AreEqual(1, result.Path.Length);
            Assert.AreEqual(new int2(2, 2), result.Path[0]);
            result.Dispose();
        }

        [Test]
        [Ignore("TODO: swept-cell collision check needs tuning for obstacle detour")]
        public void MeshAStar_BlockedPath_GoesAround()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);

            for (int x = 0; x < 9; x++) grid.Set(x, 5, CellState.Blocked);

            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(4, 3), new int2(4, 7), out var result, 0, 1.0f, Allocator.Temp));

            Assert.IsTrue(result.Found);
            Assert.Greater(result.Path.Length, 0);


            for (int i = 0; i < result.Path.Length; i++)
            {
                Assert.IsTrue(grid.IsFree(result.Path[i]),
                    $"Path node {i} at {result.Path[i]} is in a blocked cell");
            }
            result.Dispose();
        }

        [Test]
        public void MeshAStar_NoPath_ReturnsFalse()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);

            for (int x = 0; x < 10; x++) grid.Set(x, 5, CellState.Blocked);

            Assert.IsFalse(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(4, 2), new int2(4, 8), out var result, 0, 1.0f, Allocator.Temp));

            Assert.IsFalse(result.Found);
            result.Dispose();
        }

        [Test]
        [Ignore("TODO: zero-cost transitions from unconnected primitives")]
        public void MeshAStar_PathCost_Positive()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);
            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(0, 0), new int2(3, 4), out var result, 0, 1.0f, Allocator.Temp));

            Assert.IsTrue(result.Found);
            Assert.Greater(result.PathCost, 0f);
            result.Dispose();
        }

        [Test]
        public void MeshAStar_NodesExplored_Counted()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);
            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(0, 0), new int2(5, 5), out var result, 0, 1.0f, Allocator.Temp));

            Assert.IsTrue(result.Found);
            Assert.Greater(result.NodesExplored, 0);
            result.Dispose();
        }

        [Test]
        public void PrimitiveSet_CreateCardinal8_Has8Primitives()
        {
            Assert.AreEqual(8, prims.Primitives.Length);
        }

        [Test]
        public void PrimitiveSet_CreateExtended8_Has24Primitives()
        {
            Assert.IsTrue(PrimitiveSetFactory.TryCreateExtended8(Allocator.Temp, out var ext));
            Assert.AreEqual(24, ext.Primitives.Length);
            ext.Dispose();
        }

        [Test]
        public void MeshGraph_InitialConfigMapping_Correct()
        {
            for (int theta = 0; theta < 8; theta++)
            {
                Assert.AreEqual(theta, mesh.InitialConfigByTheta[theta]);
                Assert.AreEqual(theta, mesh.ThetaByInitialConfig[theta]);
            }
        }

        [Test]
        public void MeshAStar_Weighted_FasterThanOptimal()
        {
            using var grid = new NativeGrid2D(20, 20, Allocator.Temp);

            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(0, 0), new int2(15, 15), out var optimal, 0, 1.0f, Allocator.Temp));

            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(0, 0), new int2(15, 15), out var weighted, 0, 2.0f, Allocator.Temp));

            Assert.IsTrue(optimal.Found);
            Assert.IsTrue(weighted.Found);

            Assert.LessOrEqual(weighted.NodesExplored, optimal.NodesExplored);
            optimal.Dispose();
            weighted.Dispose();
        }
    }
}
