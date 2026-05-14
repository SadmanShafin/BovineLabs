using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.Tests
{
    public class NativeGrid2DTests
    {
        [Test]
        public void Create_WidthHeightSet()
        {
            using var grid = new NativeGrid2D(10, 20, Allocator.Temp);
            Assert.AreEqual(10, grid.Width);
            Assert.AreEqual(20, grid.Height);
            Assert.AreEqual(200, grid.Cells.Length);
        }

        [Test]
        public void InBounds_InsideTrue()
        {
            using var grid = new NativeGrid2D(5, 5, Allocator.Temp);
            Assert.IsTrue(grid.InBounds(new int2(0, 0)));
            Assert.IsTrue(grid.InBounds(new int2(4, 4)));
        }

        [Test]
        public void InBounds_OutsideFalse()
        {
            using var grid = new NativeGrid2D(5, 5, Allocator.Temp);
            Assert.IsFalse(grid.InBounds(new int2(-1, 0)));
            Assert.IsFalse(grid.InBounds(new int2(5, 0)));
            Assert.IsFalse(grid.InBounds(new int2(0, 5)));
        }

        [Test]
        public void SetAndIsFree_Works()
        {
            using var grid = new NativeGrid2D(5, 5, Allocator.Temp);
            Assert.IsTrue(grid.IsFree(new int2(2, 2)));
            grid.Set(2, 2, CellState.Blocked);
            Assert.IsFalse(grid.IsFree(new int2(2, 2)));
        }
    }

    public class GridHeuristicsTests
    {
        [Test]
        public void Euclidean_SamePoint_Zero()
        {
            Assert.AreEqual(0f, GridHeuristics.Euclidean(new int2(5, 5), new int2(5, 5)), 0.001f);
        }

        [Test]
        public void Euclidean_UnitDiagonal()
        {
            Assert.AreEqual(1.4142f, GridHeuristics.Euclidean(new int2(0, 0), new int2(1, 1)), 0.001f);
        }

        [Test]
        public void Octile_Cardinal()
        {
            Assert.AreEqual(3f, GridHeuristics.Octile(new int2(0, 0), new int2(3, 0)), 0.001f);
        }

        [Test]
        public void Octile_Diagonal()
        {
            Assert.AreEqual(1.4142f, GridHeuristics.Octile(new int2(0, 0), new int2(1, 1)), 0.001f);
        }

        [Test]
        public void Manhattan_Correct()
        {
            Assert.AreEqual(5f, GridHeuristics.Manhattan(new int2(0, 0), new int2(3, 2)), 0.001f);
        }
    }

}