# TODO.md — Atomic Grid Package Creation Plan

Purpose: split the grid algorithm work into the smallest practical, testable Unity/Burst packages. Each package must be understandable alone, testable alone, and replaceable alone. Do not wire packages into a final product yet.

This TODO is written for a weak AI executor. Follow it literally. Do not infer missing behavior. Do not combine packages. Do not skip tests. Do not create clever abstractions before the package earns them.

---

## 0. Execution Contract

### 0.1 Absolute Rules

- [ ] Build one package at a time.
- [ ] Do not wire packages together into a final system yet.
- [ ] Do not create cross-package behavior unless the current package explicitly lists that dependency.
- [ ] Do not create hidden global state.
- [ ] Do not create singleton services.
- [ ] Do not allocate inside hot loops unless the task explicitly allows it.
- [ ] Do not throw exceptions for normal failure paths. Use `Try*` APIs that return `bool`.
- [ ] Do not mutate inputs unless the API name says so.
- [ ] Do not leave output containers with stale data on failure.
- [ ] Do not make partial functions public. Every public API must validate inputs.
- [ ] Do not accept invalid dimensions, invalid indices, uncreated arrays, undersized arrays, negative capacities, or null pointers silently.
- [ ] Do not use random tests without fixed seeds.
- [ ] Do not use timing-sensitive tests as correctness tests.
- [ ] Do not mark a task complete until its tests pass.

### 0.2 Naming Rules

- [ ] Package names must describe domain, not implementation mood.
- [ ] Public types must be nouns: `Grid2D`, `GridBounds`, `PathBuffer`, `MinHeap`.
- [ ] Public operations must be verbs: `TryCreate`, `TryClear`, `TryInsert`, `TrySearch`, `Dispose`.
- [ ] Boolean-returning APIs must begin with `Try`, `Is`, `Has`, or `Can`.
- [ ] Mutating APIs must say what they mutate: `ClearMessages`, `NormalizeCosts`, `ResetState`.
- [ ] Test names must state condition and expected result: `Search_BlockedGoal_ReturnsFalseAndEmptyPath`.
- [ ] No abbreviation unless already standard in the domain: `LOS` is allowed only inside tests; public API should say `LineOfSight`.

### 0.3 Definition of Done for Every Package

A package is done only when all items below are checked.

- [ ] Has `package.json` with exact name, version, display name, description, dependencies.
- [ ] Has `Runtime/` folder.
- [ ] Has `Tests/` folder.
- [ ] Has runtime `.asmdef`.
- [ ] Has test `.asmdef` that references runtime `.asmdef`.
- [ ] Has `README.md` with purpose, API, invariants, examples, non-goals.
- [ ] Has no dependency not declared in `package.json`.
- [ ] Has one public API surface file unless multiple files are required for separate data types.
- [ ] Has state structs with explicit ownership and `Dispose` when native memory is owned.
- [ ] Has tests for creation.
- [ ] Has tests for invalid input.
- [ ] Has tests for success path.
- [ ] Has tests for deterministic repeatability.
- [ ] Has tests for capacity failure if the package owns fixed-capacity memory.
- [ ] Has tests for double-dispose if the package owns native memory.
- [ ] Has tests proving no stale output after failure.
- [ ] Has all public functions deterministic for the same inputs.
- [ ] Has no implicit dependency on scene objects, MonoBehaviours, time, random state, or static mutable data.
- [ ] Has Burst-compatible runtime code where practical.
- [ ] Has comments only for invariants, tricky math, memory ownership, and domain rules.
- [ ] Has no commentary comments explaining obvious syntax.

### 0.4 Package README Template

Each package `README.md` must contain exactly these sections:

```md
# <package-name>

## Purpose
One sentence.

## Public API
List exported structs and methods.

## Invariants
List facts that must always be true.

## Failure Model
List every reason each `Try*` method can return false.

## Determinism
State what inputs fully determine outputs.

## Memory Ownership
State who allocates, who disposes, and whether outputs are cleared on failure.

## Non-Goals
List what this package must not do.

## Minimal Example
Small code example.

## Test Categories
List tests by category.
```

### 0.5 Standard Test Categories

Every package must use these categories where applicable:

- [ ] `Creation`
- [ ] `InputValidation`
- [ ] `Correctness`
- [ ] `Determinism`
- [ ] `Capacity`
- [ ] `Memory`
- [ ] `Fuzz`
- [ ] `Oracle`
- [ ] `Regression`

---

## 1. Dependency Layers

Build packages in this order. Do not build a higher layer until all lower-layer dependencies are complete.

```text
Layer 0: Irreducible primitives
  grid.coordinates
  grid.bounds
  grid.indexing
  grid.costs
  grid.native-memory

Layer 1: Small reusable structures
  grid.heap
  grid.path-buffer
  grid.occupancy
  grid.neighborhood
  grid.edge-policy

Layer 2: Small geometric and graph primitives
  grid.line-of-sight
  grid.relaxation
  grid.distance-field-core
  grid.oracle

Layer 3: Single-purpose algorithms
  grid.astar
  grid.jps
  grid.anya
  grid.dstarlite
  grid.field-dstar
  grid.fast-sweeping
  grid.belief-propagation
  grid.cbs
  grid.wavestar

Layer 4: Shared validation only
  grid.test-fixtures
  grid.fuzzing
  grid.benchmarks
```

Important: Layer 4 is for tests and measurement only. It must not become production runtime glue.

---

## 2. Package Creation Skeleton

For each package below, create this folder structure:

```text
com.bovinelabs.<package-name>/
  package.json
  README.md
  Runtime/
    BovineLabs.<Namespace>.asmdef
    <MainApiOrStruct>.cs
  Tests/
    BovineLabs.<Namespace>.Tests.asmdef
    <PackageName>Tests.Creation.cs
    <PackageName>Tests.InputValidation.cs
    <PackageName>Tests.Correctness.cs
```

For packages with owned native memory, also add:

```text
    <PackageName>Tests.Memory.cs
    <PackageName>Tests.Capacity.cs
```

For packages with randomized validation, also add:

```text
    <PackageName>Tests.Fuzz.cs
    <PackageName>Tests.Oracle.cs
```

---

# Layer 0 — Irreducible Primitives

## P00 — `com.bovinelabs.grid.coordinates`

### Purpose
Represent grid coordinates without knowing about storage, obstacles, search, pathfinding, or algorithms.

### Allowed Dependencies
- `Unity.Mathematics`

### Runtime Exports
- `GridCoord2`
- `GridCoord3`
- `GridCoord2Extensions`
- `GridCoord3Extensions`

### Required Behavior
- [ ] Store integer coordinates only.
- [ ] Support equality.
- [ ] Support conversion to `int2` and `int3`.
- [ ] Support Manhattan delta.
- [ ] Support Chebyshev delta.
- [ ] Support Octile delta for 2D.
- [ ] Support squared Euclidean delta.
- [ ] No bounds checking here.
- [ ] No index conversion here.
- [ ] No grid width/height fields here.

