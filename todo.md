Assumption: `NativeArray<T>` is your source grid, but the hot algorithms should usually operate on baked primitive views: `NativeArray<byte> blocked`, `NativeArray<float> cost`, `NativeArray<int> label`, etc. Burst expects native containers instead of managed arrays, and `NativeList<T>`/other native collections are allocator-backed unmanaged containers. Nested native containers are restricted in jobs, so variable-length per-cell data should be flattened into `offset/count + flatData` arrays. ([Unity Documentation][1])

Common pattern:

```csharp
using Unity.Collections;
using Unity.Mathematics;

public struct Grid2D
{
    public int Width;
    public int Height;
    public int Length;

    public void Setup(int width, int height)
    {
        Width = width;
        Height = height;
        Length = width * height;
    }

    public void ToIndex(int2 p, out int index)
    {
        index = p.y * Width + p.x;
    }

    public void ToCoord(int index, out int2 p)
    {
        p = new int2(index % Width, index / Width);
    }

    public bool InBounds(int2 p)
    {
        return p.x >= 0 && p.y >= 0 && p.x < Width && p.y < Height;
    }

    public bool TryIndex(int2 p, out int index)
    {
        index = p.y * Width + p.x;
        return p.x >= 0 && p.y >= 0 && p.x < Width && p.y < Height;
    }
}

public struct RangeI
{
    public int Offset;
    public int Count;
}

public struct HeapNode
{
    public int Id;
    public float Key0;
    public float Key1;
}
```

**Anya any-angle pathfinding** | Search over interval states, not cells.

```csharp
public struct AnyaNode
{
    public int2 Root;
    public int Row;
    public int XMin;
    public int XMax;
    public float G;
    public float F;
    public int Parent;
}

public struct AnyaState
{
    public Grid2D Grid;
    public NativeList<AnyaNode> Nodes;
    public NativeList<HeapNode> Open;
    public NativeArray<byte> Closed;
}

public struct AnyaApi
{
    public static void Setup(ref AnyaState s, int width, int height, int maxNodes, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Nodes = new NativeList<AnyaNode>(maxNodes, a);
        s.Open = new NativeList<HeapNode>(maxNodes, a);
        s.Closed = new NativeArray<byte>(maxNodes, a);
    }

    public static void Clear(ref AnyaState s)
    {
        s.Nodes.Clear();
        s.Open.Clear();
        s.Closed.Fill(0);
    }

    public static bool Search(
        ref AnyaState s,
        NativeArray<byte> blocked,
        int2 start,
        int2 goal,
        NativeList<int2> path)
    {
        Clear(ref s);
        path.Clear();

        // 1. Insert initial visibility interval containing start.
        // 2. Pop lowest f interval.
        // 3. If goal visible from root through interval, reconstruct.
        // 4. Project successors across obstacle corners.
        // 5. Push generated intervals.

        return false;
    }

    public static void ExtractPath(ref AnyaState s, int goalNode, NativeList<int2> path)
    {
        path.Clear();

        // Walk parent interval chain.
        // Emit turning roots in reverse.
        // Reverse path in-place.
    }

    public static void Dispose(ref AnyaState s)
    {
        if (s.Nodes.IsCreated) s.Nodes.Dispose();
        if (s.Open.IsCreated) s.Open.Dispose();
        if (s.Closed.IsCreated) s.Closed.Dispose();
    }
}
```

**Field D*** | Continuous-ish shortest path over grid corners using interpolation.

```csharp
public struct FieldDStarState
{
    public Grid2D Grid;
    public NativeArray<float> G;
    public NativeArray<float> RHS;
    public NativeArray<float2> Flow;
    public NativeList<HeapNode> Open;
    public NativeArray<byte> InOpen;
}

public struct FieldDStarApi
{
    public static void Setup(ref FieldDStarState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.G = new NativeArray<float>(s.Grid.Length, a);
        s.RHS = new NativeArray<float>(s.Grid.Length, a);
        s.Flow = new NativeArray<float2>(s.Grid.Length, a);
        s.Open = new NativeList<HeapNode>(s.Grid.Length, a);
        s.InOpen = new NativeArray<byte>(s.Grid.Length, a);
    }

    public static void Reset(ref FieldDStarState s)
    {
        s.Open.Clear();
        s.InOpen.Fill(0);
        s.G.Fill(float.PositiveInfinity);
        s.RHS.Fill(float.PositiveInfinity);
        s.Flow.Fill(float2.zero);
    }

    public static void SetGoal(ref FieldDStarState s, int goal)
    {
        s.RHS[goal] = 0f;

        // Push goal into open with key = CalculateKey(goal).
    }

    public static void UpdateEdgeCost(ref FieldDStarState s, NativeArray<float> cost, int cell)
    {
        // Recompute local RHS for affected corner/cell neighborhood.
        // Reinsert inconsistent states.
    }

    public static bool Step(ref FieldDStarState s, NativeArray<float> cost)
    {
        // Pop open.
        // Apply D*/LPA* consistency rule.
        // Use bilinear interpolation when computing neighbor update.

        return s.Open.Length > 0;
    }

    public static void ExtractFlow(ref FieldDStarState s, NativeArray<float> cost)
    {
        // For every cell, estimate negative gradient of interpolated G.
        // Store normalized direction into Flow.
    }

    public static void Dispose(ref FieldDStarState s)
    {
        if (s.G.IsCreated) s.G.Dispose();
        if (s.RHS.IsCreated) s.RHS.Dispose();
        if (s.Flow.IsCreated) s.Flow.Dispose();
        if (s.Open.IsCreated) s.Open.Dispose();
        if (s.InOpen.IsCreated) s.InOpen.Dispose();
    }
}
```

**D* Lite / LPA*** | Dynamic shortest-path repair after grid changes.

