using BovineLabs.Grid.Cftp;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public class CftpTests
{
    [Test]
    public void Create_Dimensions()
    {
        Assert.IsTrue(CftpApi.TryCreate(3, 3, 100, Allocator.Temp, out var s));
        Assert.AreEqual(9, s.Grid.Length);
        CftpApi.Dispose(ref s);
    }

    [Test]
    public unsafe void InitializeExtremes()
    {
        Assert.IsTrue(CftpApi.TryCreate(3, 3, 100, Allocator.Temp, out var s));
        CftpApi.InitializeExtremes(ref s);
        for (var i = 0; i < s.Grid.Length; i++)
        {
            Assert.AreEqual(0, s.Low[i]);
            Assert.AreEqual(1, s.High[i]);
        }

        CftpApi.Dispose(ref s);
    }

    [Test]
    public void Coalesced_NotInitially()
    {
        Assert.IsTrue(CftpApi.TryCreate(3, 3, 100, Allocator.Temp, out var s));
        CftpApi.InitializeExtremes(ref s);
        Assert.IsFalse(CftpApi.Coalesced(ref s));
        CftpApi.Dispose(ref s);
    }

    [Test]
    public void GenerateUpdates()
    {
        Assert.IsTrue(CftpApi.TryCreate(3, 3, 1000, Allocator.Temp, out var s));
        var rng = new Random(42);
        Assert.IsTrue(CftpApi.TryGeneratePastUpdates(ref s, ref rng, 5));
        Assert.AreEqual(45, s.Updates.Length);
        CftpApi.Dispose(ref s);
    }

    [Test]
    public void Dispose_Double()
    {
        Assert.IsTrue(CftpApi.TryCreate(3, 3, 10, Allocator.Temp, out var s));
        CftpApi.Dispose(ref s);
        CftpApi.Dispose(ref s);
    }

    [Test]
    public void SampleExact_Coalesces()
    {
        Assert.IsTrue(CftpApi.TryCreate(4, 4, 10000, Allocator.Temp, out var s));
        var rng = new Random(7);
        var sample = new NativeArray<byte>(s.Grid.Length, Allocator.Temp);
        Assert.IsTrue(CftpApi.TrySampleExact(ref s, ref rng, ref sample));
        for (var i = 0; i < sample.Length; i++)
            Assert.IsTrue(sample[i] == 0 || sample[i] == 1, "Sample must be binary");
        CftpApi.Dispose(ref s);
        sample.Dispose();
    }
}