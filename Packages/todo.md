# TODO

Each item is self-contained. Fix one, move to next. Do not skip. Do not reorder.
A checkbox means the change compiles and all tests in that file still pass.

---

## CRITICAL — Fix these first. They crash or produce wrong answers.

### TODO-01 — `CbsApi.TryAStar` allocates 100M+ elements on Temp allocator

**File:** `com.bovinelabs.grid.cbs/Runtime/CbsApi.cs`

**Problem:** `timeHorizon = math.max(100, gridLen)`. For a 100×100 grid that is
10 000 × 10 000 = 100 M floats per array, plus an equal-size MinHeap. This
crashes at runtime.

**Exact change:**

Replace this block:
```cs
var g = new Unity.Collections.NativeArray<float>(gridLen * timeHorizon, Allocator.Temp);
var parent = new Unity.Collections.NativeArray<int>(gridLen * timeHorizon, Allocator.Temp);
```

With:
```cs
var timeHorizon = math.max(64, (s.Grid.Width + s.Grid.Height) * 2);
var g = new Unity.Collections.NativeArray<float>(gridLen * timeHorizon, Allocator.Temp);
var parent = new Unity.Collections.NativeArray<int>(gridLen * timeHorizon, Allocator.Temp);
```

Also replace the `MinHeap.TryCreate` call on the line below from
`gridLen * timeHorizon` to the same `gridLen * timeHorizon` computed value
(it will now use the smaller number automatically because the local variable
`timeHorizon` is already changed).

- [ ] Done

---

### TODO-02 — `AnyaApi.PushNode` scans entire pool on every insert (O(n²))

**File:** `com.bovinelabs.grid.anya/Runtime/AnyaApi.cs`

**Problem:** Every call to `PushNode` linearly scans all existing nodes for a
duplicate. With `maxNodes = 10 000` this is 10 000 × 10 000 = 100 M comparisons
per search.

**Step 1 — Add a lookup map to `AnyaState`.**

**File:** `com.bovinelabs.grid.anya/Runtime/AnyaState.cs`

Add one field to the struct:
```cs
public NativeHashMap<ulong, int> NodeLookup;
```

**Step 2 — Allocate and dispose the map.**

**File:** `com.bovinelabs.grid.anya/Runtime/AnyaApi.cs`, method `TryCreate`:

After `Pool = new UnsafeList<AnyaNode>(maxNodes, a.ToAllocator),` add:
```cs
NodeLookup = new NativeHashMap<ulong, int>(maxNodes, a.ToAllocator),
```

**File:** `AnyaState.cs`, method `Dispose`:

After `if (Pool.IsCreated) Pool.Dispose();` add:
```cs
if (NodeLookup.IsCreated) NodeLookup.Dispose();
```

**Step 3 — Add a hash helper.**

**File:** `AnyaApi.cs`, add this private static method anywhere in the class:
```cs
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static ulong NodeKey(double L, double R, int y, double2 root)
{
    ulong h = (ulong)(long)(L * 1000003) ^ (ulong)(long)(R * 999983);
    h ^= (ulong)y * 2654435761UL;
    h ^= (ulong)(long)(root.x * 1000033) * 40503UL;
    h ^= (ulong)(long)(root.y * 999979) * 40507UL;
    return h == 0 ? 1 : h;
}
```

**Step 4 — Replace the loop in `PushNode`.**

Find:
```cs
for (var i = 0; i < s.Pool.Length; i++)
    if (NodeEquals(s.Pool[i], L, R, y, root))
    {
        if (s.Pool[i].RootG <= rootG + EPS) return;
        s.Pool[i] = new AnyaNode { ... };
        ...
        s.Heap.TryInsertOrDecrease(new DoubleHeapNode(i, f));
        return;
    }
```

Replace with:
```cs
var key = NodeKey(L, R, y, root);
if (s.NodeLookup.TryGetValue(key, out var existingIdx))
{
    if (s.Pool[existingIdx].RootG <= rootG + EPS) return;
    s.Pool[existingIdx] = new AnyaNode { L = L, R = R, y = y, dy = dy, Root = root, RootG = rootG, Parent = parent };
    double xInt = goal.x;
    if (goal.y != root.y)
    {
        var ratio = (y - root.y) / (goal.y - root.y);
        xInt = root.x + (goal.x - root.x) * ratio;
    }
    var xOpt = math.max(L, math.min(R, xInt));
    var f = rootG + math.distance(root, new double2(xOpt, y)) +
            math.distance(new double2(xOpt, y), new double2(goal.x, goal.y));
    s.Heap.TryInsertOrDecrease(new DoubleHeapNode(existingIdx, f));
    return;
}
```