### Tests
- [ ] `Coord2_EqualValues_AreEqual`
- [ ] `Coord2_DifferentValues_AreNotEqual`
- [ ] `Coord2_ToInt2_RoundTrips`
- [ ] `Coord2_ManhattanDelta_ReturnsAbsDxPlusAbsDy`
- [ ] `Coord2_ChebyshevDelta_ReturnsMaxAbsAxis`
- [ ] `Coord2_OctileDelta_IsDeterministic`
- [ ] `Coord3_ToInt3_RoundTrips`

### DoD
- [ ] No allocation.
- [ ] All functions pure.
- [ ] All functions total for all integer inputs.

---

## P01 — `com.bovinelabs.grid.bounds`

### Purpose
Represent rectangular 2D and 3D grid bounds and answer in-bounds questions.

### Allowed Dependencies
- `Unity.Mathematics`
- `com.bovinelabs.grid.coordinates`

### Runtime Exports
- `GridBounds2D`
- `GridBounds3D`

### Required Behavior
- [ ] `TryCreate(width, height, out bounds)` returns false for width <= 0 or height <= 0.
- [ ] `TryCreate(width, height, depth, out bounds)` returns false for any dimension <= 0.
- [ ] `Length` is `width * height` for 2D.
- [ ] `Length` is `width * height * depth` for 3D.
- [ ] Detect integer overflow and return false.
- [ ] `InBounds(x, y)` is true only when `0 <= x < width` and `0 <= y < height`.
- [ ] `InBounds(index)` is true only when `0 <= index < Length`.
- [ ] No allocation.

### Tests
- [ ] `Create_Valid2D_ReturnsTrue`
- [ ] `Create_ZeroWidth_ReturnsFalse`
- [ ] `Create_NegativeHeight_ReturnsFalse`
- [ ] `Create_Overflow_ReturnsFalse`
- [ ] `InBounds_MinCorner_ReturnsTrue`
- [ ] `InBounds_MaxExclusive_ReturnsFalse`
- [ ] `InBounds_Negative_ReturnsFalse`
- [ ] `Length_Valid2D_EqualsWidthTimesHeight`
- [ ] `Length_Valid3D_EqualsWidthTimesHeightTimesDepth`

### DoD
- [ ] All functions pure.
- [ ] All functions total.
- [ ] No hidden conversion to index.

---

## P02 — `com.bovinelabs.grid.indexing`

### Purpose
Convert between bounded grid coordinates and flat array indices.

### Allowed Dependencies
- `Unity.Mathematics`
- `com.bovinelabs.grid.coordinates`
- `com.bovinelabs.grid.bounds`

### Runtime Exports
- `GridIndex2D`
- `GridIndex3D`

### Required Behavior
- [ ] `TryToIndex(bounds, x, y, out index)` validates bounds and coordinate.
- [ ] `TryToCoord(bounds, index, out coord)` validates bounds and index.
- [ ] `ToIndexUnchecked(bounds, x, y)` exists only for internal/hot code and is clearly named unchecked.
- [ ] `ToCoordUnchecked(bounds, index)` exists only for internal/hot code and is clearly named unchecked.
- [ ] 2D index formula is `x + y * width`.
- [ ] 3D index formula is `x + z * width + y * width * depth` only if that layout is explicitly chosen in README.
- [ ] README must state exact layout.

### Tests
- [ ] `TryToIndex_InBounds_ReturnsExpectedIndex`
- [ ] `TryToIndex_OutOfBounds_ReturnsFalse`
- [ ] `TryToCoord_InBounds_ReturnsExpectedCoord`
- [ ] `TryToCoord_OutOfBounds_ReturnsFalse`
- [ ] `RoundTrip_AllCells2D_ReturnsOriginalCoord`
- [ ] `RoundTrip_AllCells3D_ReturnsOriginalCoord`

### DoD
- [ ] No allocation.
- [ ] All checked public APIs are total.
- [ ] Unchecked APIs are marked as unchecked in name and README.

---

## P03 — `com.bovinelabs.grid.costs`

### Purpose
Provide deterministic grid distance and cost formulas without knowing about search state.

### Allowed Dependencies
- `Unity.Mathematics`
- `com.bovinelabs.grid.coordinates`

### Runtime Exports
- `GridCost`
- `GridHeuristic`

### Required Behavior
- [ ] Manhattan distance.
- [ ] Chebyshev distance.
- [ ] Octile distance.
- [ ] Euclidean distance.
- [ ] Squared Euclidean distance.
- [ ] `IsFiniteNonNegative(float value)`.
- [ ] No grid bounds.
- [ ] No blocked cells.
- [ ] No allocation.

### Tests
- [ ] `Manhattan_AdjacentOrthogonal_ReturnsOne`
- [ ] `Manhattan_Diagonal_ReturnsTwo`
- [ ] `Chebyshev_Diagonal_ReturnsOne`
- [ ] `Octile_SamePoint_ReturnsZero`
- [ ] `Euclidean_ThreeFourFive_ReturnsFive`
- [ ] `AllDistances_AreSymmetric`
- [ ] `AllDistances_SamePoint_ReturnZero`

### DoD
- [ ] All functions pure.
- [ ] All functions deterministic.
- [ ] No algorithm-specific names.

---

## P04 — `com.bovinelabs.grid.native-memory`

### Purpose
Centralize safe, explicit native allocation helpers for fixed-size Burst-compatible buffers.

### Allowed Dependencies
- `Unity.Collections`
- `Unity.Collections.LowLevel.Unsafe`

### Runtime Exports
- `NativeBufferGuard`
- `NativeAllocationMath`

### Required Behavior
- [ ] Validate count >= 0.
- [ ] Validate element size > 0.
- [ ] Detect byte-size overflow.
- [ ] Return false on invalid allocation request.
- [ ] Provide `ClearBytes` wrapper.
- [ ] Provide `DisposeIfCreated` patterns for raw pointers.
- [ ] No domain-specific grid logic.

### Tests
- [ ] `SizeBytes_Valid_ReturnsExpected`
- [ ] `SizeBytes_NegativeCount_ReturnsFalse`
- [ ] `SizeBytes_Overflow_ReturnsFalse`
- [ ] `ClearBytes_SetsAllBytesToZero`
- [ ] `Dispose_NullPointer_DoesNotThrow`

### DoD
- [ ] Every unsafe method has exact ownership documented.
- [ ] No hidden allocations.
- [ ] No package above this duplicates allocation math.

---

# Layer 1 — Small Reusable Structures

## P05 — `com.bovinelabs.grid.heap`

### Purpose
Provide fixed-capacity deterministic min-heaps for search frontiers.

### Allowed Dependencies
- `Unity.Collections`
- `Unity.Collections.LowLevel.Unsafe`
- `Unity.Mathematics`
- `com.bovinelabs.grid.native-memory`

### Runtime Exports
- `HeapNode`
- `MinHeap`
- `DoubleHeapNode`
- `DoubleMinHeap`

### Required Behavior
- [ ] `TryCreate(capacity, allocator, out heap)` returns false for capacity <= 0.
- [ ] `TryInsertOrDecrease(node)` inserts new IDs.
- [ ] `TryInsertOrDecrease(node)` decreases existing ID when new key is lower.
- [ ] `TryInsertOrDecrease(node)` does not increase existing key.
- [ ] `TryPop(out node)` returns lowest key.
- [ ] Tie-breaks are deterministic.
- [ ] `Clear()` resets heap without freeing memory.
- [ ] `Dispose()` can be called twice safely.
- [ ] Capacity failure returns false.