```csharp
public struct DStarLiteState
{
    public Grid2D Grid;
    public int Start;
    public int Goal;
    public float Km;
    public NativeArray<float> G;
    public NativeArray<float> RHS;
    public NativeList<HeapNode> Open;
    public NativeArray<byte> InOpen;
    public NativeArray<int> Parent;
}

public struct DStarLiteApi
{
    public static void Setup(ref DStarLiteState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.G = new NativeArray<float>(s.Grid.Length, a);
        s.RHS = new NativeArray<float>(s.Grid.Length, a);
        s.Open = new NativeList<HeapNode>(s.Grid.Length, a);
        s.InOpen = new NativeArray<byte>(s.Grid.Length, a);
        s.Parent = new NativeArray<int>(s.Grid.Length, a);
    }

    public static void Initialize(ref DStarLiteState s, int start, int goal)
    {
        s.Start = start;
        s.Goal = goal;
        s.Km = 0f;
        s.Open.Clear();
        s.InOpen.Fill(0);
        s.G.Fill(float.PositiveInfinity);
        s.RHS.Fill(float.PositiveInfinity);
        s.Parent.Fill(-1);

        s.RHS[goal] = 0f;

        // Push goal with CalculateKey(goal).
    }

    public static void NotifyMoved(ref DStarLiteState s, int oldStart, int newStart)
    {
        s.Start = newStart;

        // Km += heuristic(oldStart, newStart).
    }

    public static void UpdateCell(ref DStarLiteState s, NativeArray<float> cost, int cell)
    {
        // Recompute RHS(cell).
        // Recompute RHS of predecessors.
        // Push inconsistent vertices.
    }

    public static bool Repair(ref DStarLiteState s, NativeArray<float> cost, int maxPops)
    {
        // While open top key < key(start) or RHS(start) != G(start):
        //   pop u
        //   if G(u) > RHS(u): G(u)=RHS(u), update predecessors
        //   else G(u)=inf, update u and predecessors

        return s.RHS[s.Start] < float.PositiveInfinity;
    }

    public static void ExtractPath(ref DStarLiteState s, NativeArray<float> cost, NativeList<int> path)
    {
        path.Clear();

        // Greedily follow neighbor minimizing cost + G.
    }

    public static void Dispose(ref DStarLiteState s)
    {
        if (s.G.IsCreated) s.G.Dispose();
        if (s.RHS.IsCreated) s.RHS.Dispose();
        if (s.Open.IsCreated) s.Open.Dispose();
        if (s.InOpen.IsCreated) s.InOpen.Dispose();
        if (s.Parent.IsCreated) s.Parent.Dispose();
    }
}
```

**SIPP: Safe Interval Path Planning** | Search `(cell, safe-time-interval)` instead of `(cell, time)`.

```csharp
public struct SafeInterval
{
    public int Cell;
    public float Start;
    public float End;
}

public struct SippNode
{
    public int Cell;
    public int Interval;
    public float Time;
    public float F;
    public int Parent;
}

public struct SippState
{
    public Grid2D Grid;
    public NativeList<SafeInterval> Intervals;
    public NativeArray<RangeI> CellIntervals;
    public NativeList<SippNode> Nodes;
    public NativeList<HeapNode> Open;
}

public struct SippApi
{
    public static void Setup(ref SippState s, int width, int height, int maxIntervals, int maxNodes, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Intervals = new NativeList<SafeInterval>(maxIntervals, a);
        s.CellIntervals = new NativeArray<RangeI>(s.Grid.Length, a);
        s.Nodes = new NativeList<SippNode>(maxNodes, a);
        s.Open = new NativeList<HeapNode>(maxNodes, a);
    }

    public static void BuildSafeIntervals(
        ref SippState s,
        NativeArray<RangeI> blockedEventRanges,
        NativeArray<float2> blockedEvents)
    {
        s.Intervals.Clear();

        // For each cell:
        //   subtract occupied time intervals from [0, +inf)
        //   append maximal free intervals
        //   write offset/count into CellIntervals
    }

    public static bool Search(
        ref SippState s,
        int start,
        int goal,
        float startTime,
        NativeList<int> path,
        NativeList<float> arrivalTimes)
    {
        s.Nodes.Clear();
        s.Open.Clear();
        path.Clear();
        arrivalTimes.Clear();

        // Push start interval containing startTime.
        // Expand by earliest arrival into neighbor safe intervals.
        // Respect wait time and traversal duration.

        return false;
    }

    public static void Dispose(ref SippState s)
    {
        if (s.Intervals.IsCreated) s.Intervals.Dispose();
        if (s.CellIntervals.IsCreated) s.CellIntervals.Dispose();
        if (s.Nodes.IsCreated) s.Nodes.Dispose();
        if (s.Open.IsCreated) s.Open.Dispose();
    }
}
```

**CBS: Conflict-Based Search** | Exact multi-agent pathfinding by branching on collisions.

```csharp
public struct AgentTask
{
    public int Start;
    public int Goal;
}

public struct CbsConstraint
{
    public int Agent;
    public int Cell;
    public int CellB;
    public int Time;
    public byte IsEdge;
}

public struct CbsNode
{
    public int ConstraintOffset;
    public int ConstraintCount;
    public int PathOffset;
    public int PathCount;
    public int Cost;
    public int Parent;
}

public struct CbsState
{
    public Grid2D Grid;
    public NativeList<CbsNode> Nodes;
    public NativeList<CbsConstraint> Constraints;
    public NativeList<int> FlatPaths;
    public NativeList<RangeI> AgentPathRanges;
    public NativeList<HeapNode> Open;
}

public struct CbsApi
{
    public static void Setup(ref CbsState s, int width, int height, int maxNodes, int maxConstraints, int maxPathCells, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Nodes = new NativeList<CbsNode>(maxNodes, a);
        s.Constraints = new NativeList<CbsConstraint>(maxConstraints, a);
        s.FlatPaths = new NativeList<int>(maxPathCells, a);
        s.AgentPathRanges = new NativeList<RangeI>(maxPathCells, a);
        s.Open = new NativeList<HeapNode>(maxNodes, a);
    }

    public static bool Solve(
        ref CbsState s,
        NativeArray<byte> blocked,
        NativeArray<AgentTask> agents,
        NativeList<int> solutionFlatPaths,
        NativeList<RangeI> solutionRanges)
    {
        s.Nodes.Clear();
        s.Constraints.Clear();
        s.FlatPaths.Clear();
        s.AgentPathRanges.Clear();
        s.Open.Clear();

        // Root: plan each agent independently.
        // Pop min-cost CBS node.
        // Detect first vertex/edge conflict.
        // Branch: add one constraint for each conflicting agent.
        // Replan only constrained agent.

        return false;
    }

    public static bool FindFirstConflict(
        NativeArray<int> flatPaths,
        NativeArray<RangeI> ranges,
        out CbsConstraint conflictA,
        out CbsConstraint conflictB)
    {
        conflictA = default;
        conflictB = default;

        // Scan synchronized timesteps.
        // Detect same-cell or swapped-edge conflict.

        return false;
    }

    public static void Dispose(ref CbsState s)
    {
        if (s.Nodes.IsCreated) s.Nodes.Dispose();
        if (s.Constraints.IsCreated) s.Constraints.Dispose();
        if (s.FlatPaths.IsCreated) s.FlatPaths.Dispose();
        if (s.AgentPathRanges.IsCreated) s.AgentPathRanges.Dispose();
        if (s.Open.IsCreated) s.Open.Dispose();
    }
}
```

