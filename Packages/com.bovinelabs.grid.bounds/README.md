# com.bovinelabs.grid.bounds

## Purpose
Rectangular 2D and 3D grid bounds with checked in-bounds queries.

## Public API
- `GridBounds2D` — readonly 2D bounds (width × height)
- `GridBounds3D` — readonly 3D bounds (width × height × depth)

### GridBounds2D Methods
- `TryCreate(width, height, out bounds)` → `bool`
- `InBounds(x, y)` → `bool`
- `InBounds(index)` → `bool`
- `Length` → `int` (width × height)

### GridBounds3D Methods
- `TryCreate(width, height, depth, out bounds)` → `bool`
- `InBounds(x, y, z)` → `bool`
- `InBounds(index)` → `bool`
- `Length` → `int` (width × height × depth)

## Invariants
- `TryCreate` returns false for any dimension ≤ 0.
- `TryCreate` returns false if total cell count overflows `int.MaxValue`.
- `InBounds` uses unsigned comparison: `(uint)x < (uint)width`.
- No allocation.
- No index conversion (that belongs to `grid.indexing`).

## Failure Model
- `TryCreate` returns false with `default` output for invalid dimensions or overflow.

## Determinism
All outputs fully determined by inputs. No floating-point operations.

## Memory Ownership
No owned memory. No disposable state.

## Non-Goals
- Index conversion.
- Blocked cell awareness.
- Distance computation.
- Grid storage.

## Minimal Example
```csharp
if (GridBounds2D.TryCreate(10, 10, out var bounds))
{
    bool inside = bounds.InBounds(5, 3); // true
}
```

## Test Categories
- `Creation` — valid/invalid dimensions, overflow
- `InputValidation` — zero, negative dimensions
- `Correctness` — in-bounds checks at corners, edges
- `Memory` — N/A (no owned memory)
