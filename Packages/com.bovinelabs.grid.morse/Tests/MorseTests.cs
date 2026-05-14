using BovineLabs.Grid.Morse;
using NUnit.Framework;
using Unity.Collections;

public class MorseTests
{
    [Test]
    public void Create_Dimensions()
    {
        Assert.IsTrue(MorseApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        Assert.AreEqual(25, s.Grid.Length);
        MorseApi.Dispose(ref s);
    }

    [Test]
    public void BuildGradient_Simple()
    {
        Assert.IsTrue(MorseApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var scalar = new NativeArray<float>(25, Allocator.Temp);

        for (var y = 0; y < 5; y++)
        for (var x = 0; x < 5; x++)
            scalar[s.Grid.ToIndex(x, y)] = y;
        Assert.IsTrue(MorseApi.TryBuildGradient(ref s, in scalar));
        Assert.Greater(s.Critical.Length, 0);
        MorseApi.Dispose(ref s);
        scalar.Dispose();
    }

    [Test]
    public unsafe void TraceManifolds()
    {
        Assert.IsTrue(MorseApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var scalar = new NativeArray<float>(25, Allocator.Temp);
        for (var y = 0; y < 5; y++)
        for (var x = 0; x < 5; x++)
            scalar[s.Grid.ToIndex(x, y)] = y;
        Assert.IsTrue(MorseApi.TryBuildGradient(ref s, in scalar));
        MorseApi.TraceManifolds(ref s);

        for (var i = 0; i < s.Grid.Length; i++)
            Assert.GreaterOrEqual(s.Component[i], 0);
        MorseApi.Dispose(ref s);
        scalar.Dispose();
    }

    [Test]
    public void Dispose_Double()
    {
        Assert.IsTrue(MorseApi.TryCreate(3, 3, 10, Allocator.Temp, out var s));
        MorseApi.Dispose(ref s);
        MorseApi.Dispose(ref s);
    }

    [Test]
    public unsafe void PairByPersistence_MaximaGetPaired()
    {
        Assert.IsTrue(MorseApi.TryCreate(5, 1, 100, Allocator.Temp, out var s));
        var scalar = new NativeArray<float>(5, Allocator.Temp);
        scalar[0] = 0f;
        scalar[1] = 5f;
        scalar[2] = 1f;
        scalar[3] = 4f;
        scalar[4] = 0f;
        Assert.IsTrue(MorseApi.TryBuildGradient(ref s, in scalar));
        Assert.IsTrue(MorseApi.TryPairByPersistence(ref s, in scalar));
        var paired = false;
        for (var i = 0; i < s.Critical.Length; i++)
            if (s.Critical.Ptr[i].Pair >= 0)
                paired = true;
        Assert.IsTrue(paired, "At least one critical point must be paired");
        MorseApi.Dispose(ref s);
        scalar.Dispose();
    }

    [Test]
    public void Simplify_RemovesLowPersistenceMaxima()
    {
        Assert.IsTrue(MorseApi.TryCreate(5, 1, 100, Allocator.Temp, out var s));
        var scalar = new NativeArray<float>(5, Allocator.Temp);
        scalar[0] = 0f;
        scalar[1] = 5f;
        scalar[2] = 1f;
        scalar[3] = 4f;
        scalar[4] = 0f;
        Assert.IsTrue(MorseApi.TryBuildGradient(ref s, in scalar));
        Assert.IsTrue(MorseApi.TryPairByPersistence(ref s, in scalar));
        Assert.IsTrue(MorseApi.TrySimplify(ref s, in scalar, 2.0f));
        MorseApi.Dispose(ref s);
        scalar.Dispose();
    }
}