**Continuum Crowds** | Density + potential field + flow field.

```csharp
public struct ContinuumCrowdState
{
    public Grid2D Grid;
    public NativeArray<float> Density;
    public NativeArray<float> Speed;
    public NativeArray<float> Potential;
    public NativeArray<float2> Flow;
    public NativeArray<float> Divergence;
}

public struct ContinuumCrowdApi
{
    public static void Setup(ref ContinuumCrowdState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Density = new NativeArray<float>(s.Grid.Length, a);
        s.Speed = new NativeArray<float>(s.Grid.Length, a);
        s.Potential = new NativeArray<float>(s.Grid.Length, a);
        s.Flow = new NativeArray<float2>(s.Grid.Length, a);
        s.Divergence = new NativeArray<float>(s.Grid.Length, a);
    }

    public static void ClearDensity(ref ContinuumCrowdState s)
    {
        s.Density.Fill(0f);
        s.Divergence.Fill(0f);
    }

    public static void SplatAgents(ref ContinuumCrowdState s, NativeArray<float2> positions)
    {
        // Scatter agent mass into Density.
    }

    public static void SolvePotential(ref ContinuumCrowdState s, NativeArray<byte> blocked, int goal, int iterations)
    {
        // Fast sweeping / Eikonal-like solve:
        // speed = baseSpeed / congestion(Density)
        // potential(goal)=0
        // iterate directional sweeps
    }

    public static void BuildFlow(ref ContinuumCrowdState s)
    {
        // Flow = -normalize(gradient(Potential)).
    }

    public static void AdvectAgents(ref ContinuumCrowdState s, NativeArray<float2> positions, float dt)
    {
        // Sample Flow at position.
        // Integrate position.
    }

    public static void Dispose(ref ContinuumCrowdState s)
    {
        if (s.Density.IsCreated) s.Density.Dispose();
        if (s.Speed.IsCreated) s.Speed.Dispose();
        if (s.Potential.IsCreated) s.Potential.Dispose();
        if (s.Flow.IsCreated) s.Flow.Dispose();
        if (s.Divergence.IsCreated) s.Divergence.Dispose();
    }
}
```

**Fast Marching Method** | Dijkstra-like Eikonal solver.

```csharp
public struct FastMarchingState
{
    public Grid2D Grid;
    public NativeArray<float> T;
    public NativeArray<byte> State; // 0 far, 1 trial, 2 accepted
    public NativeList<HeapNode> Heap;
}

public struct FastMarchingApi
{
    public static void Setup(ref FastMarchingState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.T = new NativeArray<float>(s.Grid.Length, a);
        s.State = new NativeArray<byte>(s.Grid.Length, a);
        s.Heap = new NativeList<HeapNode>(s.Grid.Length, a);
    }

    public static void InitializeSources(ref FastMarchingState s, NativeArray<int> sources)
    {
        s.T.Fill(float.PositiveInfinity);
        s.State.Fill(0);
        s.Heap.Clear();

        // Set source T=0, State=trial, push heap.
    }

    public static bool Propagate(ref FastMarchingState s, NativeArray<float> speed)
    {
        // Pop min trial.
        // Mark accepted.
        // Update neighbors using upwind quadratic solve.
        // Push/decrease trial values.

        return s.Heap.Length > 0;
    }

    public static void BuildGradientFlow(ref FastMarchingState s, NativeArray<float2> flow)
    {
        // flow[i] = -normalize(gradient(T)).
    }

    public static void Dispose(ref FastMarchingState s)
    {
        if (s.T.IsCreated) s.T.Dispose();
        if (s.State.IsCreated) s.State.Dispose();
        if (s.Heap.IsCreated) s.Heap.Dispose();
    }
}
```

**Fast Sweeping Method** | Eikonal solve by directional Gauss-Seidel passes.

```csharp
public struct FastSweepingState
{
    public Grid2D Grid;
    public NativeArray<float> T;
}

public struct FastSweepingApi
{
    public static void Setup(ref FastSweepingState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.T = new NativeArray<float>(s.Grid.Length, a);
    }

    public static void Initialize(ref FastSweepingState s, NativeArray<int> sources)
    {
        s.T.Fill(float.PositiveInfinity);

        // Set source cells to 0.
    }

    public static void SweepAllDirections(ref FastSweepingState s, NativeArray<float> speed, int rounds)
    {
        // Sweep x+,y+.
        // Sweep x-,y+.
        // Sweep x+,y-.
        // Sweep x-,y-.
        // Repeat rounds.
    }

    public static void RelaxCell(ref FastSweepingState s, NativeArray<float> speed, int cell)
    {
        // Upwind local Eikonal update from accepted neighbor values.
    }

    public static void Dispose(ref FastSweepingState s)
    {
        if (s.T.IsCreated) s.T.Dispose();
    }
}
```

**Jump Point Search** | A* with grid symmetry pruning.

```csharp
public struct JpsNode
{
    public int Cell;
    public int Parent;
    public float G;
    public float F;
}

public struct JpsState
{
    public Grid2D Grid;
    public NativeArray<float> G;
    public NativeArray<int> Parent;
    public NativeArray<byte> Closed;
    public NativeList<HeapNode> Open;
}

public struct JpsApi
{
    public static void Setup(ref JpsState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.G = new NativeArray<float>(s.Grid.Length, a);
        s.Parent = new NativeArray<int>(s.Grid.Length, a);
        s.Closed = new NativeArray<byte>(s.Grid.Length, a);
        s.Open = new NativeList<HeapNode>(s.Grid.Length, a);
    }

    public static bool Search(ref JpsState s, NativeArray<byte> blocked, int start, int goal, NativeList<int> path)
    {
        s.G.Fill(float.PositiveInfinity);
        s.Parent.Fill(-1);
        s.Closed.Fill(0);
        s.Open.Clear();
        path.Clear();

        // Push start.
        // Pop node.
        // Generate natural + forced directions.
        // Jump in direction until obstacle, forced neighbor, or goal.
        // Relax jump point.

        return false;
    }

    public static bool Jump(ref JpsState s, NativeArray<byte> blocked, int cell, int2 dir, int goal, out int jumpCell)
    {
        jumpCell = -1;

        // March along dir.
        // Stop on blocked.
        // Stop on goal.
        // Stop on forced neighbor.

        return false;
    }

    public static void ExtractPath(ref JpsState s, int goal, NativeList<int> path)
    {
        path.Clear();

        // Follow Parent.
        // Optionally expand jump segments into per-cell path.
    }

    public static void Dispose(ref JpsState s)
    {
        if (s.G.IsCreated) s.G.Dispose();
        if (s.Parent.IsCreated) s.Parent.Dispose();
        if (s.Closed.IsCreated) s.Closed.Dispose();
        if (s.Open.IsCreated) s.Open.Dispose();
    }
}
```

