# Pathfinding & Grid Algorithms - TODO

## In Progress
- [ ] **Anya interval search** — `Search_WithWall` fails: start `(0,0)→(9,0)` with blocked cell at `(5,0)`. The bidirectional expansion (dy=+1/-1) was added but the algorithm still can't route around obstacles that require going down then back up to the same row. Root cause: when expanding from row N back to row 0, the interval projection and cell scanning logic doesn't properly rediscover cells past the obstacle. Needs deeper fix to the projection/interval-splitting logic or a fallback to grid-based pathfinding for these cases.

## Done
- [x] **CBS** — Edge swap conflicts, goal-wait conflicts, multi-agent bottleneck tests all passing
- [x] **Domino** — Bipartite matching via manual flow network (bypass BuildBinaryEnergy), 4-directional edges, negative diff handling
- [x] **GraphCut** — Undirected pairwise edges, bottleneck test, grid partition test
- [x] **Belief** — Message buffer clearing per iteration, consensus chain test, ghost belief test
- [x] **Test asmdef** — All 5 test assemblies use correct template (Editor platform, overrideReferences, nunit.framework.dll)
- [x] **Anya** — LineOfSight shortcut, bidirectional expansion, Euclidean cost test, corner test

## Future
- [ ] **Fuzz** — Pathfinder equivalence (A* vs JPS vs Anya) on random grids
- [ ] **Anya NoBlockedTraversal** — Line-segment intersection check against blocked cells
- [ ] **CBS edge constraints** — Upgrade `CbsConstraint` to support `(agent, cellFrom, cellTo, time)` for proper edge conflict resolution
- [ ] **Anya precision** — Replace `float` f-values in MinHeap with `double` for better Anya optimality
