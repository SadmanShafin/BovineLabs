# TODO — ReSharper Warning Fixes

Each item found by `jb inspectcode`. Fix one, verify with `jb inspectcode <csproj> -o=/tmp/out.xml`.
Skip `UnusedMember.Global`, `UnusedType.Global` — shared lib types used by other packages.
Skip `MemberCanBePrivate.Global` unless confirmed not used by tests.
Skip `InconsistentNaming`, `MergeIntoPattern`, `UseIndexFromEndExpression`, `SwapViaDeconstruction`, `ForLoopCanBeConvertedToForeach` — style only, not bugs.

Run first to get fresh data:
```bash
cd /home/i/Documents/BovineLabs
for csproj in com.bovinelabs.grid.*.csproj; do
  name=$(basename "$csproj" .csproj)
  [[ "$name" == *.Player || "$name" == *.tests* ]] && continue
  jb inspectcode "$csproj" -o="/tmp/inspect-${name}.xml" --format=Xml 2>/dev/null
done
```

---

## CRITICAL — Float equality bugs (precision loss)

### TODO-30 — AnyaApi: 3 float equality comparisons

**File:** `com.bovinelabs.grid.anya/Runtime/AnyaApi.cs`

Lines 186, 412, 426 use `==` or `!=` on doubles. Replace with epsilon comparison.

- Line 186: `if (start.Equals(goal))` — this is `int2.Equals`, OK. Check actual float/double comparisons at the reported lines.
- Lines 412, 426: Likely `tMaxX` / `tMaxY` comparisons in `LineOfSight`. Check and replace `==` with `math.abs(a - b) < EPS` where applicable.

- [x] Done

### TODO-31 — DStarLiteApi: 4 float equality comparisons

**File:** `com.bovinelabs.grid.dstarlite/Runtime/DStarLiteApi.cs`

Lines 211, 292, 340, 346.

- Line 211: Already fixed `rhsPtr == gPtr` to epsilon. Verify line numbers match after prior edits.
- Lines 340, 346: `LessOrEqual` and `Less` key comparison helpers use `k0a != k0b` on floats. These are heap key comparisons — intentional lexicographic ordering, not precision-sensitive. **Likely safe to suppress** but verify.

- [x] Done

### TODO-32 — FieldDStarApi: 2 float equality comparisons

**File:** `com.bovinelabs.grid.fielddstar/Runtime/FieldDStarApi.cs`

Lines 82, 149.

- Line 82: Likely `gPtr[uid] > rhsPtr[uid]` — relational, OK. Check if `==` is used.
- Line 149: Likely `costPtr != null` or similar. Verify.

- [x] Done

---

## CORRECTNESS — Dead fields and assignments

### TODO-33 — UnassignedField.Global (3 instances)

Run: `grep 'TypeId="UnassignedField.Global"' /tmp/inspect-com.bovinelabs.grid.*.xml`

Check each — struct fields that are declared but never assigned. May indicate missing initialization in `TryCreate`.

- [x] Done

### TODO-34 — NotAccessedField.Global (3 instances)

Run: `grep 'TypeId="NotAccessedField.Global"' /tmp/inspect-com.bovinelabs.grid.*.xml`

Fields written but never read. Dead code — remove or confirm intentional.

- [x] Done

### TODO-35 — RedundantAssignment in WfcApi

**File:** `com.bovinelabs.grid.wfc/Runtime/WfcApi.cs`, line 60

Value assigned but overwritten on all paths before being read. Remove the dead assignment.

- [x] Done

---

## CLEANUP — Unused parameters

### TODO-36 — UnusedParameter.Local (6 instances)

Run: `grep 'TypeId="UnusedParameter.Local"' /tmp/inspect-com.bovinelabs.grid.*.xml`

Remove unused method parameters. Check callers first.

- [x] Done

### TODO-37 — UnusedMethodReturnValue.Global (4 instances)

Run: `grep 'TypeId="UnusedMethodReturnValue.Global"' /tmp/inspect-com.bovinelabs.grid.*.xml`

Call sites ignore return value of `TryCreate` or similar. Add `if (!...) return false;` checks.

- [x] Done

### TODO-38 — OutParameterValueIsAlwaysDiscarded.Local (1 instance)

Run: `grep 'TypeId="OutParameterValueIsAlwaysDiscarded.Local"' /tmp/inspect-com.bovinelabs.grid.*.xml`

Caller discards `out` value. Either use it or refactor signature.

- [x] Done

---

## DONE

All items above complete.