**Rectangular Symmetry Reduction** | Compress empty rectangles into perimeter macro-edges.

```csharp
public struct RsrRect
{
    public int2 Min;
    public int2 Max;
    public int PerimeterOffset;
    public int PerimeterCount;
}

public struct RsrState
{
    public Grid2D Grid;
    public NativeArray<int> RectOfCell;
    public NativeList<RsrRect> Rects;
    public NativeList<int> PerimeterCells;
}

public struct RsrApi
{
    public static void Setup(ref RsrState s, int width, int height, int maxRects, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.RectOfCell = new NativeArray<int>(s.Grid.Length, a);
        s.Rects = new NativeList<RsrRect>(maxRects, a);
        s.PerimeterCells = new NativeList<int>(s.Grid.Length, a);
    }

    public static void Build(ref RsrState s, NativeArray<byte> blocked)
    {
        s.RectOfCell.Fill(-1);
        s.Rects.Clear();
        s.PerimeterCells.Clear();

        // Greedily decompose maximal empty rectangles.
        // Mark interior cells as pruned.
        // Store perimeter cells and macro adjacency.
    }

    public static void GetSuccessors(ref RsrState s, int cell, NativeList<int> successors)
    {
        successors.Clear();

        // If perimeter cell: emit local neighbors + macro jumps across rectangle.
        // If normal cell: emit normal grid neighbors.
    }

    public static void Dispose(ref RsrState s)
    {
        if (s.RectOfCell.IsCreated) s.RectOfCell.Dispose();
        if (s.Rects.IsCreated) s.Rects.Dispose();
        if (s.PerimeterCells.IsCreated) s.PerimeterCells.Dispose();
    }
}
```

**Compressed Path Database** | Precomputed first-move table, compressed by rectangles/runs.

```csharp
public struct CpdRun
{
    public int Source;
    public int TargetMin;
    public int TargetMax;
    public byte FirstMove;
}

public struct CpdState
{
    public Grid2D Grid;
    public NativeList<CpdRun> Runs;
    public NativeArray<RangeI> SourceRuns;
}

public struct CpdApi
{
    public static void Setup(ref CpdState s, int width, int height, int maxRuns, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Runs = new NativeList<CpdRun>(maxRuns, a);
        s.SourceRuns = new NativeArray<RangeI>(s.Grid.Length, a);
    }

    public static void Build(ref CpdState s, NativeArray<byte> blocked)
    {
        s.Runs.Clear();

        // For every source:
        //   run SSSP/A*
        //   compute first move to all targets
        //   compress equal first-move target intervals/ranges
        //   write SourceRuns[source]
    }

    public static bool TryGetFirstMove(ref CpdState s, int source, int target, out byte move)
    {
        move = 255;

        // Binary search source run range.
        // If target inside run, output run.FirstMove.

        return false;
    }

    public static void ExtractPath(ref CpdState s, int source, int target, NativeList<int> path)
    {
        path.Clear();

        // Repeatedly TryGetFirstMove(current, target).
        // Apply move until target.
    }

    public static void Dispose(ref CpdState s)
    {
        if (s.Runs.IsCreated) s.Runs.Dispose();
        if (s.SourceRuns.IsCreated) s.SourceRuns.Dispose();
    }
}
```

**Subgoal Graphs** | Obstacle-corner abstraction graph.

```csharp
public struct SubgoalEdge
{
    public int To;
    public float Cost;
}

public struct SubgoalState
{
    public Grid2D Grid;
    public NativeList<int> Subgoals;
    public NativeArray<int> SubgoalOfCell;
    public NativeList<SubgoalEdge> Edges;
    public NativeList<RangeI> EdgeRanges;
}

public struct SubgoalApi
{
    public static void Setup(ref SubgoalState s, int width, int height, int maxSubgoals, int maxEdges, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Subgoals = new NativeList<int>(maxSubgoals, a);
        s.SubgoalOfCell = new NativeArray<int>(s.Grid.Length, a);
        s.Edges = new NativeList<SubgoalEdge>(maxEdges, a);
        s.EdgeRanges = new NativeList<RangeI>(maxSubgoals, a);
    }

    public static void Build(ref SubgoalState s, NativeArray<byte> blocked)
    {
        s.Subgoals.Clear();
        s.SubgoalOfCell.Fill(-1);
        s.Edges.Clear();
        s.EdgeRanges.Clear();

        // Detect corners.
        // Keep necessary/h-level subgoals.
        // Add direct-reachable edges.
        // Prune dominated edges.
    }

    public static bool Search(ref SubgoalState s, NativeArray<byte> blocked, int start, int goal, NativeList<int> path)
    {
        path.Clear();

        // Connect start/goal temporarily to visible subgoals.
        // A* on subgoal graph.
        // Expand abstract edges into grid path if needed.

        return false;
    }

    public static void Dispose(ref SubgoalState s)
    {
        if (s.Subgoals.IsCreated) s.Subgoals.Dispose();
        if (s.SubgoalOfCell.IsCreated) s.SubgoalOfCell.Dispose();
        if (s.Edges.IsCreated) s.Edges.Dispose();
        if (s.EdgeRanges.IsCreated) s.EdgeRanges.Dispose();
    }
}
```

**Exact Euclidean Distance Transform** | Linear-time squared distance to nearest obstacle.

```csharp
public struct EdtState
{
    public Grid2D Grid;
    public NativeArray<float> Temp;
    public NativeArray<int> V;
    public NativeArray<float> Z;
}

public struct EdtApi
{
    public static void Setup(ref EdtState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Temp = new NativeArray<float>(s.Grid.Length, a);
        s.V = new NativeArray<int>(math.max(width, height), a);
        s.Z = new NativeArray<float>(math.max(width, height) + 1, a);
    }

    public static void BuildObstacleField(ref EdtState s, NativeArray<byte> blocked, NativeArray<float> dist2)
    {
        // dist2[i] = 0 for obstacle, +inf otherwise.
    }

    public static void TransformRows(ref EdtState s, NativeArray<float> dist2)
    {
        // 1D lower envelope of parabolas per row.
    }

    public static void TransformColumns(ref EdtState s, NativeArray<float> dist2)
    {
        // 1D lower envelope of parabolas per column.
    }

    public static void Build(ref EdtState s, NativeArray<byte> blocked, NativeArray<float> dist2)
    {
        BuildObstacleField(ref s, blocked, dist2);
        TransformRows(ref s, dist2);
        TransformColumns(ref s, dist2);
    }

    public static void Dispose(ref EdtState s)
    {
        if (s.Temp.IsCreated) s.Temp.Dispose();
        if (s.V.IsCreated) s.V.Dispose();
        if (s.Z.IsCreated) s.Z.Dispose();
    }
}
```