**Step 5 — Register new nodes in the lookup.**

Immediately after the line `s.Pool.Add(new AnyaNode { ... });` at the bottom of
`PushNode` (the non-duplicate path), add:
```cs
s.NodeLookup.TryAdd(key, idx);
```

**Step 6 — Clear the lookup in `TryInitSearch`.**

In `TryInitSearch`, after `s.Pool.Clear();` add:
```cs
s.NodeLookup.Clear();
```

- [ ] Done

---

### TODO-03 — `GraphCutApi.AddEdgeInternal` has no capacity guard

**File:** `com.bovinelabs.grid.graphcut/Runtime/GraphCutApi.cs`

**Problem:** The method is `void` and adds unconditionally. If edges exceed
`maxEdges`, `UnsafeList` reallocates silently, violating the stated budget.

**Change the signature from:**
```cs
public static void AddEdgeInternal(ref GraphCutState s, int u, int v, int cap)
```
**To:**
```cs
public static bool AddEdgeInternal(ref GraphCutState s, int u, int v, int cap)
```

At the top of the method body, add:
```cs
if (s.EdgeTo.Length + 2 > s.EdgeTo.Capacity) return false;
```

At the end of the method body, add:
```cs
return true;
```

Every call site that ignores the return value (`DominoApi.TryBuildTilingByMatching`,
`GraphCutApi.BuildBinaryEnergy`, `DynamicCutApi`) must now check it:

Pattern to apply at every call site:
```cs
if (!GraphCutApi.AddEdgeInternal(ref cut, ...)) return false;
```

- [ ] Done

---

### TODO-04 — `WavestarBuilderJob` uses `Allocator.Temp` inside a job thread

**File:** `com.bovinelabs.grid.wavestar/Runtime/WavestarBuilder.cs`

**Problem:** `Allocator.Temp` is not valid on job worker threads. Unity will
throw or silently corrupt.

Find every `new NativeList<...>(Allocator.Temp)` inside
`WavestarBuilderJob.Execute` and `WavestarBuilderJob.TryPropagate` and change
`Allocator.Temp` to `Allocator.TempJob`.

Then ensure `queue.Dispose()` is called before the method returns (it already
is in the existing code — verify the call is present after the while-loop).

Same fix applies to `WavestarPathExtractorJob.Execute`: find
`Allocator.Temp` uses and change to `Allocator.TempJob`, verify `Dispose`
calls exist.

- [ ] Done

---

## CORRECTNESS — Wrong results in specific cases.

### TODO-05 — `DStarLiteApi.TryRepair` uses bit-exact float equality

**File:** `com.bovinelabs.grid.dstarlite/Runtime/DStarLiteApi.cs`

**Problem:** The pre-processing loop removes consistent nodes:
```cs
if (gPtr[openTop.Id] != rhsPtr[openTop.Id]) break;
```
Float `!=` fails to detect near-equal values that are mathematically equal
after floating-point rounding, leaving stale entries in the open set.

**Change to:**
```cs
if (math.abs(gPtr[openTop.Id] - rhsPtr[openTop.Id]) > 1e-6f) break;
```

The same pattern appears inside `TryRepair` at:
```cs
if (gPtr[uid] > rhsPtr[uid])
```
That is a relational comparison, which is fine — leave it unchanged.

Also change the consistency guard further down:
```cs
if (!LessOrEqual(openTop2.Key0, openTop2.Key1, topKey.x, topKey.y) &&
    rhsPtr[s.Start] == gPtr[s.Start])
    return true;
```
Change `rhsPtr[s.Start] == gPtr[s.Start]` to
`math.abs(rhsPtr[s.Start] - gPtr[s.Start]) <= 1e-6f`.

- [ ] Done

---

### TODO-06 — `KasteleynApi.TryOrientKasteleyn` assigns wrong sign for horizontal edges

**File:** `com.bovinelabs.grid.kasteleyn/Runtime/KasteleynApi.cs`

**Problem:** A valid Kasteleyn orientation requires every interior face to have
an odd number of clockwise edges. The current rule assigns `sign = 1` to all
horizontal edges regardless of row parity. For rows where this creates an even
count, the determinant will not equal the square of the matching count.

**Replace the orientation block:**
```cs
int sign;
if (ax == bx) sign = ax % 2 == 0 ? 1 : -1;
else sign = 1;
```

