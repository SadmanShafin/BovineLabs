using NUnit.Framework;
using Unity.Collections;
using BovineLabs.Grid.Hashlife;

public class HashlifeTests
{
    [Test] public void Create_NotNull()
    { var s = HashlifeApi.Create(1000, Allocator.Temp); Assert.IsTrue(s.Nodes.IsCreated); HashlifeApi.Dispose(ref s); }

    [Test] public void Clear()
    {
        var s = HashlifeApi.Create(1000, Allocator.Temp);
        HashlifeApi.Clear(ref s);
        Assert.AreEqual(0, s.Nodes.Length);
        HashlifeApi.Dispose(ref s);
    }

    [Test] public void CreateLeaf()
    {
        var s = HashlifeApi.Create(1000, Allocator.Temp);
        int id = HashlifeApi.CreateLeaf(ref s, 1);
        Assert.AreEqual(0, id);
        Assert.AreEqual(1, s.Nodes[0].Child00);
        HashlifeApi.Dispose(ref s);
    }

    [Test] public void InternNode_Deduplicates()
    {
        var s = HashlifeApi.Create(1000, Allocator.Temp);
        int id1 = HashlifeApi.CreateLeaf(ref s, 1);
        int id2 = HashlifeApi.CreateLeaf(ref s, 1);
        Assert.AreEqual(id1, id2);
        HashlifeApi.Dispose(ref s);
    }

    [Test] public void Dispose_Double() { var s = HashlifeApi.Create(100, Allocator.Temp); HashlifeApi.Dispose(ref s); HashlifeApi.Dispose(ref s); }
}