### Tests
- [ ] `Create_ValidCapacity_CreatesHeap`
- [ ] `Create_ZeroCapacity_ReturnsFalse`
- [ ] `InsertThenPop_ReturnsLowestKeyFirst`
- [ ] `InsertOrDecrease_DecreasesExistingKey`
- [ ] `InsertOrDecrease_DoesNotIncreaseExistingKey`
- [ ] `TieBreak_SameKeys_ReturnsDeterministicOrder`
- [ ] `Pop_Empty_ReturnsFalse`
- [ ] `CapacityFull_ReturnsFalse`
- [ ] `Clear_RemovesAllNodes`
- [ ] `Dispose_Double_DoesNotThrow`

### DoD
- [ ] Heap does not know about grids.
- [ ] Heap does not allocate after create.
- [ ] Heap is deterministic.

---

## P06 — `com.bovinelabs.grid.path-buffer`

### Purpose
Provide small path validation and output-buffer helpers shared by algorithms.

### Allowed Dependencies
- `Unity.Collections`
- `Unity.Mathematics`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`

### Runtime Exports
- `PathBuffer2D`
- `PathBuffer3D`
- `PathValidation`

### Required Behavior
- [ ] Clear output before write.
- [ ] Clear output on failure.
- [ ] Validate start and goal when provided.
- [ ] Validate all waypoints in bounds.
- [ ] Validate no duplicate adjacent waypoints if requested.
- [ ] Provide `TryReverseInPlace`.
- [ ] Provide `TryCopy`.
- [ ] No algorithm logic.

### Tests
- [ ] `ClearOnFailure_RemovesStalePath`
- [ ] `Validate_InBoundsPath_ReturnsTrue`
- [ ] `Validate_OutOfBoundsWaypoint_ReturnsFalse`
- [ ] `Validate_WrongStart_ReturnsFalse`
- [ ] `Validate_WrongGoal_ReturnsFalse`
- [ ] `ReverseInPlace_ReversesPath`
- [ ] `Copy_UndersizedDestination_ReturnsFalseAndClearsDestination`

### DoD
- [ ] Does not know about blocked cells.
- [ ] Does not know about line of sight.
- [ ] Does not allocate.

---

## P07 — `com.bovinelabs.grid.occupancy`

### Purpose
Represent blocked/free grid cells with total checked accessors.

### Allowed Dependencies
- `Unity.Collections`
- `Unity.Collections.LowLevel.Unsafe`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`

### Runtime Exports
- `OccupancyGrid2D`
- `OccupancyGrid3D`
- `OccupancyReadOnly`

### Required Behavior
- [ ] Wrap `NativeArray<byte>` or raw pointer plus bounds.
- [ ] `TryCreate(bounds, blocked, out occupancy)` validates array is created and length >= bounds.Length.
- [ ] `IsBlocked(index)` checked version returns false via `TryIsBlocked`.
- [ ] `IsBlockedUnchecked(index)` is allowed only for internal/hot code.
- [ ] `IsFree` is exact inverse of `IsBlocked` for valid cells.
- [ ] No pathfinding behavior.

### Tests
- [ ] `Create_ValidArray_ReturnsTrue`
- [ ] `Create_UncreatedArray_ReturnsFalse`
- [ ] `Create_ShortArray_ReturnsFalse`
- [ ] `TryIsBlocked_BlockedCell_ReturnsTrueValue`
- [ ] `TryIsBlocked_FreeCell_ReturnsFalseValue`
- [ ] `TryIsBlocked_OutOfBounds_ReturnsFalse`

### DoD
- [ ] Occupancy stores no ownership unless explicitly named owning.
- [ ] Checked APIs never read out of bounds.
- [ ] Unchecked APIs are named unchecked.

---

## P08 — `com.bovinelabs.grid.neighborhood`

### Purpose
Generate neighboring cell offsets and indices without running search.

### Allowed Dependencies
- `Unity.Mathematics`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`

### Runtime Exports
- `GridDirection2D`
- `GridDirection3D`
- `Neighborhood2D`
- `Neighborhood3D`

### Required Behavior
- [ ] Provide 4-connected 2D directions.
- [ ] Provide 8-connected 2D directions.
- [ ] Provide 6-connected 3D directions.
- [ ] Provide 26-connected 3D directions if needed by Wavestar.
- [ ] Return neighbor count deterministically.
- [ ] Never return out-of-bounds neighbors from checked APIs.
- [ ] No obstacle checks.
- [ ] No corner-cutting policy here.

### Tests
- [ ] `FourConnected_Center_ReturnsFour`
- [ ] `FourConnected_Corner_ReturnsTwo`
- [ ] `EightConnected_Center_ReturnsEight`
- [ ] `EightConnected_Corner_ReturnsThree`
- [ ] `SixConnected3D_Center_ReturnsSix`
- [ ] `AllNeighbors_AreInBounds`
- [ ] `Order_IsDeterministic`

### DoD
- [ ] No allocation.
- [ ] No search-specific state.
- [ ] No blocked-cell reads.

---

## P09 — `com.bovinelabs.grid.edge-policy`

### Purpose
Define movement legality policies between cells, including diagonal corner cutting.

### Allowed Dependencies
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`
- `com.bovinelabs.grid.occupancy`

### Runtime Exports
- `CornerCutPolicy`
- `EdgePolicy2D`
- `EdgePolicy3D`

### Required Behavior
- [ ] Support orthogonal movement legality.
- [ ] Support diagonal movement legality.
- [ ] Support conservative no-corner-cut policy.
- [ ] Support permissive diagonal policy only if explicitly named.
- [ ] Edge validation must check source and destination in bounds.
- [ ] Edge validation must check blocked source and blocked destination.
- [ ] No pathfinding.
- [ ] No heuristics.

### Tests
- [ ] `Orthogonal_FreeCells_ReturnsTrue`
- [ ] `Orthogonal_BlockedDestination_ReturnsFalse`
- [ ] `Diagonal_NoCornerCut_WithSideBlocked_ReturnsFalse`
- [ ] `Diagonal_NoCornerCut_WithBothSidesFree_ReturnsTrue`
- [ ] `Diagonal_Permissive_WithOneSideBlocked_ReturnsTrue`
- [ ] `OutOfBounds_ReturnsFalse`

### DoD
- [ ] Policies are explicit values, not hidden booleans.
- [ ] Default policy is conservative.
- [ ] README includes diagrams for diagonal policy.

---

# Layer 2 — Geometry and Graph Primitives

## P10 — `com.bovinelabs.grid.line-of-sight`

### Purpose
Answer whether two grid cells have unobstructed line of sight under a named policy.

