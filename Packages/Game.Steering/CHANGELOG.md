# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-05-30
### Added
- Initial release of `Game.Steering`.
- `InfluenceField` grid with configurable size, step, and channel count.
- `ObjectiveFieldSystem` — iterative distance-sweep flow field propagation from `NavObjective` goals.
- `ThreatFieldSystem` — stamps threat vectors pointing away from `ThreatSource` entities.
- `Steering.Resolve()` — weighted reactive steering with exponential smoothing, dead-zone filtering, and acceleration clamping.
- `SteeringTransformSyncSystem` — real-time sync of `CameraFocus` and `NavObjective` positions from transforms.
- `FieldBootstrapSystem` — centers the influence field on the `CameraFocus` entity.
- `SteeringSystem` — per-agent steering resolution into `SteeringIntent.PreferredVelocity`.
- `MovementSystem` — applies velocity to `LocalTransform`.
- `BehaviorAuthoring` — stage-based behavior definition with per-channel weights.
- `CameraFocusAuthoring`, `NavObjectiveAuthoring`, `ThreatSourceAuthoring`.
- `SteeringDebugSystem` and `SteeringDebugColors` for Quill-based debug visualization.
- `ExampleCreator` menu item for test scene generation.