**Graph Cuts / α-expansion** | Grid labeling by repeated min-cut.

```csharp
public struct CutEdge
{
    public int A;
    public int B;
    public int Capacity;
}

public struct GraphCutState
{
    public Grid2D Grid;
    public NativeList<CutEdge> Edges;
    public NativeArray<int> Excess;
    public NativeArray<int> Height;
    public NativeArray<byte> SourceSide;
}

public struct GraphCutApi
{
    public static void Setup(ref GraphCutState s, int width, int height, int maxEdges, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Edges = new NativeList<CutEdge>(maxEdges, a);
        s.Excess = new NativeArray<int>(s.Grid.Length + 2, a);
        s.Height = new NativeArray<int>(s.Grid.Length + 2, a);
        s.SourceSide = new NativeArray<byte>(s.Grid.Length, a);
    }

    public static void BuildBinaryEnergy(
        ref GraphCutState s,
        NativeArray<int> unary0,
        NativeArray<int> unary1,
        NativeArray<int> pairwise)
    {
        s.Edges.Clear();

        // Add source/sink capacities.
        // Add grid-neighbor pairwise edges.
    }

    public static bool MinCut(ref GraphCutState s)
    {
        // Push-relabel or Dinic over flattened edge arrays.
        // Mark source-reachable residual side.

        return true;
    }

    public static void ApplyCutLabels(ref GraphCutState s, NativeArray<int> labels, int label0, int label1)
    {
        // labels[i] = SourceSide[i] ? label0 : label1.
    }

    public static void AlphaExpansion(ref GraphCutState s, NativeArray<int> labels, int alpha, NativeArray<int> unary, NativeArray<int> smooth)
    {
        // Build binary keep-vs-alpha energy.
        // MinCut.
        // Switch source/sink side cells to alpha.
    }

    public static void Dispose(ref GraphCutState s)
    {
        if (s.Edges.IsCreated) s.Edges.Dispose();
        if (s.Excess.IsCreated) s.Excess.Dispose();
        if (s.Height.IsCreated) s.Height.Dispose();
        if (s.SourceSide.IsCreated) s.SourceSide.Dispose();
    }
}
```

**Dynamic Graph Cuts** | Reuse residual flow after small energy edits.

```csharp
public struct DynamicCutState
{
    public GraphCutState Cut;
    public NativeList<int> DirtyNodes;
    public NativeList<int> Orphans;
}

public struct DynamicCutApi
{
    public static void Setup(ref DynamicCutState s, int width, int height, int maxEdges, Allocator a)
    {
        GraphCutApi.Setup(ref s.Cut, width, height, maxEdges, a);
        s.DirtyNodes = new NativeList<int>(width * height, a);
        s.Orphans = new NativeList<int>(width * height, a);
    }

    public static void EditUnary(ref DynamicCutState s, int cell, int sourceCapDelta, int sinkCapDelta)
    {
        s.DirtyNodes.Add(cell);

        // Modify terminal capacities in residual graph.
    }

    public static void EditPairwise(ref DynamicCutState s, int a, int b, int capacityDelta)
    {
        s.DirtyNodes.Add(a);
        s.DirtyNodes.Add(b);

        // Modify residual edge capacities.
    }

    public static bool Repair(ref DynamicCutState s)
    {
        // Reuse old search trees / residual graph.
        // Process orphans.
        // Resume augmenting/push-relabel only near dirty area.

        return true;
    }

    public static void Dispose(ref DynamicCutState s)
    {
        GraphCutApi.Dispose(ref s.Cut);
        if (s.DirtyNodes.IsCreated) s.DirtyNodes.Dispose();
        if (s.Orphans.IsCreated) s.Orphans.Dispose();
    }
}
```

**Loopy Belief Propagation** | Message passing on grid factor graph.

```csharp
public struct BeliefState
{
    public Grid2D Grid;
    public int LabelCount;
    public NativeArray<float> Unary;     // cell * L + label
    public NativeArray<float> Messages;  // cell * 4 * L + dir * L + label
    public NativeArray<float> Belief;    // cell * L + label
    public NativeArray<float> Scratch;
}

public struct BeliefApi
{
    public static void Setup(ref BeliefState s, int width, int height, int labelCount, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.LabelCount = labelCount;
        s.Unary = new NativeArray<float>(s.Grid.Length * labelCount, a);
        s.Messages = new NativeArray<float>(s.Grid.Length * 4 * labelCount, a);
        s.Belief = new NativeArray<float>(s.Grid.Length * labelCount, a);
        s.Scratch = new NativeArray<float>(labelCount, a);
    }

    public static void ClearMessages(ref BeliefState s)
    {
        s.Messages.Fill(0f);
    }

    public static void SetUnary(ref BeliefState s, NativeArray<float> unary)
    {
        s.Unary.CopyFrom(unary);
    }

    public static void Iterate(ref BeliefState s, NativeArray<float> pairwise, int iterations)
    {
        // For each directed edge u->v:
        //   msg(label_v) = min/sum over label_u:
        //      unary(u,label_u) + incoming except v + pairwise(label_u,label_v)
        // Normalize message.
    }

    public static void DecodeMap(ref BeliefState s, NativeArray<int> labels)
    {
        // For each cell, choose label with best belief.
    }

    public static void Dispose(ref BeliefState s)
    {
        if (s.Unary.IsCreated) s.Unary.Dispose();
        if (s.Messages.IsCreated) s.Messages.Dispose();
        if (s.Belief.IsCreated) s.Belief.Dispose();
        if (s.Scratch.IsCreated) s.Scratch.Dispose();
    }
}
```

**Watershed Transform** | Flood scalar grid from minima into basins.