### Allowed Dependencies
- `Unity.Mathematics`
- `Unity.Collections.LowLevel.Unsafe`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`
- `com.bovinelabs.grid.occupancy`
- `com.bovinelabs.grid.edge-policy`

### Runtime Exports
- `LineOfSight2D`
- `LineOfSight3D`
- `RayGridStepper2D`
- `RayGridStepper3D`

### Required Behavior
- [ ] `TryLineOfSight(bounds, blocked, from, to, out visible)` validates all inputs.
- [ ] Same-cell LOS is true only when the cell is in bounds and free.
- [ ] Blocked endpoint returns false.
- [ ] Out-of-bounds endpoint returns false.
- [ ] Diagonal corner crossing follows `CornerCutPolicy`.
- [ ] Deterministic stepping order.
- [ ] No path extraction.
- [ ] No search.

### Tests
- [ ] `LineOfSight_SameFreeCell_ReturnsTrue`
- [ ] `LineOfSight_SameBlockedCell_ReturnsFalse`
- [ ] `LineOfSight_BlockedEndpoint_ReturnsFalse`
- [ ] `LineOfSight_StraightClear_ReturnsTrue`
- [ ] `LineOfSight_StraightBlocked_ReturnsFalse`
- [ ] `LineOfSight_DiagonalCornerCutPolicy_IsConservative`
- [ ] `LineOfSight_OutOfBounds_ReturnsFalse`
- [ ] `LineOfSight_ReversedEndpoints_ReturnSameResult`

### DoD
- [ ] This package becomes the single source of truth for visibility.
- [ ] Algorithms must not duplicate LOS logic after this exists.
- [ ] Tests include the current conservative diagonal behavior.

---

## P11 — `com.bovinelabs.grid.relaxation`

### Purpose
Provide small deterministic relaxation functions for graph and field algorithms.

### Allowed Dependencies
- `Unity.Mathematics`
- `com.bovinelabs.grid.costs`

### Runtime Exports
- `Relaxation`
- `OneStepRelaxation`
- `EikonalRelaxation`

### Required Behavior
- [ ] `TryRelaxMin(current, candidate, out result)` returns lower finite cost.
- [ ] Preserve infinity behavior explicitly.
- [ ] Reject NaN.
- [ ] Provide Eikonal update primitive for fast sweeping / field algorithms.
- [ ] No grid storage.
- [ ] No heap.

### Tests
- [ ] `RelaxMin_CandidateLower_ReturnsCandidate`
- [ ] `RelaxMin_CandidateHigher_ReturnsCurrent`
- [ ] `RelaxMin_NaN_ReturnsFalse`
- [ ] `RelaxMin_InfinityCandidate_DoesNotImproveFinite`
- [ ] `Eikonal_UnitSpeedAdjacent_ReturnsOne`
- [ ] `Eikonal_InvalidSpeed_ReturnsFalse`

### DoD
- [ ] All functions pure.
- [ ] All functions total over validated numeric inputs.
- [ ] README documents all numeric edge cases.

---

## P12 — `com.bovinelabs.grid.distance-field-core`

### Purpose
Represent distance fields and flow vectors without owning a specific algorithm.

### Allowed Dependencies
- `Unity.Collections`
- `Unity.Collections.LowLevel.Unsafe`
- `Unity.Mathematics`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.native-memory`

### Runtime Exports
- `DistanceField2D`
- `DistanceField3D`
- `FlowField2D`
- `FlowField3D`

### Required Behavior
- [ ] Allocate `T`/distance values.
- [ ] Allocate flow vectors when requested.
- [ ] Initialize all distances to positive infinity.
- [ ] Initialize all flow vectors to zero.
- [ ] Validate source indices before setting distance zero.
- [ ] Dispose safely.
- [ ] No sweeping.
- [ ] No D*.
- [ ] No pathfinding.

### Tests
- [ ] `Create_Valid_CreatesFields`
- [ ] `Create_InvalidBounds_ReturnsFalse`
- [ ] `Initialize_AllDistancesInfinity`
- [ ] `SetSources_ValidSources_SetZero`
- [ ] `SetSources_InvalidSource_ReturnsFalse`
- [ ] `Dispose_Double_DoesNotThrow`

### DoD
- [ ] Owns memory explicitly.
- [ ] Does not know how distances are computed.
- [ ] Output state after failed source set is documented.

---

## P13 — `com.bovinelabs.grid.oracle`

### Purpose
Provide slow, obvious, deterministic correctness oracles for tests only.

### Allowed Dependencies
- Lower-layer packages only.
- `NUnit` only in Tests assembly.

### Runtime Exports
- None for production if possible.
- If runtime export is needed, put it under `Tests` or an editor/test-only assembly.

### Required Behavior
- [ ] Brute-force Dijkstra for small grids.
- [ ] Visibility graph oracle for any-angle 2D paths.
- [ ] Exhaustive neighbor oracle for movement policies.
- [ ] Cost comparison helpers.
- [ ] No performance goals.
- [ ] No production dependency.

### Tests
- [ ] `Oracle_OpenGrid_ReturnsEuclideanWhenVisibilityAllowed`
- [ ] `Oracle_BlockedBarrier_ReturnsInfinityWhenNoPath`
- [ ] `Oracle_SmallGrid_IsDeterministic`

### DoD
- [ ] Oracle code is simple, not optimized.
- [ ] README says it is forbidden in runtime gameplay paths.
- [ ] Algorithm packages may reference it only from tests.

---

# Layer 3 — Single-Purpose Algorithms

## P20 — `com.bovinelabs.grid.astar`

### Purpose
Compute shortest grid-constrained paths using A*.

