using BovineLabs.Grid.Coordinates;
using NUnit.Framework;
using Unity.Mathematics;

public class GridCoord2Tests
{
    [Test]
    public void EqualValues_AreEqual()
    {
        Assert.AreEqual(new GridCoord2(3, 7), new GridCoord2(3, 7));
    }

    [Test]
    public void DifferentValues_AreNotEqual()
    {
        Assert.AreNotEqual(new GridCoord2(1, 2), new GridCoord2(2, 1));
    }

    [Test]
    public void ToInt2_RoundTrips()
    {
        var coord = new GridCoord2(-5, 10);
        coord.ToInt2(out var v);
        Assert.AreEqual(new int2(-5, 10), v);
        GridCoord2.FromInt2(v, out var roundTripped);
        Assert.AreEqual(coord, roundTripped);
    }

    [Test]
    public void ManhattanDelta_ReturnsAbsDxPlusAbsDy()
    {
        var a = new GridCoord2(1, 2);
        var b = new GridCoord2(4, 6);
        a.ManhattanDelta(b, out var ab);
        b.ManhattanDelta(a, out var ba);
        Assert.AreEqual(7, ab);
        Assert.AreEqual(7, ba);
    }

    [Test]
    public void ChebyshevDelta_ReturnsMaxAbsAxis()
    {
        var a = new GridCoord2(0, 0);
        var b = new GridCoord2(3, 5);
        a.ChebyshevDelta(b, out var result);
        Assert.AreEqual(5, result);
    }

    [Test]
    public void OctileDelta_IsDeterministic()
    {
        var a = new GridCoord2(0, 0);
        var b = new GridCoord2(3, 4);
        a.OctileDelta(b, out var c1);
        a.OctileDelta(b, out var c2);
        Assert.AreEqual(c1, c2);
    }

    [Test]
    public void OctileDelta_SamePoint_ReturnsZero()
    {
        var a = new GridCoord2(5, 5);
        a.OctileDelta(a, out var result);
        Assert.AreEqual(0f, result);
    }

    [Test]
    public void OctileDelta_PureOrthogonal_ReturnsExactDistance()
    {
        var a = new GridCoord2(0, 0);
        var b = new GridCoord2(5, 0);
        a.OctileDelta(b, out var result);
        Assert.AreEqual(5f, result);
    }

    [Test]
    public void SquaredEuclideanDelta_ReturnsSumOfSquares()
    {
        var a = new GridCoord2(0, 0);
        var b = new GridCoord2(3, 4);
        a.SquaredEuclideanDelta(b, out var result);
        Assert.AreEqual(25f, result);
    }

    [Test]
    public void EuclideanDelta_ReturnsSqrt()
    {
        var a = new GridCoord2(0, 0);
        var b = new GridCoord2(3, 4);
        a.EuclideanDelta(b, out var result);
        Assert.AreEqual(5f, result, 0.0001f);
    }

    [Test]
    public void AllDistances_AreSymmetric()
    {
        var a = new GridCoord2(2, 8);
        var b = new GridCoord2(10, 3);

        a.ManhattanDelta(b, out var mAB);
        b.ManhattanDelta(a, out var mBA);
        Assert.AreEqual(mAB, mBA);

        a.ChebyshevDelta(b, out var cAB);
        b.ChebyshevDelta(a, out var cBA);
        Assert.AreEqual(cAB, cBA);

        a.OctileDelta(b, out var oAB);
        b.OctileDelta(a, out var oBA);
        Assert.AreEqual(oAB, oBA);

        a.SquaredEuclideanDelta(b, out var sAB);
        b.SquaredEuclideanDelta(a, out var sBA);
        Assert.AreEqual(sAB, sBA);

        a.EuclideanDelta(b, out var eAB);
        b.EuclideanDelta(a, out var eBA);
        Assert.AreEqual(eAB, eBA, 0.0001f);
    }

    [Test]
    public void AllDistances_SamePoint_ReturnZero()
    {
        var p = new GridCoord2(42, -7);

        p.ManhattanDelta(p, out var m);
        Assert.AreEqual(0, m);

        p.ChebyshevDelta(p, out var c);
        Assert.AreEqual(0, c);

        p.OctileDelta(p, out var o);
        Assert.AreEqual(0f, o);

        p.SquaredEuclideanDelta(p, out var s);
        Assert.AreEqual(0f, s);

        p.EuclideanDelta(p, out var e);
        Assert.AreEqual(0f, e, 0.0001f);
    }
}

public class GridCoord3Tests
{
    [Test]
    public void ToInt3_RoundTrips()
    {
        var coord = new GridCoord3(-1, 2, 3);
        coord.ToInt3(out var v);
        Assert.AreEqual(new int3(-1, 2, 3), v);
        GridCoord3.FromInt3(v, out var roundTripped);
        Assert.AreEqual(coord, roundTripped);
    }

    [Test]
    public void ManhattanDelta_ReturnsSumOfAbsDeltas()
    {
        var a = new GridCoord3(1, 2, 3);
        var b = new GridCoord3(4, 6, 0);
        a.ManhattanDelta(b, out var result);
        Assert.AreEqual(3 + 4 + 3, result);
    }

    [Test]
    public void ChebyshevDelta_ReturnsMaxAbsDelta()
    {
        var a = new GridCoord3(0, 0, 0);
        var b = new GridCoord3(2, 5, 3);
        a.ChebyshevDelta(b, out var result);
        Assert.AreEqual(5, result);
    }

    [Test]
    public void SquaredEuclideanDelta_ReturnsSumOfSquares()
    {
        var a = new GridCoord3(0, 0, 0);
        var b = new GridCoord3(1, 2, 2);
        a.SquaredEuclideanDelta(b, out var result);
        Assert.AreEqual(9f, result);
    }

    [Test]
    public void EuclideanDelta_ReturnsSqrt()
    {
        var a = new GridCoord3(0, 0, 0);
        var b = new GridCoord3(1, 2, 2);
        a.EuclideanDelta(b, out var result);
        Assert.AreEqual(3f, result, 0.0001f);
    }

    [Test]
    public void EqualValues_AreEqual()
    {
        Assert.AreEqual(new GridCoord3(1, 2, 3), new GridCoord3(1, 2, 3));
    }

    [Test]
    public void DifferentValues_AreNotEqual()
    {
        Assert.AreNotEqual(new GridCoord3(1, 2, 3), new GridCoord3(3, 2, 1));
    }
}
