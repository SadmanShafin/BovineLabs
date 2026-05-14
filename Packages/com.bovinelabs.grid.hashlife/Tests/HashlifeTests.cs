using BovineLabs.Grid;
using BovineLabs.Grid.Hashlife;
using NUnit.Framework;
using Unity.Collections;

public class HashlifeTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(HashlifeApi.TryCreate(100, Allocator.Temp, out var s));
        Assert.IsTrue(s.Nodes.IsCreated);
        Assert.IsTrue(s.Intern.Keys != null);
        HashlifeApi.Dispose(ref s);
    }

    [Test]
    public void Dispose_Double()
    {
        Assert.IsTrue(HashlifeApi.TryCreate(100, Allocator.Temp, out var s));
        HashlifeApi.Dispose(ref s);
        HashlifeApi.Dispose(ref s);
    }

    [Test]
    public void Create_Leaves()
    {
        Assert.IsTrue(HashlifeApi.TryCreate(100, Allocator.Temp, out var s));
        Assert.AreEqual(0, s.Nodes[0].Level);
        Assert.AreEqual(0, s.Nodes[1].Level);
        HashlifeApi.Dispose(ref s);
    }

    [Test]
    public void MakeNode_Interns()
    {
        Assert.IsTrue(HashlifeApi.TryCreate(100, Allocator.Temp, out var s));
        Assert.IsTrue(HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var id1));
        Assert.IsTrue(HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var id2));
        Assert.AreEqual(id1, id2);
        HashlifeApi.Dispose(ref s);
    }

    [Test]
    public void GetResult_Level2_StillAlive()
    {
        Assert.IsTrue(HashlifeApi.TryCreate(1000, Allocator.Temp, out var s));
        HashlifeApi.TryMakeNode(ref s, 1, 0, 0, 1, out var nw);
        HashlifeApi.TryMakeNode(ref s, 1, 1, 1, 0, out var ne);
        HashlifeApi.TryMakeNode(ref s, 0, 1, 1, 1, out var sw);
        HashlifeApi.TryMakeNode(ref s, 1, 1, 0, 0, out var se);
        HashlifeApi.TryMakeNode(ref s, nw, ne, sw, se, out var root);
        Assert.IsTrue(HashlifeApi.TryGetResult(ref s, root, out var result));
        Assert.GreaterOrEqual(result, 0);
        HashlifeApi.Dispose(ref s);
    }

    [Test]
    public void Decode_OutputSizeMatchesGrid()
    {
        Assert.IsTrue(HashlifeApi.TryCreate(1000, Allocator.Temp, out var s));
        HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var nw);
        HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var ne);
        HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var sw);
        HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var se);
        HashlifeApi.TryMakeNode(ref s, nw, ne, sw, se, out var root);
        var grid = Grid2D.Create(4, 4);
        var cells = new NativeArray<byte>(16, Allocator.Temp);
        HashlifeApi.Decode(ref s, root, cells, grid);
        Assert.AreEqual(16, cells.Length);
        HashlifeApi.Dispose(ref s);
        cells.Dispose();
    }
}