### Allowed Dependencies
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`
- `com.bovinelabs.grid.costs`
- `com.bovinelabs.grid.heap`
- `com.bovinelabs.grid.path-buffer`
- `com.bovinelabs.grid.occupancy`
- `com.bovinelabs.grid.neighborhood`
- `com.bovinelabs.grid.edge-policy`

### Runtime Exports
- `AStarState`
- `AStarApi`

### Required Behavior
- [ ] `TryCreate(width, height, maxNodes, allocator, out state)`.
- [ ] `TrySearch(ref state, blocked, startIndex, goalIndex, ref path)`.
- [ ] Start out of bounds returns false and clears path.
- [ ] Goal out of bounds returns false and clears path.
- [ ] Blocked start returns false and clears path.
- [ ] Blocked goal returns false and clears path.
- [ ] Start equals goal returns single-point path.
- [ ] No path returns false and clears path.
- [ ] Capacity failure returns false and clears path.
- [ ] Output path begins at start and ends at goal.
- [ ] Every segment follows selected edge policy.

### Tests
- [ ] `Search_StartEqualsGoal_ReturnsSinglePoint`
- [ ] `Search_DirectOrthogonal_ReturnsExpectedPath`
- [ ] `Search_WithObstacle_ReturnsValidPath`
- [ ] `Search_NoPossiblePath_ReturnsFalseAndEmptyPath`
- [ ] `Search_ReusedOutputPath_DoesNotContainOldResults`
- [ ] `Search_BlockedStart_ReturnsFalseAndEmptyPath`
- [ ] `Search_BlockedGoal_ReturnsFalseAndEmptyPath`
- [ ] `Search_CapacityTooSmall_ReturnsFalseWithoutStalePath`
- [ ] `RandomSmallGrids_MatchDijkstraOracle`

### DoD
- [ ] Correct before fast.
- [ ] No any-angle behavior here.
- [ ] No multi-agent behavior here.

---

## P21 — `com.bovinelabs.grid.jps`

### Purpose
Compute grid-constrained paths with Jump Point Search, isolated from A* except for shared primitives.

### Allowed Dependencies
- Same lower-layer dependencies as A*.
- `com.bovinelabs.grid.oracle` in tests only.

### Runtime Exports
- `JpsState`
- `JpsApi`

### Required Behavior
- [ ] Same failure model as A*.
- [ ] Same path output contract as A*.
- [ ] Deterministic jump expansion.
- [ ] Conservative corner-cut policy by default.
- [ ] No any-angle smoothing.
- [ ] No CBS integration.

### Tests
- [ ] `Search_StartEqualsGoal_ReturnsSinglePoint`
- [ ] `Search_OpenGrid_ReturnsValidPath`
- [ ] `Search_WithObstacle_ReturnsValidPath`
- [ ] `Search_NoPossiblePath_ReturnsFalseAndEmptyPath`
- [ ] `Search_ReusedOutputPath_DoesNotContainOldResults`
- [ ] `Search_CapacityTooSmall_ReturnsFalseWithoutStalePath`
- [ ] `RandomSmallGrids_MatchAStarCost`
- [ ] `Fuzz_OpenGrid_10x10`
- [ ] `Fuzz_SparseObstacles_10x10`
- [ ] `Fuzz_DenseObstacles_20x10`

### DoD
- [ ] JPS must be removable without breaking A*.
- [ ] JPS tests must not depend on Anya.
- [ ] Fuzz failures print seed, grid size, density, start, goal.

---

## P22 — `com.bovinelabs.grid.anya`

### Purpose
Compute any-angle 2D grid paths using Anya-style interval search.

### Allowed Dependencies
- `Unity.Mathematics`
- `Unity.Collections`
- `Unity.Collections.LowLevel.Unsafe`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`
- `com.bovinelabs.grid.heap`
- `com.bovinelabs.grid.path-buffer`
- `com.bovinelabs.grid.occupancy`
- `com.bovinelabs.grid.line-of-sight`
- `com.bovinelabs.grid.oracle` in tests only.

### Runtime Exports
- `AnyaNode`
- `AnyaNodeKey`
- `AnyaState`
- `AnyaApi`

### Required Behavior
- [ ] `TryCreate(width, height, maxNodes, allocator, out state)`.
- [ ] `TryInitSearch(ref state, blocked, ref start, ref goal)`.
- [ ] `TryStepSearch(ref state, blocked)`.
- [ ] `TryExtractPath(ref state, ref path)`.
- [ ] `TrySearch(ref state, blocked, ref start, ref goal, ref path)`.
- [ ] Batch search and incremental search return same path cost on same map.
- [ ] Start equals goal returns one point.
- [ ] Open direct line returns two points.
- [ ] Every returned path segment has line of sight.
- [ ] Failed search clears output path.
- [ ] Capacity failure returns false and clears output path.
- [ ] Node keys are quantized deterministically.
- [ ] Root cost table is reset before each search.
- [ ] Heap, pool, lookup, and root cost memory are owned by state.

### Tests
- [ ] `Create_ValidDimensions_CreatesState`
- [ ] `Dispose_Double_DoesNotThrow`
- [ ] `Search_StartEqualsGoal_ReturnsSinglePoint`
- [ ] `Search_DirectLine_ReturnsTwoPointEuclideanPath`
- [ ] `Search_WithObstacle_ReturnsVisibleUnblockedPath`
- [ ] `Search_NoPossiblePath_ReturnsFalseAndEmptyPath`
- [ ] `Search_ReusedOutputPath_DoesNotContainOldResults`
- [ ] `Search_UncreatedBlockedArray_ReturnsFalseAndClearsPath`
- [ ] `Search_ShortBlockedArray_ReturnsFalseAndClearsPath`
- [ ] `Search_UncreatedOutputPath_ReturnsFalse`
- [ ] `Search_OutOfBoundsStart_ReturnsFalseAndClearsPath`
- [ ] `Search_OutOfBoundsGoal_ReturnsFalseAndClearsPath`
- [ ] `Search_BlockedStart_ReturnsFalseAndEmptyPath`
- [ ] `Search_BlockedGoal_ReturnsFalseAndEmptyPath`
- [ ] `Search_CapacityTooSmall_ReturnsFalseWithoutStalePath`
- [ ] `LineOfSight_BlockedEndpoint_ReturnsFalse`
- [ ] `LineOfSight_DiagonalCornerCutPolicy_IsConservative`
- [ ] `IncrementalSearch_MatchesBatchSearch_OnObstacleMap`
- [ ] `RandomSmallGrids_MatchVisibilityGraphOracle`

### DoD
- [ ] LOS logic comes from `grid.line-of-sight` or is marked temporary until extracted.
- [ ] Anya does not depend on JPS or A*.
- [ ] Anya does not solve multi-agent conflicts.
- [ ] All memory is disposed and double-dispose safe.

---

## P23 — `com.bovinelabs.grid.dstarlite`

### Purpose
Incrementally replan grid-constrained paths when the start moves or occupancy changes.

### Allowed Dependencies
- `Unity.Mathematics`
- `Unity.Collections`
- `Unity.Collections.LowLevel.Unsafe`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`
- `com.bovinelabs.grid.costs`
- `com.bovinelabs.grid.heap`
- `com.bovinelabs.grid.occupancy`
- `com.bovinelabs.grid.neighborhood`
- `com.bovinelabs.grid.edge-policy`
- `com.bovinelabs.grid.path-buffer`
- `com.bovinelabs.grid.oracle` in tests only.

### Runtime Exports
- `DStarLiteState`
- `DStarLiteApi`

### Required Behavior
- [ ] `TryCreate(width, height, allocator, out state)`.
- [ ] `TryInitialize(ref state, start, goal, blocked)`.
- [ ] `NotifyMoved(ref state, newStart)`.
- [ ] `TryUpdateCell(ref state, cell, blocked)` if dynamic obstacle update exists.
- [ ] `TryComputeShortestPath(ref state, blocked, maxSteps)`.
- [ ] `TryExtractPath(ref state, blocked, ref path)`.
- [ ] Invalid start returns false.
- [ ] Invalid goal returns false.
- [ ] Blocked goal returns false.
- [ ] G and RHS arrays initialize to infinity except goal RHS = 0.
- [ ] Open heap deterministic.
- [ ] Parent array resets to -1.
- [ ] Moving start updates `Km` deterministically.

### Tests
- [ ] `Create_ValidDimensions_CreatesState`
- [ ] `Initialize_ValidStartGoal_ReturnsTrue`
- [ ] `Initialize_InvalidStart_ReturnsFalse`
- [ ] `Initialize_InvalidGoal_ReturnsFalse`
- [ ] `Initialize_BlockedGoal_ReturnsFalse`
- [ ] `Initialize_SetsGoalRhsToZero`
- [ ] `Compute_OpenGrid_ReturnsPath`
- [ ] `Compute_WithObstacle_ReturnsPathAroundObstacle`
- [ ] `NotifyMoved_UpdatesStartAndKm`
- [ ] `DynamicObstacle_Replan_ReturnsNewValidPath`
- [ ] `ExtractPath_NoPath_ReturnsFalseAndClearsPath`
- [ ] `Dispose_Double_DoesNotThrow`

### DoD
- [ ] D* Lite owns incremental replanning only.
- [ ] It does not own LOS.
- [ ] It does not own flow fields.

---

## P24 — `com.bovinelabs.grid.field-dstar`

### Purpose
Compute continuous-ish flow/cost fields using Field D* style updates.

### Allowed Dependencies
- `Unity.Mathematics`
- `Unity.Collections`
- `Unity.Collections.LowLevel.Unsafe`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`
- `com.bovinelabs.grid.heap`
- `com.bovinelabs.grid.occupancy`
- `com.bovinelabs.grid.relaxation`
- `com.bovinelabs.grid.distance-field-core`

