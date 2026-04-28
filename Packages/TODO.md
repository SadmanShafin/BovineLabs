BovineLabs.Timeline.Animation

    TimelineAnimationBlendTree2DTrackSystem.cs

        [x] Removed the temporary TargetMap pass and now extract unique targets directly from ClipDataMap with GetUniqueKeyArray.
        [x] Replaced the NativeHashMap fallback path with UnsafeList<PerTrackBlend> + linear search.

    TimelineAnimationUnificationSystem.cs

        [x] Already uses linear search; no per-entity hashmap allocation remains.

BovineLabs.Timeline.EntityLinks

    EntityLinkMutateSystem.cs

        [x] Kept order-preserving assign/remove behavior because links are maintained sorted by key.

BovineLabs.Timeline.Essence

    ActionTickDistributionSystem.cs & TimelineEssenceEventSystem.cs & TimelineEssenceIntrinsicSystem.cs

        [x] Stopped deriving unique keys from multi-hashmap scans.
        [x] Added write-time NativeParallelHashSet key tracking and key-list extraction from those sets.
        [x] FixedList4096Bytes boundary remains intentional and documented in code behavior.

BovineLabs.Timeline.PlayerInputs

    InputBufferClearSystem.cs

        [x] Removed temp NativeArray allocation in Normalize (now in-place rotation, zero allocation).
        [x] Replaced O(N^2) RemoveAt filtering with O(N) in-place compaction + tail trim.

BovineLabs.Timeline.Time

    WorldTimeScaleSystem.cs

        [x] Removed NativeReference/job scheduling path and restored direct inline accumulation.

Global Paradigm Consistency

    [x] Updated HitStop target resolution from ResolveTarget(...) to TryResolveTarget(..., out target).