```csharp
public struct WatershedState
{
    public Grid2D Grid;
    public NativeArray<int> Label;
    public NativeArray<byte> State;
    public NativeList<HeapNode> Flood;
}

public struct WatershedApi
{
    public static void Setup(ref WatershedState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Label = new NativeArray<int>(s.Grid.Length, a);
        s.State = new NativeArray<byte>(s.Grid.Length, a);
        s.Flood = new NativeList<HeapNode>(s.Grid.Length, a);
    }

    public static void FindMinima(ref WatershedState s, NativeArray<float> height)
    {
        s.Label.Fill(-1);
        s.State.Fill(0);
        s.Flood.Clear();

        // Assign unique basin labels to local minima plateaus.
        // Push basin boundary cells into priority queue keyed by height.
    }

    public static void Flood(ref WatershedState s, NativeArray<float> height)
    {
        // Pop lowest cell.
        // Adopt neighbor basin label.
        // If competing labels meet, mark watershed ridge.
    }

    public static void ExtractBoundaries(ref WatershedState s, NativeArray<byte> boundary)
    {
        boundary.Fill(0);

        // boundary[i] = 1 if adjacent labels differ.
    }

    public static void Dispose(ref WatershedState s)
    {
        if (s.Label.IsCreated) s.Label.Dispose();
        if (s.State.IsCreated) s.State.Dispose();
        if (s.Flood.IsCreated) s.Flood.Dispose();
    }
}
```

**Morse-Smale / Persistent Watershed** | Critical points + persistence simplification.

```csharp
public struct CriticalPoint
{
    public int Cell;
    public byte Type; // min, saddle, max
    public float Value;
    public int Pair;
    public float Persistence;
}

public struct MorseState
{
    public Grid2D Grid;
    public NativeArray<int> Ascending;
    public NativeArray<int> Descending;
    public NativeList<CriticalPoint> Critical;
    public NativeArray<int> Component;
}

public struct MorseApi
{
    public static void Setup(ref MorseState s, int width, int height, int maxCritical, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Ascending = new NativeArray<int>(s.Grid.Length, a);
        s.Descending = new NativeArray<int>(s.Grid.Length, a);
        s.Critical = new NativeList<CriticalPoint>(maxCritical, a);
        s.Component = new NativeArray<int>(s.Grid.Length, a);
    }

    public static void BuildGradient(ref MorseState s, NativeArray<float> scalar)
    {
        // For each cell, choose steepest lower and upper neighbor.
        // Mark cells with no lower/upper as critical candidates.
    }

    public static void TraceManifolds(ref MorseState s)
    {
        // Follow ascending/descending pointers.
        // Assign each cell to critical endpoints.
    }

    public static void PairByPersistence(ref MorseState s, NativeArray<float> scalar)
    {
        // Sort/process merge tree conceptually.
        // Pair extrema with saddles.
        // Store persistence.
    }

    public static void Simplify(ref MorseState s, float threshold)
    {
        // Cancel pairs below threshold.
        // Relabel affected basins/manifolds.
    }

    public static void Dispose(ref MorseState s)
    {
        if (s.Ascending.IsCreated) s.Ascending.Dispose();
        if (s.Descending.IsCreated) s.Descending.Dispose();
        if (s.Critical.IsCreated) s.Critical.Dispose();
        if (s.Component.IsCreated) s.Component.Dispose();
    }
}
```

**Topology-preserving thinning** | Delete simple boundary pixels without changing topology.

```csharp
public struct ThinningState
{
    public Grid2D Grid;
    public NativeArray<byte> Mark;
    public NativeList<int> Frontier;
}

public struct ThinningApi
{
    public static void Setup(ref ThinningState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Mark = new NativeArray<byte>(s.Grid.Length, a);
        s.Frontier = new NativeList<int>(s.Grid.Length, a);
    }

    public static void InitializeFrontier(ref ThinningState s, NativeArray<byte> solid)
    {
        s.Mark.Fill(0);
        s.Frontier.Clear();

        // Add boundary solid cells.
    }

    public static bool IsSimplePoint(ref ThinningState s, NativeArray<byte> solid, int cell)
    {
        // Check 8-neighborhood connectivity:
        //   foreground component count unchanged
        //   background component count unchanged

        return false;
    }

    public static void Iterate(ref ThinningState s, NativeArray<byte> solid)
    {
        s.Mark.Fill(0);

        // Mark deletable simple boundary cells.
        // Delete marked cells.
        // Refresh affected frontier.
    }

    public static void ExtractSkeleton(ref ThinningState s, NativeArray<byte> solid, NativeList<int> skeleton)
    {
        skeleton.Clear();

        // Append remaining solid cells.
    }

    public static void Dispose(ref ThinningState s)
    {
        if (s.Mark.IsCreated) s.Mark.Dispose();
        if (s.Frontier.IsCreated) s.Frontier.Dispose();
    }
}
```

**Wave Function Collapse / Model Synthesis** | Local CSP propagation on a grid.

```csharp
public struct WfcState
{
    public Grid2D Grid;
    public int PatternCount;
    public NativeArray<ulong> PossibleBits;
    public NativeArray<int> Entropy;
    public NativeArray<ulong> Compatibility; // pattern * 4 + dir
    public NativeQueue<int> Queue;
}

public struct WfcApi
{
    public static void Setup(ref WfcState s, int width, int height, int patternCount, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.PatternCount = patternCount;
        s.PossibleBits = new NativeArray<ulong>(s.Grid.Length, a);
        s.Entropy = new NativeArray<int>(s.Grid.Length, a);
        s.Compatibility = new NativeArray<ulong>(patternCount * 4, a);
        s.Queue = new NativeQueue<int>(a);
    }

    public static void InitializeAllPossible(ref WfcState s)
    {
        ulong all = 0UL;

        // Set low PatternCount bits in all.
        // PossibleBits[i] = all.
        // Entropy[i] = PatternCount.
    }

    public static void LearnAdjacency(ref WfcState s, NativeArray<int> sample, int sampleWidth, int sampleHeight)
    {
        s.Compatibility.Fill(0UL);

        // Extract pattern ids from sample.
        // Fill Compatibility[pattern,dir] bitsets.
    }

    public static bool Observe(ref WfcState s, int cell, int chosenPattern)
    {
        // Collapse cell to chosenPattern.
        // Enqueue cell.

        return true;
    }

    public static bool Propagate(ref WfcState s)
    {
        // While queue non-empty:
        //   restrict neighbor possible bits by compatibility.
        //   if zero possibilities, contradiction.

        return true;
    }

    public static bool Run(ref WfcState s, NativeArray<int> output)
    {
        // Repeatedly choose min-entropy cell.
        // Observe random weighted pattern.
        // Propagate.
        // Decode singleton pattern per cell.

        return true;
    }

    public static void Dispose(ref WfcState s)
    {
        if (s.PossibleBits.IsCreated) s.PossibleBits.Dispose();
        if (s.Entropy.IsCreated) s.Entropy.Dispose();
        if (s.Compatibility.IsCreated) s.Compatibility.Dispose();
        if (s.Queue.IsCreated) s.Queue.Dispose();
    }
}
```