**With:**
```cs
var ay = cellA / w;
int sign;
if (ax == bx)
    sign = ax % 2 == 0 ? 1 : -1;
else
    sign = ay % 2 == 0 ? 1 : -1;
```

- [ ] Done

---

### TODO-07 — `SandpileApi` silently destroys boundary grains; conservation test hides this

**File:** `com.bovinelabs.grid.sandpile/Tests/SandpileTests.cs`

**Problem:** The test `Relax_Conservation` uses a centre cell where no grains
reach the boundary. It passes trivially. Add a test that reveals the open-
boundary behaviour explicitly so future readers know it is intentional.

Add this test to `SandpileTests.cs`:
```cs
[Test]
public unsafe void Relax_BoundaryLosesGrains()
{
    Assert.IsTrue(SandpileApi.TryCreate(3, 3, Allocator.Temp, out var s));
    SandpileApi.AddGrains(ref s, 0, 4);
    SandpileApi.RelaxAll(ref s);
    var total = 0;
    for (var i = 0; i < s.Grid.Length; i++) total += s.Grains[i];
    Assert.Less(total, 4, "Corner topple loses grains to open boundary");
    SandpileApi.Dispose(ref s);
}
```

- [ ] Done

---

### TODO-08 — `CpdApi.TryBuild` run encoding groups wrong when first-moves alternate

**File:** `com.bovinelabs.grid.cpd/Runtime/CpdApi.cs`

**Problem:** Runs are closed when `fm[target] != currentMove`. If reachable
targets alternate between move-0 and move-1 (common after BFS ordering), every
pair of targets creates a separate run, negating compression entirely. Sort
targets by first-move value before the run-encoding loop.

After the line:
```cs
for (var i = 0; i < len; i++) fm[i] = 255;
```

And after the first-move assignment loop that sets `fm[target]`, but **before**
the run-encoding loop that emits `CpdRun` entries, insert a stable index sort:

```cs
var sortedTargets = new NativeArray<int>(len, Allocator.Temp);
var sortCount = 0;
for (var t = 0; t < len; t++)
    if (fm[t] != 255) sortedTargets[sortCount++] = t;

sortedTargets.GetSubArray(0, sortCount).Sort(
    new FmComparer { Fm = fm });
```

Add this private struct inside `CpdApi` (at the bottom, before the closing
brace):
```cs
private struct FmComparer : IComparer<int>
{
    [NativeDisableUnsafePtrRestriction] public byte* Fm;
    public int Compare(int a, int b) => Fm[a].CompareTo(Fm[b]);
}
```

Change the run-encoding loop from iterating `for (var target = 0; target < len; target++)`
to iterating `for (var si = 0; si < sortCount; si++)` where `target = sortedTargets[si]`.

Dispose `sortedTargets` before `firstMove.Dispose()`.

Add `using System.Collections.Generic;` at the top of the file if not present.

- [ ] Done

---

### TODO-09 — `HashlifeApi.FlatHashMap.Add` silently drops entries when full

**File:** `com.bovinelabs.grid.hashlife/Runtime/HashlifeApi.cs`

**Problem:** When the linear-probe loop exhausts all slots, `Add` returns
without inserting, silently losing the entry. Subsequent lookups miss it,
causing incorrect deduplication in `TryInternNode`.

**Change `Add` signature from `void` to `bool`:**
```cs
public unsafe bool Add(ulong key, int value)
```

At the end of the `for` loop body, before the final `return;` add the return
value `return true;`.

After the loop, replace the implicit fall-through with:
```cs
return false;
```

**Change `TryInternNode` to propagate the failure:**
```cs
private static bool TryInternNode(ref HashlifeState s, ref HashlifeNode node, out int id)
{
    ...
    if (!s.Intern.Add(h, id)) { id = -1; return false; }
    return true;
}
```

Update all callers of `TryInternNode` and `TryMakeNode` to check the bool.

- [ ] Done

---

## DEAD CODE — Delete every item in this section. Do not refactor. Delete.

### TODO-10 — Delete `NativeMinHeap`

**File:** `com.bovinelabs.grid/Runtime/IPathfinder.cs`

Delete the entire struct `NativeMinHeap` (everything from
`[Obsolete("Use MinHeap...")]` through the matching closing `}`).

- [ ] Done

---

### TODO-11 — Delete `IPathfinder`

**File:** `com.bovinelabs.grid/Runtime/IPathfinder.cs`

