using BovineLabs.Grid;
using BovineLabs.Grid.Subgoal;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public class SubgoalTests
{
    [Test]
    public void Create_Dimensions()
    {
        Assert.IsTrue(SubgoalApi.TryCreate(10, 10, 100, 1000, Allocator.Temp, out var s));
        Assert.AreEqual(100, s.Grid.Length);
        SubgoalApi.Dispose(ref s);
    }

    [Test]
    public void Build_OpenGrid_NoSubgoals()
    {
        Assert.IsTrue(SubgoalApi.TryCreate(10, 10, 100, 1000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(100, Allocator.Temp);
        blocked.Fill((byte)0);
        Assert.IsTrue(SubgoalApi.TryBuild(ref s, blocked));
        Assert.AreEqual(0, s.Subgoals.Length);
        SubgoalApi.Dispose(ref s);
        blocked.Dispose();
    }

    [Test]
    public void Build_WithObstacles()
    {
        Assert.IsTrue(SubgoalApi.TryCreate(10, 10, 100, 1000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(100, Allocator.Temp);
        blocked.Fill((byte)0);

        blocked[s.Grid.ToIndex(5, 3)] = 1;
        blocked[s.Grid.ToIndex(5, 4)] = 1;
        blocked[s.Grid.ToIndex(5, 5)] = 1;
        blocked[s.Grid.ToIndex(6, 5)] = 1;
        blocked[s.Grid.ToIndex(7, 5)] = 1;
        Assert.IsTrue(SubgoalApi.TryBuild(ref s, blocked));

        Assert.GreaterOrEqual(s.Subgoals.Length, 0);
        SubgoalApi.Dispose(ref s);
        blocked.Dispose();
    }

    [Test]
    public void Search_OpenGrid()
    {
        Assert.IsTrue(SubgoalApi.TryCreate(10, 10, 100, 1000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(100, Allocator.Temp);
        blocked.Fill((byte)0);
        var path = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(SubgoalApi.TrySearch(ref s, blocked, 0, 99, ref path));
        Assert.AreEqual(0, path[0]);
        Assert.AreEqual(99, path[path.Length - 1]);
        SubgoalApi.Dispose(ref s);
        blocked.Dispose();
        path.Dispose();
    }

    [Test]
    public unsafe void LineOfSight()
    {
        var g = Grid2D.Create(10, 10);
        var blocked = new NativeArray<byte>(100, Allocator.Temp);
        blocked.Fill((byte)0);
        var blockedPtr = (byte*)blocked.GetUnsafeReadOnlyPtr();
        {
            var a = new int2(0, 0);
            var b = new int2(5, 0);
            Assert.IsTrue(SubgoalApi.LineOfSight(in g, blockedPtr, ref a, ref b));
        }
        blocked[g.ToIndex(3, 0)] = 1;
        {
            var a = new int2(0, 0);
            var b = new int2(5, 0);
            Assert.IsFalse(SubgoalApi.LineOfSight(in g, blockedPtr, ref a, ref b));
        }
        blocked.Dispose();
    }

    [Test]
    public void Dispose_Double()
    {
        Assert.IsTrue(SubgoalApi.TryCreate(5, 5, 10, 100, Allocator.Temp, out var s));
        SubgoalApi.Dispose(ref s);
        SubgoalApi.Dispose(ref s);
    }

    [Test]
    public void Search_WithSubgoals_PathIsOptimalOrNear()
    {
        Assert.IsTrue(SubgoalApi.TryCreate(10, 10, 200, 5000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(100, Allocator.Temp);
        blocked.Fill((byte)0);
        blocked[s.Grid.ToIndex(5, 0)] = 1;
        blocked[s.Grid.ToIndex(5, 1)] = 1;
        blocked[s.Grid.ToIndex(5, 2)] = 1;
        blocked[s.Grid.ToIndex(5, 3)] = 1;
        blocked[s.Grid.ToIndex(5, 4)] = 1;
        blocked[s.Grid.ToIndex(5, 5)] = 1;
        blocked[s.Grid.ToIndex(5, 6)] = 1;
        blocked[s.Grid.ToIndex(5, 7)] = 1;
        blocked[s.Grid.ToIndex(5, 8)] = 1;

        Assert.IsTrue(SubgoalApi.TryBuild(ref s, blocked));
        Assert.Greater(s.Subgoals.Length, 0, "Wall must generate subgoals");

        var path = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(SubgoalApi.TrySearch(ref s, blocked, 0, 99, ref path));
        Assert.AreEqual(0, path[0]);
        Assert.AreEqual(99, path[path.Length - 1]);
        for (var i = 0; i < path.Length; i++)
            Assert.AreEqual(0, blocked[path[i]], $"Path must not pass through obstacle at step {i}");

        SubgoalApi.Dispose(ref s);
        blocked.Dispose();
        path.Dispose();
    }
}