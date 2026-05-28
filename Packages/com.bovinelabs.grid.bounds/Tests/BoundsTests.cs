using BovineLabs.Grid.Bounds;
using NUnit.Framework;

public class GridBounds2DTests
{
    [Test]
    public void Create_Valid2D_ReturnsTrue()
    {
        Assert.IsTrue(GridBounds2D.TryCreate(5, 10, out var b));
        Assert.AreEqual(5, b.Width);
        Assert.AreEqual(10, b.Height);
    }

    [Test]
    public void Create_ZeroWidth_ReturnsFalse()
    {
        Assert.IsFalse(GridBounds2D.TryCreate(0, 10, out var b));
        Assert.AreEqual(default(GridBounds2D), b);
    }

    [Test]
    public void Create_ZeroHeight_ReturnsFalse()
    {
        Assert.IsFalse(GridBounds2D.TryCreate(5, 0, out _));
    }

    [Test]
    public void Create_NegativeWidth_ReturnsFalse()
    {
        Assert.IsFalse(GridBounds2D.TryCreate(-1, 10, out _));
    }

    [Test]
    public void Create_NegativeHeight_ReturnsFalse()
    {
        Assert.IsFalse(GridBounds2D.TryCreate(5, -1, out _));
    }

    [Test]
    public void Create_Overflow_ReturnsFalse()
    {
        Assert.IsFalse(GridBounds2D.TryCreate(100000, 100000, out _));
    }

    [Test]
    public void InBounds_MinCorner_ReturnsTrue()
    {
        GridBounds2D.TryCreate(5, 5, out var b);
        Assert.IsTrue(b.InBounds(0, 0));
        Assert.IsTrue(b.InBounds(0));
    }

    [Test]
    public void InBounds_MaxExclusive_ReturnsFalse()
    {
        GridBounds2D.TryCreate(5, 5, out var b);
        Assert.IsFalse(b.InBounds(5, 0));
        Assert.IsFalse(b.InBounds(0, 5));
        Assert.IsFalse(b.InBounds(25));
    }

    [Test]
    public void InBounds_Negative_ReturnsFalse()
    {
        GridBounds2D.TryCreate(5, 5, out var b);
        Assert.IsFalse(b.InBounds(-1, 0));
        Assert.IsFalse(b.InBounds(0, -1));
        Assert.IsFalse(b.InBounds(-1));
    }

    [Test]
    public void Length_Valid2D_EqualsWidthTimesHeight()
    {
        GridBounds2D.TryCreate(7, 3, out var b);
        Assert.AreEqual(21, b.Length);
    }

    [Test]
    public void InBounds_LastValidIndex_ReturnsTrue()
    {
        GridBounds2D.TryCreate(5, 5, out var b);
        Assert.IsTrue(b.InBounds(4, 4));
        Assert.IsTrue(b.InBounds(24));
    }
}

public class GridBounds3DTests
{
    [Test]
    public void Create_Valid3D_ReturnsTrue()
    {
        Assert.IsTrue(GridBounds3D.TryCreate(3, 4, 5, out var b));
        Assert.AreEqual(3, b.Width);
        Assert.AreEqual(4, b.Height);
        Assert.AreEqual(5, b.Depth);
    }

    [Test]
    public void Create_ZeroDepth_ReturnsFalse()
    {
        Assert.IsFalse(GridBounds3D.TryCreate(3, 4, 0, out _));
    }

    [Test]
    public void Create_NegativeDimension_ReturnsFalse()
    {
        Assert.IsFalse(GridBounds3D.TryCreate(3, -1, 5, out _));
    }

    [Test]
    public void Create_Overflow_ReturnsFalse()
    {
        Assert.IsFalse(GridBounds3D.TryCreate(10000, 10000, 100, out _));
    }

    [Test]
    public void Length_Valid3D_EqualsWidthTimesHeightTimesDepth()
    {
        GridBounds3D.TryCreate(3, 4, 5, out var b);
        Assert.AreEqual(60, b.Length);
    }

    [Test]
    public void InBounds_Valid_ReturnsTrue()
    {
        GridBounds3D.TryCreate(3, 4, 5, out var b);
        Assert.IsTrue(b.InBounds(0, 0, 0));
        Assert.IsTrue(b.InBounds(2, 3, 4));
        Assert.IsTrue(b.InBounds(0));
        Assert.IsTrue(b.InBounds(59));
    }

    [Test]
    public void InBounds_OutOfRange_ReturnsFalse()
    {
        GridBounds3D.TryCreate(3, 4, 5, out var b);
        Assert.IsFalse(b.InBounds(3, 0, 0));
        Assert.IsFalse(b.InBounds(0, 4, 0));
        Assert.IsFalse(b.InBounds(0, 0, 5));
        Assert.IsFalse(b.InBounds(60));
    }

    [Test]
    public void InBounds_Negative_ReturnsFalse()
    {
        GridBounds3D.TryCreate(3, 4, 5, out var b);
        Assert.IsFalse(b.InBounds(-1, 0, 0));
        Assert.IsFalse(b.InBounds(-1));
    }
}