Delete the entire interface `IPathfinder`.

After TODO-10 and TODO-11, the file should be empty of types. Delete the file
entirely and remove it from any `.asmdef` references.

- [ ] Done

---

### TODO-12 — Delete `PathNode`

**File:** `com.bovinelabs.grid/Runtime/GridTypes.cs`

Delete the entire struct `PathNode` (the struct block, not the file).

- [ ] Done

---

### TODO-13 — Delete `GridNeighbors.Cardinal` and `GridNeighbors.Diagonal`

**File:** `com.bovinelabs.grid/Runtime/GridTypes.cs`

Delete these two lines:
```cs
public static readonly int2[] Cardinal = { ... };
public static readonly int2[] Diagonal = { ... };
```

Leave `CardinalCost`, `DiagonalCost`, `GetNeighbors4`, `GetNeighbors8`, and
`IsDiagonalPassable` — these are used.

- [ ] Done

---

### TODO-14 — Delete `ExtendedCell`, `MeshSearchNode`, `PrimEndpoint`

**File:** `com.bovinelabs.grid.mesha/Runtime/MeshATypes.cs`

Delete the entire struct `ExtendedCell`.
Delete the entire struct `MeshSearchNode`.
Delete the entire struct `PrimEndpoint`.

- [ ] Done

---

### TODO-15 — Delete `MultiResCostField`

**File:** `com.bovinelabs.grid.wavestar/Runtime/WavestarTypes.cs`

Delete the entire struct `MultiResCostField` (from `public struct MultiResCostField`
through its closing `}`).

- [ ] Done

---

### TODO-16 — Delete `UnsafeFastQueue<T>`

**File:** `com.bovinelabs.grid/Runtime/UnsafeQueue.cs`

Delete the entire struct `UnsafeFastQueue<T>`. The file will be empty; delete
the file and remove it from any `.asmdef` references.

- [ ] Done

---

## ALLOCATOR CONSISTENCY — `MinHeap` and `DoubleMinHeap` use legacy allocator API.

### TODO-17 — Migrate `MinHeap` to `AllocatorManager.AllocatorHandle`

**File:** `com.bovinelabs.grid/Runtime/MinHeap.cs`

**Step 1 — Change the stored allocator type:**
```cs
public Allocator Allocator;
```
Change to:
```cs
public AllocatorManager.AllocatorHandle Allocator;
```

**Step 2 — Change `TryCreate` parameter:**
```cs
public static bool TryCreate(int maxId, Allocator allocator, out MinHeap result)
```
Change to:
```cs
public static bool TryCreate(int maxId, AllocatorManager.AllocatorHandle allocator, out MinHeap result)
```

**Step 3 — Change `UnsafeUtility.Malloc` calls to `AllocatorManager.Allocate`:**

Replace:
```cs
Data = (HeapNode*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<HeapNode>() * maxId,
    UnsafeUtility.AlignOf<HeapNode>(), allocator),
Positions = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * maxId,
    UnsafeUtility.AlignOf<int>(), allocator),
```
With:
```cs
Data = (HeapNode*)AllocatorManager.Allocate(allocator,
    UnsafeUtility.SizeOf<HeapNode>(), UnsafeUtility.AlignOf<HeapNode>(), maxId),
Positions = (int*)AllocatorManager.Allocate(allocator,
    UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), maxId),
```

**Step 4 — Change `Dispose` to use `AllocatorManager.Free`:**

Replace:
```cs
UnsafeUtility.Free(Data, Allocator);
UnsafeUtility.Free(Positions, Allocator);
```
With:
```cs
AllocatorManager.Free(Allocator, Data);
AllocatorManager.Free(Allocator, Positions);
```

**Step 5 — Update every call site of `MinHeap.TryCreate`** in the codebase.
Search for `MinHeap.TryCreate(` — every occurrence passes `Allocator.Temp`,
`Allocator.Persistent`, or `Allocator.TempJob`. These are implicitly
convertible to `AllocatorManager.AllocatorHandle`, so no call-site change is
needed if the conversion operator exists in the Unity version in use. If the
compiler complains, wrap: `new AllocatorManager.AllocatorHandle { Index = (int)Allocator.Temp }`.

Apply the identical four-step migration to `DoubleMinHeap` in
`com.bovinelabs.grid/Runtime/DoubleMinHeap.cs`.

- [ ] Done

---

## IDISPOSABLE — Add `IDisposable` to all state structs that own unmanaged memory.

