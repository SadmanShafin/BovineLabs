using NUnit.Framework;
using Unity.Collections;
using BovineLabs.Grid;
using BovineLabs.Grid.GraphCut;

public class GraphCutTests
{
    [Test] public void Create_Dimensions()
    { Assert.IsTrue(GraphCutApi.TryCreate(5, 5, 100, Allocator.Temp, out var s)); Assert.AreEqual(25, s.Grid.Length); GraphCutApi.Dispose(ref s); }

    [Test] public void Solve_Small()
    {
        Assert.IsTrue(GraphCutApi.TryCreate(2, 1, 10, Allocator.Temp, out var s));
        Assert.IsTrue(GraphCutApi.TrySolve(ref s, 0, 1));
        GraphCutApi.Dispose(ref s);
    }
}