### Runtime Exports
- `FieldDStarState`
- `FieldDStarApi`

### Required Behavior
- [ ] `TryCreate(width, height, allocator, out state)`.
- [ ] Allocate `G`, `RHS`, `Flow`, and heap.
- [ ] Initialize distances deterministically.
- [ ] Validate goal.
- [ ] Reject blocked goal.
- [ ] Compute flow vectors deterministically.
- [ ] Dispose safely.
- [ ] No path list extraction unless explicitly requested as a separate package.

### Tests
- [ ] `Create_ValidDimensions_CreatesState`
- [ ] `Initialize_ValidGoal_ReturnsTrue`
- [ ] `Initialize_InvalidGoal_ReturnsFalse`
- [ ] `Initialize_BlockedGoal_ReturnsFalse`
- [ ] `Compute_OpenGrid_CostIncreasesAwayFromGoal`
- [ ] `Compute_WithObstacle_FlowAvoidsBlockedCell`
- [ ] `Flow_AllVectorsFiniteOrZero`
- [ ] `Dispose_Double_DoesNotThrow`

### DoD
- [ ] Field D* does not depend on D* Lite code directly unless a primitive is extracted first.
- [ ] Shared math lives in `grid.relaxation` or `grid.distance-field-core`.
- [ ] Flow output contract documented.

---

## P25 — `com.bovinelabs.grid.fast-sweeping`

### Purpose
Compute distance fields by sweeping directions over a grid.

### Allowed Dependencies
- `Unity.Mathematics`
- `Unity.Collections`
- `Unity.Collections.LowLevel.Unsafe`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`
- `com.bovinelabs.grid.relaxation`
- `com.bovinelabs.grid.distance-field-core`

### Runtime Exports
- `FastSweepingState`
- `FastSweepingApi`

### Required Behavior
- [ ] `TryCreate(width, height, allocator, out state)`.
- [ ] `TryInitialize(ref state, sources)` validates source indices.
- [ ] `TryRelaxCell(state, speed, index)` validates speed array and index.
- [ ] `TrySweepAllDirections(ref state, speed, iterations)` validates iterations >= 0.
- [ ] Sources remain zero.
- [ ] Unreached cells remain infinity.
- [ ] Speed <= 0 is invalid.
- [ ] Dispose safely.

### Tests
- [ ] `Create_ValidDimensions_CreatesState`
- [ ] `Initialize_ValidSource_SetsZero`
- [ ] `Initialize_InvalidSource_ReturnsFalse`
- [ ] `Sweep_2D_Center`
- [ ] `RelaxCell_Updates`
- [ ] `Sweep_InvalidSpeed_ReturnsFalse`
- [ ] `Sweep_NegativeIterations_ReturnsFalse`
- [ ] `Dispose_Double_DoesNotThrow`

### DoD
- [ ] Fast sweeping does not own pathfinding.
- [ ] Fast sweeping does not know about agents.
- [ ] Relaxation math is not duplicated.

---

## P26 — `com.bovinelabs.grid.belief-propagation`

### Purpose
Run grid belief propagation over label costs and decode MAP labels.

### Allowed Dependencies
- `Unity.Collections`
- `Unity.Collections.LowLevel.Unsafe`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`
- `com.bovinelabs.grid.native-memory`

### Runtime Exports
- `BeliefState`
- `BeliefApi`

### Required Behavior
- [ ] `TryCreate(width, height, labelCount, allocator, out state)`.
- [ ] Reject invalid grid dimensions.
- [ ] Reject labelCount <= 0.
- [ ] Allocate unary, messages, next messages, belief, scratch.
- [ ] `SetUnary` validates created array and required length.
- [ ] `ClearMessages` zeros both current and next messages.
- [ ] `TryIterate(ref state, pairwise, iterations)` validates pairwise length is `labelCount * labelCount`.
- [ ] `TryIterate` rejects negative iterations.
- [ ] `TryDecodeMap(ref state, ref labels)` validates output length.
- [ ] Decode writes one label per cell.
- [ ] Dispose safely.

### Tests
- [ ] `Create_Dimensions`
- [ ] `Create_InvalidLabelCount_ReturnsFalse`
- [ ] `SetUnary_UncreatedArray_ReturnsFalse`
- [ ] `SetUnary_ShortArray_ReturnsFalse`
- [ ] `ClearMessages_ZerosAllMessages`
- [ ] `Iterate_ThenDecode`
- [ ] `ConsensusChain`
- [ ] `MessageClear_NoGhostBeliefs`
- [ ] `Decode_UncreatedLabels_ReturnsFalse`
- [ ] `Decode_ShortLabels_ReturnsFalse`
- [ ] `Dispose_Double_DoesNotThrow`

### DoD
- [ ] Belief propagation does not know about paths.
- [ ] It does not depend on occupancy unless a future package explicitly adds observed evidence.
- [ ] Pairwise layout documented exactly.

---

## P27 — `com.bovinelabs.grid.cbs`

### Purpose
Solve multi-agent path conflicts using Conflict-Based Search over single-agent paths.

