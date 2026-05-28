# com.bovinelabs.grid.coordinates

## Purpose
Integer grid coordinate types with distance metrics. No bounds, no storage, no algorithms.

## Public API
- `GridCoord2` — readonly 2D integer coordinate
- `GridCoord3` — readonly 3D integer coordinate
- `GridCoord2Extensions` — EuclideanDelta for 2D
- `GridCoord3Extensions` — EuclideanDelta for 3D

### GridCoord2 Methods
- `ToInt2()` → `int2`
- `FromInt2(int2)` → `GridCoord2`
- `ManhattanDelta(other)` → `int`
- `ChebyshevDelta(other)` → `int`
- `OctileDelta(other)` → `float`
- `SquaredEuclideanDelta(other)` → `float`
- `EuclideanDelta(other)` → `float` (extension)

### GridCoord3 Methods
- `ToInt3()` → `int3`
- `FromInt3(int3)` → `GridCoord3`
- `ManhattanDelta(other)` → `int`
- `ChebyshevDelta(other)` → `int`
- `SquaredEuclideanDelta(other)` → `float`
- `EuclideanDelta(other)` → `float` (extension)

## Invariants
- All functions are pure.
- All functions are total for all integer inputs.
- No allocation.
- No bounds checking (not this package's responsibility).
- No index conversion (not this package's responsibility).
- Distance functions are symmetric.

## Failure Model
No `Try*` methods. All functions are total. No failure modes.

## Determinism
All outputs fully determined by inputs. No floating-point nondeterminism for integer-distance metrics. OctileDelta and EuclideanDelta use `Unity.Mathematics` which is deterministic per platform.

## Memory Ownership
No owned memory. No disposable state.

## Non-Goals
- Bounds checking.
- Index conversion.
- Blocked cell awareness.
- Pathfinding.
- Grid storage.

## Minimal Example
```csharp
var a = new GridCoord2(0, 0);
var b = new GridCoord2(3, 4);
float dist = a.EuclideanDelta(b); // 5.0
```

## Test Categories
- `Correctness` — equal/different, round-trips, each distance metric
- `Determinism` — repeated calls return same value
- `Symmetry` — all distances symmetric
- `Identity` — same-point distances return zero
