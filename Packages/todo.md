# Pathfinding & Grid Algorithms - TODO

## In Progress
(none currently)

## Done
- [x] **CBS** — Edge swap conflicts, goal-wait conflicts, multi-agent bottleneck tests all passing
- [x] **Domino** — Bipartite matching via manual flow network (bypass BuildBinaryEnergy), 4-directional edges, negative diff handling
- [x] **GraphCut** — Undirected pairwise edges, bottleneck test, grid partition test
- [x] **Belief** — Message buffer clearing per iteration, consensus chain test, ghost belief test
- [x] **Test asmdef** — All 5 test assemblies use correct template (Editor platform, overrideReferences, nunit.framework.dll)
- [x] **Anya** — LineOfSight shortcut, bidirectional expansion, Euclidean cost test, corner test
- [x] **Anya Search_WithWall** — Fixed: `(int)math.ceil(double.PositiveInfinity)` overflow guard, zero-length interval filter, duplicate node detection
- [x] **Anya precision** — `DoubleMinHeap` + `DoubleHeapNode` with `double` keys; AnyaApi now uses full double-precision f-values eliminating float-truncation suboptimality
- [x] **Anya NoBlockedTraversal** — Replaced Bresenham LoS with Amanatides & Woo fast voxel traversal (epsilon-robust)
- [x] **CBS edge constraints** — `CbsConstraint` upgraded to `(Agent, Cell, CellFrom, Time)` format. Vertex constraints: `CellFrom == -1`. Edge constraints: `CellFrom >= 0`. `FindConflict` returns `conflictType` (0=vertex, 1=swap). `TryAStar` validates both. Test: `AStar_EdgeConstraint`
- [x] **Fuzz** — `PathfinderFuzzTests` in `shattered-unit-tests` package. 7 test configurations × 10-20 trials each. Random xorshift grids at varying densities. Validates path start/goal correctness, cross-validates JPS/Anya reachability, monitors Anya optimality gaps. 145 total tests all pass.
- [x] **Anya completeness** — Interval splitting at blocked cells: clear-run walker creates sub-intervals `[max(pL,x), min(pR,runEnd+1)]` around blocked cells instead of `continue`-ing
- [x] **Anya optimality** — `ExpandCorners` scans all integer x positions in `[L,R]` for left/right wall corners; `TryAddCornerNode` creates bi-directional expansion from detected corners
- [x] **MeshA hash map elimination** — Replaced 3× `NativeHashMap` with flat `float*`/`int*` arrays + `uint*` bit-packed closed set (1 bit per state). Uses `MinHeap` with `TryInsertOrDecrease` for decrease-key. All via `UnsafeUtility.Malloc(Allocator.Temp)`.
- [x] **EHL SIMD Jaccard** — `ComputeOverlap` uses `ulong` bitmask + `math.countbits()` for Jaccard coefficient. Fast path for ≤64 hubs, multi-word fallback for larger.
- [x] **Belief propagation optimization** — `TryIterate` rewritten with precomputed bounds, running `cellIdx++` increment, inlined direction mapping, unrolled 4-direction message sum, `UnsafeUtility.MemSet` for buffer clear, pointer-only access (zero NativeArray indexing). `TryDecodeMap` uses unrolled 4-direction message sum.
- [x] **EDT parallel jobs** — `EdtRowJob`/`EdtColJob` as `IJobFor` structs for parallelizable separable passes. `TryBuildParallel` runs row pass then column pass. Rows operate in-place on dist2 buffer; columns use thread-local contiguous temp buffers.

## Future
- [ ] **Anya steppable** — Split monolithic `TrySearch` into `TryInitSearch` + `TryStepSearch` for visualization
- [ ] **WFC steppable** — Split into `TryObserveStep` + `TryPropagateStep` for frame-by-frame execution
- [ ] **CBS steppable** — Separate high-level constraint tree stepper from low-level A* stepper
- [ ] **EDT `ScheduleParallel`** — Wire `EdtRowJob`/`EdtColJob` into `JobHandle` pipeline with proper `ScheduleParallel(batchCount)` instead of `Run()`
- [ ] **Continuum crowd** — Flatten 2D loops into 1D pointer increments, `[NoAlias]` annotation on dense field pointers