### TODO-18 — Add `IDisposable` to the 18 structs that are missing it

For each struct listed below, add `: IDisposable` to the struct declaration and
add the method shown. The method body is always the same pattern: call the
existing `XxxApi.Dispose(ref this)`.

| Struct | File | Method to add |
|---|---|---|
| `WfcState` | `WfcApi.cs` | `public void Dispose() => WfcApi.Dispose(ref this);` |
| `SandpileState` | `SandpileApi.cs` | `public void Dispose() => SandpileApi.Dispose(ref this);` |
| `SippState` | `SippApi.cs` | `public void Dispose() => SippApi.Dispose(ref this);` |
| `SubgoalState` | `SubgoalApi.cs` | `public void Dispose() => SubgoalApi.Dispose(ref this);` |
| `ThinningState` | `ThinningApi.cs` | `public void Dispose() => ThinningApi.Dispose(ref this);` |
| `WatershedState` | `WatershedApi.cs` | `public void Dispose() => WatershedApi.Dispose(ref this);` |
| `FieldDStarState` | `FieldDStarApi.cs` | `public void Dispose() => FieldDStarApi.Dispose(ref this);` |
| `FastMarchingState` | `FastMarchingApi.cs` | `public void Dispose() => FastMarchingApi.Dispose(ref this);` |
| `FastSweepingState` | `FastSweepingApi.cs` | `public void Dispose() => FastSweepingApi.Dispose(ref this);` |
| `WilsonState` | `WilsonApi.cs` | `public void Dispose() => WilsonApi.Dispose(ref this);` |
| `MorseState` | `MorseApi.cs` | `public void Dispose() => MorseApi.Dispose(ref this);` |
| `RsrState` | `RsrApi.cs` | `public void Dispose() => RsrApi.Dispose(ref this);` |
| `KasteleynState` | `KasteleynApi.cs` | `public void Dispose() => KasteleynApi.Dispose(ref this);` |
| `CftpState` | `CftpApi.cs` | `public void Dispose() => CftpApi.Dispose(ref this);` |
| `ContinuumCrowdState` | `ContinuumCrowdApi.cs` | `public void Dispose() => ContinuumCrowdApi.Dispose(ref this);` |
| `DStarLiteState` | `DStarLiteApi.cs` | `public void Dispose() => DStarLiteApi.Dispose(ref this);` |
| `JpsState` | `JpsApi.cs` | `public void Dispose() => JpsApi.Dispose(ref this);` |
| `EdtState` | `EdtApi.cs` | `public void Dispose() => EdtApi.Dispose(ref this);` |

Each struct declaration line must become e.g.:
```cs
public unsafe struct WfcState : IDisposable
```

- [ ] Done

---

## MISSING TESTS

Each TODO below adds tests to an existing test file. Do not create new files.

---

### TODO-19 — `DStarLiteApi`: test replanning after obstacle added

**File:** `com.bovinelabs.grid.dstarlite/Tests/DStarLiteTests.cs`

Add after the existing tests:
```cs
[Test]
public unsafe void Replan_AfterObstacleAdded_PathChanges()
{
    Assert.IsTrue(DStarLiteApi.TryCreate(10, 10, Allocator.Temp, out var s));
    var b = new NativeArray<byte>(100, Allocator.Temp);
    b.Fill((byte)0);
    var cost = new NativeArray<float>(0, Allocator.Temp);
    var path1 = new NativeList<int>(Allocator.Temp);
    var path2 = new NativeList<int>(Allocator.Temp);

    Assert.IsTrue(DStarLiteApi.TryInitialize(ref s, 0, 99, b));
    Assert.IsTrue(DStarLiteApi.TryRepair(ref s, b, cost, 10000));
    Assert.IsTrue(DStarLiteApi.TryExtractPath(ref s, b, cost, ref path1));
    Assert.Greater(path1.Length, 0);

    b[s.Grid.ToIndex(5, 0)] = 1;
    b[s.Grid.ToIndex(5, 1)] = 1;
    b[s.Grid.ToIndex(5, 2)] = 1;
    b[s.Grid.ToIndex(5, 3)] = 1;
    b[s.Grid.ToIndex(5, 4)] = 1;

    Assert.IsTrue(DStarLiteApi.TryUpdateCell(ref s, b, cost, s.Grid.ToIndex(5, 0)));
    Assert.IsTrue(DStarLiteApi.TryUpdateCell(ref s, b, cost, s.Grid.ToIndex(5, 1)));
    Assert.IsTrue(DStarLiteApi.TryUpdateCell(ref s, b, cost, s.Grid.ToIndex(5, 2)));
    Assert.IsTrue(DStarLiteApi.TryUpdateCell(ref s, b, cost, s.Grid.ToIndex(5, 3)));
    Assert.IsTrue(DStarLiteApi.TryUpdateCell(ref s, b, cost, s.Grid.ToIndex(5, 4)));

    Assert.IsTrue(DStarLiteApi.TryRepair(ref s, b, cost, 10000));
    Assert.IsTrue(DStarLiteApi.TryExtractPath(ref s, b, cost, ref path2));
    Assert.Greater(path2.Length, 0);

    for (var i = 0; i < path2.Length; i++)
        Assert.AreEqual(0, b[path2[i]], $"Replanned path must not cross new obstacle at step {i}");

    DStarLiteApi.Dispose(ref s);
    b.Dispose();
    cost.Dispose();
    path1.Dispose();
    path2.Dispose();
}
```