**Domino tiling via height functions** | Grid tilings as integer potentials.

```csharp
public struct DominoState
{
    public Grid2D Grid;
    public NativeArray<byte> Region;
    public NativeArray<int> Height;
    public NativeArray<byte> MatchingDir;
}

public struct DominoApi
{
    public static void Setup(ref DominoState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Region = new NativeArray<byte>(s.Grid.Length, a);
        s.Height = new NativeArray<int>(s.Grid.Length, a);
        s.MatchingDir = new NativeArray<byte>(s.Grid.Length, a);
    }

    public static bool CheckTileableByParity(ref DominoState s)
    {
        // Count checkerboard black/white cells inside region.

        return true;
    }

    public static bool BuildTilingByMatching(ref DominoState s)
    {
        // Build bipartite graph between black/white adjacent cells.
        // Run Hopcroft-Karp on flattened adjacency.
        // Store matched direction per cell.

        return true;
    }

    public static void BuildHeightFunction(ref DominoState s)
    {
        s.Height.Fill(0);

        // Traverse dual grid.
        // Apply +/- height change depending on whether edge crosses domino.
    }

    public static bool FlipAt(ref DominoState s, int cell)
    {
        // If two parallel dominoes form a 2x2 block:
        //   rotate them.
        //   update local height.

        return false;
    }

    public static void Dispose(ref DominoState s)
    {
        if (s.Region.IsCreated) s.Region.Dispose();
        if (s.Height.IsCreated) s.Height.Dispose();
        if (s.MatchingDir.IsCreated) s.MatchingDir.Dispose();
    }
}
```

**Kasteleyn / Pfaffian planar matching** | Count domino tilings using signed matrix determinant/Pfaffian.

```csharp
public struct KasteleynState
{
    public Grid2D Grid;
    public NativeArray<byte> Region;
    public NativeList<int2> Edges;
    public NativeArray<double> Matrix;
    public int VertexCount;
}

public struct KasteleynApi
{
    public static void Setup(ref KasteleynState s, int width, int height, int maxEdges, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Region = new NativeArray<byte>(s.Grid.Length, a);
        s.Edges = new NativeList<int2>(maxEdges, a);
        s.Matrix = new NativeArray<double>(s.Grid.Length * s.Grid.Length, a);
        s.VertexCount = 0;
    }

    public static void BuildPlanarGraph(ref KasteleynState s)
    {
        s.Edges.Clear();

        // Add adjacency edges between region cells.
        // Compact active cells to vertex ids.
    }

    public static void OrientKasteleyn(ref KasteleynState s)
    {
        s.Matrix.Fill(0.0);

        // Assign signs so every face has odd clockwise orientation.
        // Fill skew-symmetric matrix.
    }

    public static bool CountPerfectMatchings(ref KasteleynState s, out double count)
    {
        count = 0.0;

        // Compute determinant of skew matrix.
        // count = sqrt(abs(det)).
        // For exactness, replace double with modular arithmetic / big integer outside Burst.

        return true;
    }

    public static void Dispose(ref KasteleynState s)
    {
        if (s.Region.IsCreated) s.Region.Dispose();
        if (s.Edges.IsCreated) s.Edges.Dispose();
        if (s.Matrix.IsCreated) s.Matrix.Dispose();
    }
}
```

**Wilson’s uniform spanning tree** | Loop-erased random walks for unbiased mazes.

```csharp
public struct WilsonState
{
    public Grid2D Grid;
    public NativeArray<byte> InTree;
    public NativeArray<int> Parent;
    public NativeArray<int> WalkNext;
    public NativeList<int> Walk;
}

public struct WilsonApi
{
    public static void Setup(ref WilsonState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.InTree = new NativeArray<byte>(s.Grid.Length, a);
        s.Parent = new NativeArray<int>(s.Grid.Length, a);
        s.WalkNext = new NativeArray<int>(s.Grid.Length, a);
        s.Walk = new NativeList<int>(s.Grid.Length, a);
    }

    public static void Initialize(ref WilsonState s, int root)
    {
        s.InTree.Fill(0);
        s.Parent.Fill(-1);
        s.WalkNext.Fill(-1);
        s.Walk.Clear();

        s.InTree[root] = 1;
    }

    public static void AddRandomWalk(ref WilsonState s, ref Unity.Mathematics.Random rng, int start)
    {
        s.Walk.Clear();

        // Random walk until reaching InTree.
        // If revisiting a cell in current walk, erase loop.
        // Commit loop-erased path into Parent/InTree.
    }

    public static void BuildTree(ref WilsonState s, ref Unity.Mathematics.Random rng)
    {
        // For every cell not in tree:
        //   AddRandomWalk.
    }

    public static void ExtractMazeWalls(ref WilsonState s, NativeArray<byte> walls)
    {
        // Convert Parent tree edges into removed walls/passages.
    }

    public static void Dispose(ref WilsonState s)
    {
        if (s.InTree.IsCreated) s.InTree.Dispose();
        if (s.Parent.IsCreated) s.Parent.Dispose();
        if (s.WalkNext.IsCreated) s.WalkNext.Dispose();
        if (s.Walk.IsCreated) s.Walk.Dispose();
    }
}
```

**Coupling From The Past** | Exact stationary sampling from monotone grid Markov chain.

```csharp
public struct CftpUpdate
{
    public int Cell;
    public uint RandomBits;
}

public struct CftpState
{
    public Grid2D Grid;
    public NativeArray<byte> Low;
    public NativeArray<byte> High;
    public NativeList<CftpUpdate> Updates;
}

public struct CftpApi
{
    public static void Setup(ref CftpState s, int width, int height, int maxUpdates, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Low = new NativeArray<byte>(s.Grid.Length, a);
        s.High = new NativeArray<byte>(s.Grid.Length, a);
        s.Updates = new NativeList<CftpUpdate>(maxUpdates, a);
    }

    public static void InitializeExtremes(ref CftpState s)
    {
        s.Low.Fill(0);
        s.High.Fill(1);
    }

    public static void GeneratePastUpdates(ref CftpState s, ref Unity.Mathematics.Random rng, int count)
    {
        s.Updates.Clear();

        // Generate update schedule for times [-count, 0).
        // Prepend logically by storing in chronological replay order.
    }

    public static void Replay(ref CftpState s)
    {
        InitializeExtremes(ref s);

        // Apply same updates to Low and High chains.
        // Use monotone local transition.
    }

    public static bool Coalesced(ref CftpState s)
    {
        for (int i = 0; i < s.Grid.Length; i++)
        {
            if (s.Low[i] != s.High[i])
            {
                return false;
            }
        }

        return true;
    }

    public static bool SampleExact(ref CftpState s, ref Unity.Mathematics.Random rng, NativeArray<byte> sample)
    {
        // Double the past horizon until Coalesced.
        // Copy Low/High to sample when coalesced.

        return false;
    }

    public static void Dispose(ref CftpState s)
    {
        if (s.Low.IsCreated) s.Low.Dispose();
        if (s.High.IsCreated) s.High.Dispose();
        if (s.Updates.IsCreated) s.Updates.Dispose();
    }
}
```

