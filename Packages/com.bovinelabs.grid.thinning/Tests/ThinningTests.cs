using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Thinning;

public class ThinningTests
{
    [Test] public void Create_Dimensions()
    { var s = ThinningApi.Create(5, 5, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); ThinningApi.Dispose(ref s); }

    [Test] public void IsSimplePoint_Interior()
    {
        var s = ThinningApi.Create(5, 5, Allocator.Temp);
        var solid = new NativeArray<byte>(25, Allocator.Temp);
        solid.Fill((byte)1);
        // In a 5x5 all-solid grid, center is surrounded on all sides
        // Transitions: 0 (no 0->1 transitions since all are 1)
        // So it's NOT simple (would disconnect background)
        // Actually for 5x5 solid, all neighbors are 1 -> transitions = 0 -> not simple
        Assert.IsFalse(ThinningApi.IsSimplePoint(s.Grid, solid, s.Grid.ToIndex(2, 2)));
        ThinningApi.Dispose(ref s); solid.Dispose();
    }

    [Test] public void IsSimplePoint_Border()
    {
        var s = ThinningApi.Create(3, 3, Allocator.Temp);
        var solid = new NativeArray<byte>(9, Allocator.Temp);
        solid.Fill((byte)0);
        // Horizontal line: center has 2 fg neighbors -> transition=2 -> not simple
        // Endpoint: only 1 fg neighbor -> not simple
        // Just verify the method runs without errors
        solid[3] = 1; solid[4] = 1; solid[5] = 1;
        Assert.IsFalse(ThinningApi.IsSimplePoint(s.Grid, solid, 4));
        ThinningApi.Dispose(ref s); solid.Dispose();
    }

    [Test] public void IsSimplePoint_Endpoint()
    {
        var s = ThinningApi.Create(3, 3, Allocator.Temp);
        var solid = new NativeArray<byte>(9, Allocator.Temp);
        solid.Fill((byte)0);
        solid[4] = 1; solid[5] = 1; // Two horizontal cells
        // Cell 5 has only 1 foreground neighbor -> not simple (removing disconnects)
        Assert.IsFalse(ThinningApi.IsSimplePoint(s.Grid, solid, 5));
        ThinningApi.Dispose(ref s); solid.Dispose();
    }

    [Test] public void InitializeFrontier()
    {
        var s = ThinningApi.Create(3, 3, Allocator.Temp);
        var solid = new NativeArray<byte>(9, Allocator.Temp);
        solid.Fill((byte)1);
        ThinningApi.InitializeFrontier(ref s, solid);
        Assert.Greater(s.Frontier.Length, 0);
        ThinningApi.Dispose(ref s); solid.Dispose();
    }

    [Test] public void Iterate_RemovesBorder()
    {
        var s = ThinningApi.Create(5, 5, Allocator.Temp);
        var solid = new NativeArray<byte>(25, Allocator.Temp);
        solid.Fill((byte)1);
        ThinningApi.InitializeFrontier(ref s, solid);
        int removed = ThinningApi.Iterate(ref s, solid);
        Assert.Greater(removed, 0);
        ThinningApi.Dispose(ref s); solid.Dispose();
    }

    [Test] public void Dispose_Double() { var s = ThinningApi.Create(3, 3, Allocator.Temp); ThinningApi.Dispose(ref s); ThinningApi.Dispose(ref s); }
}