- [ ] Done

---

### TODO-20 — `FieldDStarApi`: test `TryExtractFlow`

**File:** `com.bovinelabs.grid.fielddstar/Tests/FieldDStarTests.cs`

Add:
```cs
[Test]
public unsafe void ExtractFlow_HasNonzeroVectors()
{
    Assert.IsTrue(FieldDStarApi.TryCreate(5, 5, Allocator.Temp, out var s));
    var cost = new NativeArray<float>(25, Allocator.Temp);
    for (var i = 0; i < 25; i++) cost[i] = 1f;
    Assert.IsTrue(FieldDStarApi.TryReset(ref s));
    Assert.IsTrue(FieldDStarApi.TrySetGoal(ref s, 24));
    while (FieldDStarApi.TryStep(ref s, cost)) { }
    Assert.IsTrue(FieldDStarApi.TryExtractFlow(ref s, cost));
    var nonzero = false;
    for (var i = 0; i < s.Grid.Length; i++)
        if (Unity.Mathematics.math.length(s.Flow[i]) > 0.01f)
            nonzero = true;
    Assert.IsTrue(nonzero, "Flow field must contain non-zero vectors after solve");
    FieldDStarApi.Dispose(ref s);
    cost.Dispose();
}
```

- [ ] Done

---

### TODO-21 — `CftpApi`: test `TrySampleExact`

**File:** `com.bovinelabs.grid.cftp/Tests/CftpTests.cs`

Add:
```cs
[Test]
public unsafe void SampleExact_Coalesces()
{
    Assert.IsTrue(CftpApi.TryCreate(4, 4, 10000, Allocator.Temp, out var s));
    var rng = new Unity.Mathematics.Random(7);
    var sample = new NativeArray<byte>(s.Grid.Length, Allocator.Temp);
    Assert.IsTrue(CftpApi.TrySampleExact(ref s, ref rng, ref sample));
    for (var i = 0; i < sample.Length; i++)
        Assert.IsTrue(sample[i] == 0 || sample[i] == 1, "Sample must be binary");
    CftpApi.Dispose(ref s);
    sample.Dispose();
}
```

- [ ] Done

---

### TODO-22 — `HashlifeApi`: test `TryGetResult` and `Decode`

**File:** `com.bovinelabs.grid.hashlife/Tests/HashlifeTests.cs`

Add:
```cs
[Test]
public unsafe void GetResult_Level2_StillAlive()
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
public unsafe void Decode_OutputSizeMatchesGrid()
{
    Assert.IsTrue(HashlifeApi.TryCreate(1000, Allocator.Temp, out var s));
    HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var nw);
    HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var ne);
    HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var sw);
    HashlifeApi.TryMakeNode(ref s, 0, 0, 0, 0, out var se);
    HashlifeApi.TryMakeNode(ref s, nw, ne, sw, se, out var root);
    var grid = BovineLabs.Grid.Grid2D.Create(4, 4);
    var cells = new NativeArray<byte>(16, Allocator.Temp);
    HashlifeApi.Decode(ref s, root, cells, grid);
    Assert.AreEqual(16, cells.Length);
    HashlifeApi.Dispose(ref s);
    cells.Dispose();
}
```

- [ ] Done

---

### TODO-23 — `KasteleynApi`: test `TryCountPerfectMatchings` result

**File:** `com.bovinelabs.grid.kasteleyn/Tests/KasteleynTests.cs`