**Hashlife-like cellular automata cache** | Quad-tree time skipping; awkward in pure `Native*`, but possible with flat node tables.

```csharp
public struct HashlifeNode
{
    public int Level;
    public int Child00;
    public int Child10;
    public int Child01;
    public int Child11;
    public ulong Hash;
    public int Result;
}

public struct HashlifeState
{
    public NativeList<HashlifeNode> Nodes;
    public NativeParallelHashMap<ulong, int> Intern;
    public NativeParallelHashMap<ulong, int> ResultCache;
}

public struct HashlifeApi
{
    public static void Setup(ref HashlifeState s, int maxNodes, Allocator a)
    {
        s.Nodes = new NativeList<HashlifeNode>(maxNodes, a);
        s.Intern = new NativeParallelHashMap<ulong, int>(maxNodes, a);
        s.ResultCache = new NativeParallelHashMap<ulong, int>(maxNodes, a);
    }

    public static void Clear(ref HashlifeState s)
    {
        s.Nodes.Clear();
        s.Intern.Clear();
        s.ResultCache.Clear();
    }

    public static bool InternNode(ref HashlifeState s, HashlifeNode node, out int id)
    {
        id = -1;

        // Hash children+level.
        // If exists, output existing id.
        // Else append node and map hash->id.

        return true;
    }

    public static bool StepPowerOfTwo(ref HashlifeState s, int node, int stepsPow2, out int resultNode)
    {
        resultNode = -1;

        // Recursive memoized result:
        //   split into overlapping subnodes
        //   compute centers
        //   cache by node+stepsPow2

        return false;
    }

    public static void Decode(ref HashlifeState s, int root, NativeArray<byte> cells, Grid2D grid)
    {
        // Traverse flat quadtree into cells.
    }

    public static void Dispose(ref HashlifeState s)
    {
        if (s.Nodes.IsCreated) s.Nodes.Dispose();
        if (s.Intern.IsCreated) s.Intern.Dispose();
        if (s.ResultCache.IsCreated) s.ResultCache.Dispose();
    }
}
```

**Abelian Sandpile / chip-firing** | Order-independent avalanche relaxation.

```csharp
public struct SandpileState
{
    public Grid2D Grid;
    public NativeArray<int> Grains;
    public NativeQueue<int> Queue;
    public NativeArray<byte> InQueue;
}

public struct SandpileApi
{
    public static void Setup(ref SandpileState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Grains = new NativeArray<int>(s.Grid.Length, a);
        s.Queue = new NativeQueue<int>(a);
        s.InQueue = new NativeArray<byte>(s.Grid.Length, a);
    }

    public static void Clear(ref SandpileState s)
    {
        s.Grains.Fill(0);
        s.InQueue.Fill(0);
        s.Queue.Clear();
    }

    public static void AddGrains(ref SandpileState s, int cell, int amount)
    {
        s.Grains[cell] += amount;

        if (s.Grains[cell] >= 4 && s.InQueue[cell] == 0)
        {
            s.Queue.Enqueue(cell);
            s.InQueue[cell] = 1;
        }
    }

    public static bool RelaxStep(ref SandpileState s)
    {
        int cell = -1;

        if (s.Queue.TryDequeue(out cell))
        {
            s.InQueue[cell] = 0;

            // toppleCount = Grains[cell] / 4
            // Grains[cell] %= 4
            // Add toppleCount to 4-neighbors
            // Enqueue neighbors that become unstable
        }

        return s.Queue.Count > 0;
    }

    public static void RelaxAll(ref SandpileState s)
    {
        while (s.Queue.Count > 0)
        {
            RelaxStep(ref s);
        }
    }

    public static void Dispose(ref SandpileState s)
    {
        if (s.Grains.IsCreated) s.Grains.Dispose();
        if (s.Queue.IsCreated) s.Queue.Dispose();
        if (s.InQueue.IsCreated) s.InQueue.Dispose();
    }
}
```

The reusable ECS pattern is:

```csharp
public struct AlgorithmState
{
    public Grid2D Grid;
    public NativeArray<int> Dense;
    public NativeList<int> Sparse;
    public NativeQueue<int> Frontier;
}

public struct AlgorithmApi
{
    public static void Setup(ref AlgorithmState s, int width, int height, Allocator a)
    {
        s.Grid.Setup(width, height);
        s.Dense = new NativeArray<int>(s.Grid.Length, a);
        s.Sparse = new NativeList<int>(s.Grid.Length, a);
        s.Frontier = new NativeQueue<int>(a);
    }

    public static void Reset(ref AlgorithmState s)
    {
        s.Dense.Fill(0);
        s.Sparse.Clear();
        s.Frontier.Clear();
    }

    public static bool Run<TCell>(
        ref AlgorithmState s,
        NativeArray<TCell> cells,
        NativeList<int> output)
        where TCell : unmanaged
    {
        output.Clear();

        // Algorithm body over cells + scratch buffers.

        return true;
    }

    public static void Dispose(ref AlgorithmState s)
    {
        if (s.Dense.IsCreated) s.Dense.Dispose();
        if (s.Sparse.IsCreated) s.Sparse.Dispose();
        if (s.Frontier.IsCreated) s.Frontier.Dispose();
    }
}
```

For real Unity ECS, I would keep these `State` structs owned by a system or passed into jobs, not stored per-entity. Per-entity components should store plain indices/ranges/handles into large system-owned native buffers.

[1]: https://docs.unity3d.com/Packages/com.unity.burst%401.6/manual/docs/CSharpLanguageSupport_Types.html?utm_source=chatgpt.com "C#/.NET Language Support | Burst | 1.6.6"
