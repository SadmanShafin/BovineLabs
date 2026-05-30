# Game Steering

Weighted reactive steering over dynamic potential/flow fields for Unity DOTS/ECS.

This package implements an influence-field–based steering system where independent fields (objectives, threats, etc.) are sampled, weighted, and blended into a **preferred velocity** per agent. The final collision-free velocity should be handled by a separate avoidance layer (e.g. ORCA/RVO).

> **Design backing**: Reynolds 1999 *Steering Behaviors For Autonomous Characters* (weighted behavior blending); Treuille, Cooper & Popović 2006 *Continuum Crowds* (dynamic potential/flow fields for real-time crowd flow).

## Installation

Add this package to your Unity project using the Package Manager:

1. Open the Package Manager (`Window > Package Manager`).
2. Click the `+` button and select `Add package from git URL...`.
3. Enter the URL of this repository.

## Requirements

- **Unity**: 2022.3 or newer
- **com.unity.entities**: 1.3.0+
- **com.unity.collections**: 1.5.1+
- **com.unity.burst**: 1.8.11+
- **com.unity.mathematics**: 1.3.2+

## Architecture

The steering stack follows a layered pipeline:

```
Influence fields + steering weights
        ↓
SteeringIntent.PreferredVelocity
        ↓
ORCA / avoidance system
        ↓
final movement
```

The package is organized into three assemblies:

| Assembly | Purpose |
|----------|---------|
| `Game.Steering` | Runtime systems: field bootstrapping, objective integration, threat stamping, steering resolution, movement |
| `Game.Steering.Authoring` | Authoring components and bakers (Editor-only) |
| `Game.Steering.Debug` | Debug visualization via BovineLabs Quill (Editor-only) |

## Core Concepts

### Influence Fields

A single `InfluenceField` entity holds a grid of `InfluenceValue` vectors, one per channel per cell. Channels correspond to `Influence` enum values (Objective, Threat, etc.).

- **Objective field**: Propagated via iterative distance sweeps from `NavObjective` entities, producing a flow field that points toward goals.
- **Threat field**: Stamped by `ThreatSource` entities; vectors point *away* from threats with quadratic falloff.

### Weighted Steering

`BehaviorBlob` defines per-stage weights for each influence channel. `Steering.Resolve()` samples the field at the agent's position, accumulates weighted contributions, then applies:

1. **Dead-zone filtering** — ignores tiny noisy magnitudes.
2. **Exponential smoothing** — frame-rate independent heading response via `α = 1 − exp(−response × dt)`.
3. **Acceleration clamping** — prevents instant velocity snapping.
4. **Speed clamping** — caps output to `MaxSpeed`.

### Components

| Component | Description |
|-----------|-------------|
| `BehaviorRef` | Reference to the baked `BehaviorBlob` (weights + stage headers) |
| `Stage` | Current stage index on the agent entity |
| `SteeringIntent` | Output: `PreferredVelocity` + `MaxSpeed` for downstream avoidance |
| `CameraFocus` | Followed by the field origin; synced from `LocalTransform` at runtime |
| `NavObjective` | Goal position; synced from `LocalTransform` at runtime |
| `ThreatSource` | Radius + Strength for threat field stamping |

### Systems (execution order)

| System | Group | Description |
|--------|-------|-------------|
| `SteeringTransformSyncSystem` | `SimulationSystemGroup` (before `FieldBootstrapSystem`) | Syncs `CameraFocus` and `NavObjective` positions from transforms every frame |
| `FieldBootstrapSystem` | `SimulationSystemGroup` | Centers the influence field grid on the `CameraFocus` position |
| `ObjectiveFieldSystem` | `SimulationSystemGroup` (after `FieldBootstrapSystem`) | Propagates objective flow field via distance sweeps |
| `ThreatFieldSystem` | `SimulationSystemGroup` (after `FieldBootstrapSystem`) | Stamps threat vectors pointing away from threat sources |
| `SteeringSystem` | `SimulationSystemGroup` (after field systems) | Resolves weighted steering into `SteeringIntent.PreferredVelocity` |
| `MovementSystem` | `SimulationSystemGroup` (after `SteeringSystem`) | Applies velocity to `LocalTransform` |

## Usage

### 1. Define a Behavior

Add `BehaviorAuthoring` to your agent entity and configure stages with weights:

```csharp
behavior.Stages = new[]
{
    new BehaviorAuthoring.StageDefinition
    {
        Name = "Default",
        MaxSpeed = 5f,
        TurnResponse = 12f,       // exponential smoothing factor
        MaxAcceleration = 40f,    // velocity units / second²
        DeadZone = 0.001f,
        Weights = new[]
        {
            new BehaviorAuthoring.InfluenceWeight
            {
                Influence = Influence.Objective,
                Weight = 1f
            },
            new BehaviorAuthoring.InfluenceWeight
            {
                Influence = Influence.Threat,
                Weight = 1f
            }
        }
    }
};
```

### 2. Place Objectives and Threats

- Add `NavObjectiveAuthoring` to goal GameObjects.
- Add `ThreatSourceAuthoring` to threat GameObjects with `Radius` and `Strength`.
- Add `CameraFocusAuthoring` to the GameObject the field should follow.

### 3. Feed ORCA

`SteeringIntent.PreferredVelocity` is the intended input for your ORCA/RVO avoidance system. Do **not** apply it directly to movement if ORCA owns final velocity resolution.

## Example Scene

Use the menu item **BovineLabs → Steering → Create Example Scene** to generate a test scene with a camera focus, nav objective, threat source, and a steered enemy.

## License

Copyright (c) BovineLabs. All rights reserved.