Add:
```cs
[Test]
public unsafe void CountPerfectMatchings_2x2_Returns2()
{
    Assert.IsTrue(KasteleynApi.TryCreate(2, 2, 10, Allocator.Temp, out var s));
    var region = new NativeArray<byte>(4, Allocator.Temp);
    region.Fill((byte)1);
    KasteleynApi.SetRegion(ref s, region);
    Assert.IsTrue(KasteleynApi.TryBuildPlanarGraph(ref s));
    Assert.IsTrue(KasteleynApi.TryOrientKasteleyn(ref s));
    Assert.IsTrue(KasteleynApi.TryCountPerfectMatchings(ref s, out var count));
    Assert.AreEqual(2.0, count, 0.5, "2x2 full region has exactly 2 perfect matchings");
    KasteleynApi.Dispose(ref s);
    region.Dispose();
}

[Test]
public unsafe void CountPerfectMatchings_OddRegion_Zero()
{
    Assert.IsTrue(KasteleynApi.TryCreate(3, 1, 10, Allocator.Temp, out var s));
    var region = new NativeArray<byte>(3, Allocator.Temp);
    region.Fill((byte)1);
    KasteleynApi.SetRegion(ref s, region);
    Assert.IsTrue(KasteleynApi.TryBuildPlanarGraph(ref s));
    Assert.IsTrue(KasteleynApi.TryOrientKasteleyn(ref s));
    Assert.IsTrue(KasteleynApi.TryCountPerfectMatchings(ref s, out var count));
    Assert.AreEqual(0.0, count, 0.5, "Odd-size region cannot be perfectly tiled");
    KasteleynApi.Dispose(ref s);
    region.Dispose();
}
```

- [ ] Done

---

### TODO-24 — `MorseApi`: test `TryPairByPersistence` and `TrySimplify`

**File:** `com.bovinelabs.grid.morse/Tests/MorseTests.cs`

Add:
```cs
[Test]
public unsafe void PairByPersistence_MaximaGetPaired()
{
    Assert.IsTrue(MorseApi.TryCreate(5, 1, 100, Allocator.Temp, out var s));
    var scalar = new NativeArray<float>(5, Allocator.Temp);
    scalar[0] = 0f; scalar[1] = 5f; scalar[2] = 1f; scalar[3] = 4f; scalar[4] = 0f;
    Assert.IsTrue(MorseApi.TryBuildGradient(ref s, in scalar));
    Assert.IsTrue(MorseApi.TryPairByPersistence(ref s, in scalar));
    var paired = false;
    for (var i = 0; i < s.Critical.Length; i++)
        if (s.Critical.Ptr[i].Pair >= 0) paired = true;
    Assert.IsTrue(paired, "At least one critical point must be paired");
    MorseApi.Dispose(ref s);
    scalar.Dispose();
}

[Test]
public unsafe void Simplify_RemovesLowPersistenceMaxima()
{
    Assert.IsTrue(MorseApi.TryCreate(5, 1, 100, Allocator.Temp, out var s));
    var scalar = new NativeArray<float>(5, Allocator.Temp);
    scalar[0] = 0f; scalar[1] = 5f; scalar[2] = 1f; scalar[3] = 4f; scalar[4] = 0f;
    Assert.IsTrue(MorseApi.TryBuildGradient(ref s, in scalar));
    Assert.IsTrue(MorseApi.TryPairByPersistence(ref s, in scalar));
    Assert.IsTrue(MorseApi.TrySimplify(ref s, in scalar, 2.0f));
    MorseApi.Dispose(ref s);
    scalar.Dispose();
}
```

- [ ] Done

---

### TODO-25 — `DominoApi`: test `TryBuildHeightFunction`

**File:** `com.bovinelabs.grid.domino/Tests/DominoTests.cs`

Add:
```cs
[Test]
public unsafe void BuildHeightFunction_2x2_AllAssigned()
{
    Assert.IsTrue(DominoApi.TryCreate(2, 2, Allocator.Temp, out var s));
    var region = new NativeArray<byte>(4, Allocator.Temp);
    region.Fill((byte)1);
    DominoApi.SetRegion(ref s, region);
    Assert.IsTrue(DominoApi.TryBuildHeightFunction(ref s));
    var allAssigned = true;
    for (var i = 0; i < 4; i++)
        if (s.Region[i] != 0 && s.Height[i] == 0 && i != 0)
            allAssigned = false;
    Assert.IsTrue(allAssigned);
    DominoApi.Dispose(ref s);
    region.Dispose();
}
```

- [ ] Done

---