### Allowed Dependencies
- `Unity.Mathematics`
- `Unity.Collections`
- `Unity.Collections.LowLevel.Unsafe`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`
- `com.bovinelabs.grid.heap`
- `com.bovinelabs.grid.path-buffer`
- `com.bovinelabs.grid.occupancy`
- `com.bovinelabs.grid.astar`

### Runtime Exports
- `AgentTask`
- `CbsConstraint`
- `CbsConflict`
- `CbsNode`
- `CbsState`
- `CbsApi`

### Required Behavior
- [ ] `TryCreate(width, height, maxNodes, allocator, out state)`.
- [ ] `TryInitSolve(ref state, blocked, agents)`.
- [ ] `TryStepSolve(ref state, blocked, agents)`.
- [ ] `TryExtractSolution(ref state, ref resultFlatPaths, ref resultPathLengths)`.
- [ ] `TrySolve(ref state, blocked, agents, ref resultFlatPaths, ref resultPathLengths)`.
- [ ] Zero agents returns success with empty output.
- [ ] Invalid agent start returns false.
- [ ] Invalid agent goal returns false.
- [ ] Blocked agent start returns false.
- [ ] Blocked agent goal returns false.
- [ ] Vertex conflicts are detected.
- [ ] Edge-swap conflicts are detected.
- [ ] Constraints apply only to their agent.
- [ ] Failed solve clears output paths.
- [ ] Capacity failure returns false.

### Tests
- [ ] `Create_ValidDimensions_CreatesState`
- [ ] `Solve_ZeroAgents_ReturnsEmptySolution`
- [ ] `Solve_OneAgent_ReturnsSinglePath`
- [ ] `Solve_TwoAgentsNoConflict_ReturnsBothPaths`
- [ ] `Solve_VertexConflict_ResolvesConflict`
- [ ] `Solve_EdgeSwapConflict_ResolvesConflict`
- [ ] `Solve_NoPossibleSolution_ReturnsFalseAndEmptyOutput`
- [ ] `Solve_ReusedOutput_DoesNotContainOldPaths`
- [ ] `Solve_CapacityTooSmall_ReturnsFalse`
- [ ] `FindConflict_VertexConflict_ReturnsExpectedAgentsAndTime`
- [ ] `FindConflict_EdgeConflict_ReturnsExpectedAgentsAndTime`
- [ ] `Dispose_Double_DoesNotThrow`

### DoD
- [ ] CBS owns multi-agent conflict resolution only.
- [ ] Single-agent search lives in a dependency package.
- [ ] Constraints are typed data, not ad hoc tuples.

---

## P28 — `com.bovinelabs.grid.wavestar`

### Purpose
Compute 3D any-angle or multi-resolution paths over voxel/subvolume data.

### Allowed Dependencies
- `Unity.Mathematics`
- `Unity.Collections`
- `Unity.Collections.LowLevel.Unsafe`
- `com.bovinelabs.grid.coordinates`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.indexing`
- `com.bovinelabs.grid.costs`
- `com.bovinelabs.grid.heap`
- `com.bovinelabs.grid.path-buffer`
- `com.bovinelabs.grid.occupancy`
- `com.bovinelabs.grid.neighborhood`
- `com.bovinelabs.grid.line-of-sight`

### Runtime Exports
- `WavestarVolume`
- `WavestarSubvolume`
- `WavestarBuilder`
- `WavestarState`
- `WavestarApi`

### Required Behavior
- [ ] `TryBuild(grid, width, height, depth, maxLevel, out traversable, out distanceField)`.
- [ ] Reject invalid dimensions.
- [ ] Reject uncreated grid.
- [ ] Distance field marks obstacle cells as distance 0.
- [ ] Traversable subvolumes are deterministic.
- [ ] `TryCreate` owns search state.
- [ ] `TrySearch` outputs valid 3D path.
- [ ] Any-angle path never enters blocked voxel.
- [ ] Epsilon parameter is validated and documented.
- [ ] Failed search clears output.

### Tests
- [ ] `Build_InvalidGrid_ReturnsFalse`
- [ ] `Build_ObstacleDistanceField_SetsBlockedCellsZero`
- [ ] `Build_MultiResolution_RefinesNearObstacles`
- [ ] `Search_OpenGrid_ReturnsPath`
- [ ] `Search_WithObstacle_ReturnsPathAroundObstacle`
- [ ] `Search_PathWaypoints_AreNotBlocked`
- [ ] `Search_AnyAngle_ShorterThanGridConstrained`
- [ ] `Search_EpsilonZero_OptimalPathWithinTolerance`
- [ ] `Search_ReusedOutput_DoesNotContainOldPath`
- [ ] `Dispose_Double_DoesNotThrow`

### DoD
- [ ] Wavestar does not own 2D algorithms.
- [ ] Builder and search state are separate.
- [ ] Multi-resolution data has typed structs and documented invariants.

---

# Layer 4 — Validation, Fuzzing, and Benchmarks

## P40 — `com.bovinelabs.grid.test-fixtures`

### Purpose
Provide reusable test-only builders and assertions.

### Allowed Dependencies
- All lower runtime primitives as needed.
- `NUnit`.

### Runtime Exports
- None for production.

### Test Helper Exports
- `GridTestMaps`
- `PathAssertions`
- `OccupancyAssertions`
- `CostAssertions`

### Required Behavior
- [ ] Create empty blocked grids.
- [ ] Create wall with gap.
- [ ] Create corridor map.
- [ ] Create maze-like map.
- [ ] Assert path starts at start.
- [ ] Assert path ends at goal.
- [ ] Assert all waypoints in bounds.
- [ ] Assert all waypoints free.
- [ ] Assert all path segments legal.
- [ ] Assert no stale output after failure.

### Tests
- [ ] Test the test helpers with intentionally valid and invalid paths.

### DoD
- [ ] Test helpers must not hide algorithm failures.
- [ ] Assertion failure messages include index, coordinate, seed when applicable.

---

## P41 — `com.bovinelabs.grid.fuzzing`

### Purpose
Provide deterministic fuzz input generation for grid algorithms.

### Allowed Dependencies
- `Unity.Mathematics`
- `Unity.Collections`
- `com.bovinelabs.grid.bounds`
- `com.bovinelabs.grid.occupancy`
- `com.bovinelabs.grid.test-fixtures`

### Runtime Exports
- None for production unless explicitly needed.

### Test Helper Exports
- `GridFuzzCase`
- `GridFuzzGenerator`
- `SeededGridRandom`

### Required Behavior
- [ ] Generate grid dimensions from fixed seed.
- [ ] Generate obstacle density from fixed seed.
- [ ] Pick valid start and goal from free cells.
- [ ] Return no-case marker when insufficient free cells exist.
- [ ] Print seed, dimensions, density, start, goal on failure.
- [ ] Never use nondeterministic random source.

### Tests
- [ ] `Generate_SameSeed_ReturnsSameGrid`
- [ ] `Generate_DifferentSeed_ReturnsDifferentGridUsually`
- [ ] `PickStartGoal_NoFreeCells_ReturnsFalse`
- [ ] `PickStartGoal_FreeCells_ReturnsValidDistinctCellsWhenPossible`

### DoD
- [ ] Fuzzing is reusable by JPS, Anya, A*, CBS, and Wavestar tests.
- [ ] Fuzzing package has no production dependency.

---

## P42 — `com.bovinelabs.grid.benchmarks`

### Purpose
Measure performance without affecting correctness packages.

### Allowed Dependencies
- Runtime packages being benchmarked.
- Unity performance testing packages if available.

### Required Behavior
- [ ] Benchmarks are separate from correctness tests.
- [ ] Benchmarks use fixed maps and fixed seeds.
- [ ] Benchmarks report allocations.
- [ ] Benchmarks report average runtime.
- [ ] Benchmarks never determine pass/fail correctness except for sanity validation.

### Benchmark Cases
- [ ] A* open grid.
- [ ] JPS open grid.
- [ ] Anya open grid.
- [ ] Anya sparse obstacles.
- [ ] D* Lite replan after obstacle change.
- [ ] Fast sweeping fixed iteration count.
- [ ] Belief propagation fixed label count.
- [ ] CBS two-agent conflict.
- [ ] Wavestar 3D obstacle map.

