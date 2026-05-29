This document outlines the core architectural pillars, strict coding
conventions, and anti-patterns for this Unity ECS codebase. All AI agents and
developers must adhere to these rules to ensure determinism, performance, memory
safety, and modularity across the project.

1. Data Architecture & Memory Management

1.1 Favor IEnableableComponent Over Structural Changes

Problem: Adding and removing components (structural changes) forces sync points
and causes chunk fragmentation, killing performance. Rule: Use
IEnableableComponent to toggle states.

  - Example: Use SystemAPI.SetComponentEnabled<Active>(entity, true/false)
    instead of adding/removing the Active component.
  - System queries should naturally filter out disabled components unless
    explicitly requested via EntityQueryOptions.IgnoreComponentEnabledState.

1.2 Immutable Complex Data via BlobAssetReference

Problem: Storing complex data (like AnimationCurve, string arrays, or deep
nested logic) inside components breaks unmanaged constraints or causes memory
leaks. Rule: All complex authored data must be baked into a
BlobAssetReference<T>.

  - Use BlobBuilder during baking.
  - Examples: BlobCurve, CompositeLogic, ConditionMetaData.
  - Clip data components (e.g., PositionClipData, VolumeBloomClipData) should
    primarily contain a single BlobAssetReference.

1.3 Bitwise Operations for State and Filtering

Rule: When dealing with multiple boolean flags or condition arrays, use bitmasks
(BitArray32, BitArray256) instead of DynamicBuffer<bool> or multiple bool
fields.

  - Allows for blazing-fast logical comparisons (AllTrue, AllFalse, BitAnd,
    CountBits()).

2. Concurrency & Job Safety

2.1 The Accumulator Pattern (Many-to-One Writes)

Problem: Multiple jobs/clips trying to write to the same component (e.g.,
PhysicsVelocity, Stat) at the same time causes race conditions. Rule: Use the
Accumulator pattern.

1.  Gather: Parallel jobs append data to a DynamicBuffer (e.g., PendingForce,
    PendingVelocity) or enqueue to a NativeStream/NativeQueue.
2.  Drain/Apply: A dedicated system later in the frame (e.g.,
    PhysicsProducerForceAccumulatorSystem) drains the buffer and safely applies
    the sum to the actual component.

2.2 NativeQueue and NativeStream for Structural Commands

Rule: If a parallel job needs to trigger structural changes (adding tags,
destroying entities, spawning prefabs) based on complex logic:

1.  Gather the target entities and data into a NativeQueue<T>.ParallelWriter or
    NativeStream.Writer.
2.  Pass the queue/stream to a subsequent sequential IJob to execute the changes
    via EntityCommandBuffer.

  - Why? Prevents ECB playback bloat and allows deduplication before applying
    commands.

2.3 Strict Native Disable Restrictions

Rule: Use [NativeDisableParallelForRestriction] extremely sparingly.

  - Safe use-case: Reading from a specific index of an array where you guarantee
    uniqueness (e.g., IJobParallelHashMapDefer).
  - Safe use-case: Using BovineLabs.Core.Utility.EntityLock to lock a specific
    entity before modifying a buffer (using (EntityLock.Acquire(entity)) { ...
    }).
  - Unsafe: Blindly writing to ComponentLookup<T> or BufferLookup<T> from a
    parallel job.

3. Entity Relationships & Routing

3.1 Contextual Routing (Targets Component)

Problem: Hardcoding entity references makes abilities, timelines, and reactions
rigid and un-reusable. Rule: Every reactive entity acts within a context defined
by the Targets component (Owner, Source, Target, Custom).

  - Use targets.Get(TargetEnum, selfEntity) to resolve who to apply an effect
    to.
  - Never assume the target is self.

3.2 EntityLink Resolution over Hierarchy Traversal

Problem: Traversing Parent and Child buffers at runtime is a massive performance
bottleneck and deeply couples logic to the rig hierarchy. Rule: Use
EntityLinkSchema (baked to a ushort key) to identify specific nodes (e.g.,
"RightHand", "WeaponMuzzle").

  - Resolve at runtime using EntityLinkResolver.TryResolve(...) which does an
    instant O(N) lookup against a flat EntityLinkEntry buffer located on the
    root entity.

