Here is the first batch of 20 files from the BovineLabs.Timeline.* packages that
can be significantly improved by strictly enforcing the rules laid out in the
new AGENTS.md document.

This batch primarily focuses on the most critical architectural violations:
Anti-Jitter (Rule 1.1), Track State Management (Rule 3.2), and Framerate
Independent Math (Rule 1.3).

Batch 1: Core Physics, Transform, and Debug Systems

Physics & Application Systems (Rule 1.1: LocalTransform vs LocalToWorld Anti-Jitter)

These systems currently rely on LocalToWorld to compute vectors, offsets, or
errors. Because LocalToWorld is 1-frame behind for active physics bodies, this
introduces massive physical inaccuracies, lag, and PID oscillation.

1.  BovineLabs.Timeline.Physics/PhysicsPidApplySystem.cs
      - Violation: Rule 1.1
      - Fix: Computes PID positional/angular error using LocalToWorld for both
        the acting entity and tracking target. Needs to be rewritten to resolve
        up-to-date positions via LocalTransform first so the PID loop operates
        on current-frame data, eliminating stutter.
2.  BovineLabs.Timeline.Physics/PhysicsKinematicsApplySystem.cs
      - Violation: Rule 1.1
      - Fix: DirectionTarget and spatial rotation logic uses LocalToWorld.
        Lags 1 frame behind when applying directional forces towards/away from
        moving targets. Must prioritize LocalTransform.
3.  BovineLabs.Timeline.Physics/TriggerEvents/PhysicsTriggerForceSystem.cs
      - Violation: Rule 1.1
      - Fix: Impact forces (Vortex, Radial, Directional) are calculated from
        LtwLookup[self] and LtwLookup[other]. If both are dynamic bodies, the
        explosion/force is applied using stale spatial data.
4.  BovineLabs.Timeline.Physics/TriggerEvents/PhysicsTriggerInstantiateSystem.cs
      - Violation: Rule 1.1
      - Fix: Spawns prefabs using LocalToWorld offsets. Spawning particle
        effects or hit-markers on fast-moving bodies currently results in them
        trailing behind the impact point.
5.  BovineLabs.Timeline.Physics/Teleports/Physicsteleportapplysystem.cs
      - Violation: Rule 1.1
      - Fix: Line-of-sight checks and candidate sphere generation use
        LocalToWorld. The teleport anchor drifts behind the actual visual mesh
        position at high speeds.
6.  BovineLabs.Timeline.Physics/PhysicsVelocityOverrideSystem.cs
      - Violation: Rule 1.1
      - Fix: Calculates spatial vectors (e.g., Target Local Space) using
        LocalToWorld rotation, causing applied velocities to slightly skew
        off-axis when the body rotates rapidly.
7.  BovineLabs.Timeline.Physics/PhysicsMath.cs
      - Violation: Rule 1.1
      - Fix: The utility methods ResolveSpaceVector, ResolveLinearPidTarget, and
        ResolveAngularPidTarget hardcode LocalToWorld parameter lookups. They
        need to be updated to accept and prioritize LocalTransform lookups.
8.  BovineLabs.Timeline.Animation/FollowPositionOnlySystem.cs
      - Violation: Rule 1.1
      - Fix: Reads TargetL2WLookup and sets LocalTransform.Position. This
        guarantees that the follower is permanently 1 frame behind the target.
9.  BovineLabs.Timeline.Distance/DistanceToStatSystem.cs
      - Violation: Rule 1.1
      - Fix: Uses LocalToWorld to compute the distance between two entities. If
        evaluating a proximity trigger on two physics bodies, the math is stale.
10. BovineLabs.Timeline.EntityLinks/EntityLinkParentSystem.cs
      - Violation: Rule 1.1
      - Fix: During EnterJob, uses LocalToWorld to grab the new parent's spatial
        matrix. Needs to fall back securely to LocalTransform if unparented
        physics object.

Debug Systems (Rule 1.1 & Rule 4.2: Visual Jitter & Telemetry)

Debug tools currently use LocalToWorld blindly. In high-speed gameplay, the
debug lines visually detach from the entities they are supposedly tracking.

11. BovineLabs.Timeline.Physics.Debug/PhysicsLinearPIDDebugSystem.cs
      - Violation: Rule 1.1
      - Fix: Debug lines mapping error and goal float visibly behind the entity
        during fast movements.
12. BovineLabs.Timeline.Physics.Debug/PhysicsAngularPIDDebugSystem.cs
      - Violation: Rule 1.1
      - Fix: Forward/Up vectors are drawn 1-frame lagged.
13. BovineLabs.Timeline.Physics.Debug/TeleportDebugSystem.cs
      - Violation: Rule 1.1
      - Fix: Teleport candidate sphere and clearance gizmos detach from the
        player.
14. BovineLabs.Timeline.Distance.Debug/DistanceToStatDebugSystem.cs
      - Violation: Rule 1.1
      - Fix: The "Elegant Tether" lines trail behind the actual objects.
15. BovineLabs.Timeline.EntityLinks.Debug/EntityLinkDebugSystem.cs
      - Violation: Rule 1.1
      - Fix: Connection manifolds lag behind the entities.

Track Standardization (Rule 3.2: State Reset & Stale Tracks)

These older systems manually manage Activation/Deactivation lifecycles instead
of utilizing the robust, boilerplate-free SharedTrackJobs.cs
(ResetStateTrackJob, DisableStaleTrackJob, etc.) implemented for the Physics
module.

16. BovineLabs.Timeline.Transform/PositionTrackSystem.cs
      - Violation: Rule 3.2
      - Fix: Uses bespoke ActivateResetJob and DeactivateResetJob
        implementations. Should be refactored to use the standardized generic
        track state jobs to ensure consistency across the project.
17. BovineLabs.Timeline.Transform/RotationTrackSystem.cs
      - Violation: Rule 3.2
      - Fix: Same as above. Uses manual setup.
18. BovineLabs.Timeline.Transform/ScaleTrackSystem.cs
      - Violation: Rule 3.2
      - Fix: Same as above. Uses manual setup.
19. BovineLabs.Timeline.Time/WorldTimeScaleSystem.cs
      - Violation: Rule 3.2 & Rule 3.1
      - Fix: Does not use TrackBlendImpl<T> or standardized state jobs.
        Hardcodes a strange array-shifting accumulation logic for blending time
        weights.

Math & Transitions (Rule 1.3: Framerate Independent Smoothing)

20. BovineLabs.Timeline.Animation/TimelineAnimationUnificationSystem.cs
      - Violation: Rule 1.3
      - Fix: Anim weights are blended via CurrentWeight += speed * DeltaTime.
        This is linear, framerate-dependent, and prone to overshooting bounds.
        Must be converted to Exponential Decay to comply with the project
        standards.

Let me know if you would like me to rewrite any specific files from this batch
using the new rules, or if you would like me to generate Batch 2 (21-40).
