using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Cbs;

public class CbsTests
{
    [Test] public void Create_Dimensions()
    { var s = CbsApi.Create(10, 10, 100, Allocator.Temp); Assert.AreEqual(100, s.Grid.Length); CbsApi.Dispose(ref s); }

    [Test] public void Solve_TwoAgents()
    {
        var s = CbsApi.Create(5, 5, 100, Allocator.Temp);
        var blocked = new NativeArray<byte>(25, Allocator.Temp); blocked.Fill((byte)0);
        var agents = new NativeArray<AgentTask>(2, Allocator.Temp);
        agents[0] = new AgentTask { Start = s.Grid.ToIndex(0, 0), Goal = s.Grid.ToIndex(4, 4) };
        agents[1] = new AgentTask { Start = s.Grid.ToIndex(4, 0), Goal = s.Grid.ToIndex(0, 4) };
        var paths = new NativeList<int>(Allocator.Temp);
        var ranges = new NativeList<RangeI>(Allocator.Temp);
        Assert.IsTrue(CbsApi.Solve(ref s, blocked, agents, paths, ranges));
        Assert.AreEqual(2, ranges.Length);
        CbsApi.Dispose(ref s); blocked.Dispose(); agents.Dispose(); paths.Dispose(); ranges.Dispose();
    }

    [Test] public void Dispose_Double() { var s = CbsApi.Create(5, 5, 10, Allocator.Temp); CbsApi.Dispose(ref s); CbsApi.Dispose(ref s); }
}