### DoD
- [ ] Benchmark package is optional.
- [ ] No runtime package depends on benchmark package.

---

# 3. Extraction Checklist for Existing Code

Use this checklist when splitting existing code into atomic packages.

## 3.1 Before Moving Code

- [ ] Identify one concept in the source file.
- [ ] Name the concept as a noun.
- [ ] List every input the concept needs.
- [ ] List every output the concept produces.
- [ ] List every state value it mutates.
- [ ] List every allocation it performs.
- [ ] List every failure mode.
- [ ] List every invariant.
- [ ] If the concept has more than one reason to change, split it again.

## 3.2 While Moving Code

- [ ] Move data structs before algorithms.
- [ ] Move pure math before stateful search.
- [ ] Move validation before unchecked hot path.
- [ ] Move reusable test assertions before algorithm fuzz tests.
- [ ] Preserve behavior with regression tests before refactoring internals.
- [ ] Keep old package tests passing until replacement package tests exist.

## 3.3 After Moving Code

- [ ] Delete duplicate code from the source package.
- [ ] Replace duplicate code with dependency on the new atomic package.
- [ ] Run tests for the new package.
- [ ] Run tests for the old package.
- [ ] Confirm public API did not accidentally grow.
- [ ] Confirm no package references a higher layer.

---

# 4. Per-Function Atomicity Rules

Each function must satisfy these rules.

- [ ] Name states what it does.
- [ ] Inputs are explicit.
- [ ] Outputs are explicit.
- [ ] Failure is explicit.
- [ ] No hidden read from static mutable state.
- [ ] No hidden write to static mutable state.
- [ ] No allocation unless function name or README says it allocates.
- [ ] No partial behavior on invalid input.
- [ ] Deterministic for same input.
- [ ] Unit test exists for normal case.
- [ ] Unit test exists for invalid input.
- [ ] Unit test exists for boundary input.

If a function violates any rule, split it or rename it.

---

# 5. Public API Rules

## 5.1 Creation APIs

Every owned-state package must use this shape:

```cs
public static bool TryCreate(..., Allocator allocator, out State result)
```

Rules:

- [ ] Return false and set `result = default` on invalid input.
- [ ] Return false and clean up partial allocations if any allocation fails.
- [ ] Never return true with partially initialized state.
- [ ] Tests must inspect every owned buffer after successful create.

## 5.2 Search APIs

Every search package must use this shape:

```cs
public static bool TrySearch(ref State state, in NativeArray<byte> blocked, ..., ref NativeList<T> path)
```

Rules:

- [ ] Validate state.
- [ ] Validate blocked array.
- [ ] Validate output path.
- [ ] Clear output path before work starts.
- [ ] Clear output path before returning false.
- [ ] Return true only when output satisfies path contract.
- [ ] Return false for invalid input, no path, or capacity failure.
- [ ] If different false reasons matter, add an optional status enum later; do not guess now.

## 5.3 Incremental APIs

Incremental algorithms must split lifecycle:

```cs
TryInit*(...)
TryStep*(...)
TryExtract*(...)
TrySolve/TrySearch(...)
```

Rules:

- [ ] Batch API must be equivalent to init + repeated step + extract.
- [ ] Step must be safe when already complete.
- [ ] Extract must fail cleanly when no solution exists.
- [ ] Tests compare batch and incremental results.

## 5.4 Dispose APIs

Rules:

- [ ] Dispose may be called twice.
- [ ] Dispose sets pointers to null.
- [ ] Dispose clears owned containers.
- [ ] Dispose sets state to default when practical.
- [ ] Tests call dispose twice.

---

# 6. Package Boundary Smell List

If any item below happens, stop and split again.

- [ ] A coordinate type knows about blocked cells.
- [ ] A bounds type knows about paths.
- [ ] A heap knows about grids.
- [ ] A line-of-sight type owns search state.
- [ ] A path buffer computes heuristics.
- [ ] An A* package knows about CBS constraints.
- [ ] A CBS package implements its own heap.
- [ ] Anya duplicates line-of-sight after LOS package exists.
- [ ] D* Lite and Field D* copy-paste relaxation math.
- [ ] Tests require running unrelated packages to validate a primitive.
- [ ] A README explains implementation mood instead of domain rules.

---

# 7. Minimal Completion Order

Use this exact order for first pass.

- [ ] P00 `grid.coordinates`
- [ ] P01 `grid.bounds`
- [ ] P02 `grid.indexing`
- [ ] P03 `grid.costs`
- [ ] P04 `grid.native-memory`
- [ ] P05 `grid.heap`
- [ ] P06 `grid.path-buffer`
- [ ] P07 `grid.occupancy`
- [ ] P08 `grid.neighborhood`
- [ ] P09 `grid.edge-policy`
- [ ] P10 `grid.line-of-sight`
- [ ] P11 `grid.relaxation`
- [ ] P12 `grid.distance-field-core`
- [ ] P13 `grid.oracle`
- [ ] P20 `grid.astar`
- [ ] P21 `grid.jps`
- [ ] P22 `grid.anya`
- [ ] P23 `grid.dstarlite`
- [ ] P24 `grid.field-dstar`
- [ ] P25 `grid.fast-sweeping`
- [ ] P26 `grid.belief-propagation`
- [ ] P27 `grid.cbs`
- [ ] P28 `grid.wavestar`
- [ ] P40 `grid.test-fixtures`
- [ ] P41 `grid.fuzzing`
- [ ] P42 `grid.benchmarks`

---

# 8. Final Verification Gate

Do not consider the decomposition complete until all checks pass.

- [ ] Each package can be described in one sentence.
- [ ] Each package has one reason to change.
- [ ] Each package has its own tests.
- [ ] Each package can fail independently.
- [ ] Each package can be deleted without breaking lower-layer packages.
- [ ] Each package declares all dependencies.
- [ ] No package depends upward.
- [ ] No runtime package depends on test fixtures, fuzzing, oracle, or benchmarks.
- [ ] Public API names explain domain behavior.
- [ ] Invalid input behavior is documented.
- [ ] Memory ownership is documented.
- [ ] All deterministic tests pass twice in a row.
- [ ] Fuzz tests print enough information to reproduce failure.
- [ ] There is no hidden wiring into gameplay, scenes, ECS systems, or MonoBehaviours.

---

# 9. Stop Conditions

Stop the current package and fix design before continuing if any of these occur.

- [ ] A package needs a circular dependency.
- [ ] A test requires reflection to inspect correctness.
- [ ] A public method needs more than one paragraph to explain.
- [ ] A function name contains `And`.
- [ ] A state struct owns memory that is not disposed.
- [ ] A failed operation leaves stale output.
- [ ] A random test cannot be reproduced from its failure message.
- [ ] An unchecked API is public without `Unchecked` in the name.
- [ ] An algorithm package contains general-purpose primitives that another package needs.

---

# 10. Final Principle

A package is correct when its smallest useful behavior is named, typed, tested, deterministic, and disposable. Composition comes later. Wiring comes later. Performance comes after correctness, except where fixed allocation and deterministic hot paths are already part of the package contract.