### TODO-26 — `WfcApi`: test output satisfies learned adjacency

**File:** `com.bovinelabs.grid.wfc/Tests/WfcTests.cs`

Add:
```cs
[Test]
public unsafe void Run_OutputSatisfiesAdjacency()
{
    Assert.IsTrue(WfcApi.TryCreate(4, 4, 2, Allocator.Temp, out var s));
    var sample = new NativeArray<int>(new[] {
        0, 0, 1, 1,
        0, 0, 1, 1,
        0, 0, 1, 1,
        0, 0, 1, 1
    }, Allocator.Temp);
    Assert.IsTrue(WfcApi.TryLearnAdjacency(ref s, sample, 4, 4));
    var output = new NativeArray<int>(16, Allocator.Temp);
    var rng = new Unity.Mathematics.Random(7);
    Assert.IsTrue(WfcApi.TryRun(ref s, ref output, ref rng));
    for (var i = 0; i < output.Length; i++)
        Assert.IsTrue(output[i] == 0 || output[i] == 1, $"Cell {i} must be 0 or 1");
    WfcApi.Dispose(ref s);
    sample.Dispose();
    output.Dispose();
}
```

- [ ] Done

---

### TODO-27 — `AnyaApi`: test incremental `TryInitSearch` + `TryStepSearch`

**File:** `com.bovinelabs.grid.anya/Tests/AnyaTests.cs`

Add:
```cs
[Test]
public unsafe void Incremental_StepByStep_SameResultAsTrySearch()
{
    Assert.IsTrue(AnyaApi.TryCreate(10, 10, 10000, Allocator.Temp, out var s));
    var blocked = new NativeArray<byte>(100, Allocator.Temp);
    blocked.Fill((byte)0);
    var startV = new int2(0, 0);
    var goalV = new int2(9, 9);

    var path1 = new NativeList<int2>(Allocator.Temp);
    Assert.IsTrue(AnyaApi.TrySearch(ref s, blocked, ref startV, ref goalV, ref path1));

    var path2 = new NativeList<int2>(Allocator.Temp);
    var startV2 = new int2(0, 0);
    var goalV2 = new int2(9, 9);
    Assert.IsTrue(AnyaApi.TryInitSearch(ref s, blocked, ref startV2, ref goalV2));
    if (s.SearchComplete == 0)
    {
        var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
        while (!s.Heap.IsEmpty)
            if (AnyaApi.TryStepSearch(ref s, blocked)) break;
    }
    Assert.IsTrue(AnyaApi.TryExtractPath(ref s, ref path2));
    Assert.AreEqual(path1.Length, path2.Length, "Incremental and batch paths must have same length");

    AnyaApi.Dispose(ref s);
    blocked.Dispose();
    path1.Dispose();
    path2.Dispose();
}
```

- [ ] Done

---

### TODO-28 — `SubgoalApi`: test path cost on a map that actually creates subgoals

**File:** `com.bovinelabs.grid.subgoal/Tests/SubgoalTests.cs`

Add:
```cs
[Test]
public unsafe void Search_WithSubgoals_PathIsOptimalOrNear()
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
```

- [ ] Done

---

### TODO-29 — `EdtApi`: verify `TryBuildParallel` matches `TryBuild`

**File:** `com.bovinelabs.grid.edt/Tests/EdtTests.cs`

Add:
```cs
[Test]
public unsafe void BuildParallel_MatchesBuildSerial()
{
    Assert.IsTrue(EdtApi.TryCreate(8, 8, Allocator.Temp, out var s));
    var b = new NativeArray<byte>(64, Allocator.Temp);
    b.Fill((byte)0);
    b[s.Grid.ToIndex(3, 3)] = 1;
    b[s.Grid.ToIndex(6, 1)] = 1;

    var d1 = new NativeArray<float>(64, Allocator.Temp);
    var d2 = new NativeArray<float>(64, Allocator.Temp);

    Assert.IsTrue(EdtApi.TryBuild(ref s, in b, ref d1));
    Assert.IsTrue(EdtApi.TryBuildParallel(ref s, in b, ref d2, 8));

    for (var i = 0; i < 64; i++)
        Assert.AreEqual(d1[i], d2[i], 0.001f, $"Cell {i} parallel result must match serial");

    EdtApi.Dispose(ref s);
    b.Dispose();
    d1.Dispose();
    d2.Dispose();
}
```

- [ ] Done

---

## DONE

All items above complete = the codebase is correct by construction, alive by
composition, and testable at every seam.