4. Timeline & Animation Systems

4.1 The Timeline Component Triad

All animatable properties must be split into three components to separate
authoring, blending, and restoration:

1.  ClipData / ClipBlob: Immutable authored data baked from the Timeline clip.
2.  Initial: A snapshot of the component's state before the timeline took
    control (used to restore state on deactivation).
3.  Animated / Blend: The transient state generated every frame by blending
    clips together.

4.2 The IMixer<T> Pattern

Rule: All Timeline track systems must implement an IMixer<T> to handle clip
overlap.

  - Implement Lerp(a, b, t) for overlapping clips on the same track.
  - Implement Add(a, b) for adding tracks together.
  - Use TrackBlendImpl and TrackLifeImpl utilities to standardize track
    evaluation and cleanup boilerplate.

4.3 Track State Management (Edge Detection)

Rule: Track systems must cleanly handle initialization and teardown using edge
detection (TimelineActive vs TimelineActivePrevious).

  - Activate (!Previous && Current): Capture the Initial state.
  - Update (Current): Sample curves, evaluate logic, and blend.
  - Deactivate (Previous && !Current): Restore the Initial state if
    TrackResetOnDeactivate is present.

5. Reactions & State Machines

5.1 Edge Detection (Active vs ActivePrevious)

Rule: Always use dual-state components to detect state changes without
maintaining separate lists.

  - [WithAll(typeof(Active)), WithDisabled(typeof(ActivePrevious))]: Fires
    exactly once on Enter.
  - [WithAll(typeof(ActivePrevious)), WithDisabled(typeof(Active))]: Fires
    exactly once on Exit.
  - A dedicated system (e.g., ActivePreviousSystem) must sync ActivePrevious to
    Active at the start or end of the update group.

5.2 Condition Event Writers

Rule: Do not write directly to ConditionEvent buffers from arbitrary systems.

  - Instantiate a ConditionEventWriter.Lookup, update it in OnUpdate, and call
    writer.Trigger(key, value). This sets the required dirty flags (EventsDirty)
    so downstream systems know to process the events.

6. Physics & Transforms

6.1 LocalTransform vs LocalToWorld

Problem: LocalToWorld is updated at the end of the frame by
TransformSystemGroup. Reading it during early simulation for moving physics
bodies results in a 1-frame visual lag (jitter). Rule:

  - When resolving positions for active physics bodies (e.g., tracking a target,
    applying PID forces), try to read LocalTransform first if the entity has no
    Parent.
  - Fallback to LocalToWorld only if the entity is parented or is not a physics
    body.

6.2 Frame-Rate Independent Mathematics

Rule: Never use math.lerp with raw DeltaTime for continuous damping or
smoothing. It is mathematically incorrect and behaves differently at 30Hz
vs 144Hz.

  - Use Exponential Decay: smoothed = math.lerp(current, target, 1f -
    math.exp(-smoothing * DeltaTime))
  - Use analytical spring equations (SpringUtility.Sample) for bouncy/elastic
    follow logic.

7. Debugging & Tooling

7.1 Strict BovineLabs.Quill Constraints

Rule: The Quill drawing API must be strictly isolated to prevent overhead in
release builds.

  - Wrap all drawing systems in #if UNITY_EDITOR || BL_DEBUG.
  - Place them in the [UpdateInGroup(typeof(DebugSystemGroup))].
  - Always gate drawing logic behind a SharedStatic<bool> (ConfigVar) using
    TimelineDebugUtility.TryGetDrawer.
    if (!TimelineDebugUtility.TryGetDrawer<MyDebugSystem>(ref state, MyConfig.Enabled.Data, out var drawer)) return;

7.2 Visual Consistency

Rule: Use TimelineDebugColors (e.g., OwnerLink, LinearForce, Connection) for all
debug lines and text. Do not hardcode magic colors unless the visual is entirely
isolated to a specific domain (like rendering filters).
