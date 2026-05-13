using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Domino;

public class DominoTests
{
    [Test] public void Create_Dimensions()
    { Assert.IsTrue(DominoApi.TryCreate(4, 4, Allocator.Temp, out var s)); Assert.AreEqual(16, s.Grid.Length); DominoApi.Dispose(ref s); }

    [Test] public void CheckTileable_Even()
    {
        Assert.IsTrue(DominoApi.TryCreate(2, 2, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(4, Allocator.Temp); region.Fill((byte)1);
        DominoApi.SetRegion(ref s, region);
        Assert.IsTrue(DominoApi.CheckTileableByParity(ref s));
        DominoApi.Dispose(ref s); region.Dispose();
    }

    [Test] public void CheckTileable_Odd()
    {
        Assert.IsTrue(DominoApi.TryCreate(3, 3, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(9, Allocator.Temp); region.Fill((byte)1);
        DominoApi.SetRegion(ref s, region);
        Assert.IsFalse(DominoApi.CheckTileableByParity(ref s));
        DominoApi.Dispose(ref s); region.Dispose();
    }

    [Test] public void BuildTiling_2x2()
    {
        Assert.IsTrue(DominoApi.TryCreate(2, 2, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(4, Allocator.Temp); region.Fill((byte)1);
        DominoApi.SetRegion(ref s, region);
        Assert.IsTrue(DominoApi.TryBuildTilingByMatching(ref s));
        DominoApi.Dispose(ref s); region.Dispose();
    }

    [Test] public void BuildTiling_4x4()
    {
        Assert.IsTrue(DominoApi.TryCreate(4, 4, Allocator.Temp, out var s));
        var region = new NativeArray<byte>(16, Allocator.Temp); region.Fill((byte)1);
        DominoApi.SetRegion(ref s, region);

        Assert.IsTrue(DominoApi.CheckTileableByParity(ref s));
        DominoApi.Dispose(ref s); region.Dispose();
    }

    [Test] public void Dispose_Double() { Assert.IsTrue(DominoApi.TryCreate(3, 3, Allocator.Temp, out var s)); DominoApi.Dispose(ref s); DominoApi.Dispose(ref s); }
}
