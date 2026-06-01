# BovineLabs Reaction & Essence - Example Scenes

This package contains example scenes demonstrating the key concepts of the BovineLabs Reaction & Essence systems.

## Scenes Overview

Each example scene has a **main `Scene.unity`** with a SubScene reference, and a **subscene asset** (e.g., `Range.unity`) containing the example entities.

---

## Example 1: Range - Randomized Loot Stats (`Range/`)

### Concept
`Range` generates a random value between Min and Max **once at entity initialization** (via `InitializeActionStatRangeSystem`). It does NOT reroll on each activation.

### Example: Randomized Swords
Spawn multiple "Sword" entities, each with a random Attack Power between 5 and 15.

### Setup in Unity

1. **Open** `Range/Range.unity` subscene
2. **Select** one of the Sword GameObjects
3. **In ReactionAuthoring → Active**:
   - Set **Trigger** = `OnInitialize` (0)
   - Set **Duration** = `0` (instant)
4. **In ActionStatAuthoring → Stats**:
   - Add a new stat entry
   - **Stat Schema**: Load `Assets/Settings/Schemas/Stats/AttackPower.asset`
   - **Value Type**: Select `Range`
   - **Range**:
     - **Min**: `5`
     - **Max**: `15`
     - **Modify Type**: `Added`
5. **Save** the scene
6. **Enter Play Mode** and spawn multiple Sword entities - each will have a different random attack value!

---

## Example 2: Linear - Dynamic Scaling (`Linear/`)

### Concept
`Linear` scales stat modifiers **dynamically every time the Action fires**, reading from the `ConditionValues` buffer. Maps an input range (From) to an output range (To).

### Example: Charge Attack
A bow's damage scales from 10 to 50 based on charge time (0-1000ms).

### Setup in Unity

1. **Open** `Linear/Linear.unity` subscene
2. **Select** the Player - Charge Attack GameObject
3. **In ReactionAuthoring → Conditions**:
   - Add a condition
   - **Condition**: Load an event that fires during charge (e.g., `ChargeTime` event from an Intrinsic)
   - **Operation**: `GreaterThan`
   - **Value**: `0`
   - **Features**: Set to `Value` (records the charge time value)
4. **In ActionStatAuthoring → Stats**:
   - Add a new stat entry
   - **Stat Schema**: Load `Assets/Settings/Schemas/Stats/ChargeDamage.asset`
   - **Value Type**: Select `Linear`
   - **Linear**:
     - **Condition**: Select the same condition from step 3
     - **FromMin**: `0`
     - **FromMax**: `1000`
     - **ToMin**: `10`
     - **ToMax**: `50`
     - **Modify Type**: `Added`
5. **Save** the scene

---

## Example 3: Condition Feature - Boolean Trigger (`Condition/`)

### Concept
`Condition` is a **boolean check** (True/False). If the condition is met, it sets a bit to true in `ConditionActive`. Used for triggers like "play hurt animation".

### Example: Slow Down When Hurt
When taking damage, reduce movement speed temporarily.

### Setup in Unity

1. **Open** `Condition/Condition.unity` subscene
2. **Select** the Player - Condition Example GameObject
3. **In ReactionAuthoring → Conditions**:
   - Add a condition
   - **Condition**: Load `Assets/Settings/Schemas/Events/Reactions/OnDamaged.asset`
   - **Operation**: `GreaterThan`
   - **Value**: `0`
   - **Features**: Set to `Condition`
4. **In ActionStatAuthoring → Stats**:
   - Add a new stat entry
   - **Stat Schema**: Load `Assets/Settings/Schemas/Stats/MovementSpeed.asset`
   - **Value Type**: `Fixed`
   - **Fixed**:
     - **Value**: `-0.3`
     - **Modify Type**: `Multiplied` (30% speed reduction)
5. **Save** the scene

---

## Example 4: Value Feature - Data Recording (`Value/`)

### Concept
`Value` **records the actual number** passed by the event into `ConditionValues` buffer. Does NOT trigger the reaction by itself - just stores data for other Actions to use.

### Example: Blood Particles Based on Damage
Spawn particles equal to the damage taken.

### Setup in Unity

1. **Open** `Value/Value.unity` subscene
2. **Select** the Enemy - Value Example GameObject
3. **Create** a Condition that listens to `OnHit` event with **Features = Value** (records damage value)
4. **Create** an Action that spawns particles, using the recorded value to set particle count
5. **Save** the scene

---

## Example 5: Intrinsics & Events - Thorns Armor (`Intrinsics/`)

### Concept
Intrinsics are dynamic pools (like Health) that auto-fire events when modified. The `IntrinsicSchemaObject` can have a child `ConditionEventObject` that triggers reactions.

### Example: Thorns Armor
When taking damage, deal 50% of damage taken back to the attacker.

### Setup in Unity

1. **Open** `Intrinsics/Intrinsics.unity` subscene
2. **Select** the Player - Thorns Armor GameObject
3. **In StatAuthoring → Intrinsic Defaults**:
   - Add an entry
   - **Intrinsic**: Load `Assets/Settings/Schemas/Intrinsics/CurrentHealth.asset`
   - **Value**: `100`
4. **Create the Event**:
   - Expand `CurrentHealth.asset` in Project view
   - You should see a child `CurrentHealth_Event` object
   - If not, click "Add Event Condition" on the Intrinsic asset
5. **In ReactionAuthoring → Conditions**:
   - Add a condition
   - **Condition**: Drag `CurrentHealth_Event` from the Intrinsic's child
   - **Operation**: `LessThan`
   - **Value**: `0`
   - **Features**: `Value` (records the damage delta, which is negative)
6. **In ActionIntrinsicAuthoring → Intrinsics**:
   - Add an entry
   - **Intrinsic**: Select the same `CurrentHealth`
   - **Amount**: `-15` (half of 30 damage = 15 thorns damage)
   - **Target**: `Source` (the attacker)
7. **Save** the scene

---

## Example 6: Accumulate Feature - Summed Values (`Accumulate/`)

### Concept
`Accumulate` is `Condition + Value`, but instead of **replacing** the value on each event, it **adds** them together. Used when multiple events fire in the same frame (e.g., shotgun pellets).

### Example: Stagger from Shotgun
Getting hit by 3 pellets (10 damage each) = 30 total damage = staggered.

### Setup in Unity

1. **Open** `Accumulate/Accumulate.unity` subscene
2. **Select** the Enemy - Shotgun Victim GameObject
3. **In ReactionAuthoring → Conditions**:
   - Add a condition
   - **Condition**: Load `OnHit` event
   - **Operation**: `GreaterThan`
   - **Value**: `0`
   - **Features**: Set to `Accumulate`
4. **In ActionStatAuthoring → Stats**:
   - Add a new stat entry for stagger
   - **Stat Schema**: Load `Assets/Settings/Schemas/Stats/EnemyMaxStagger.asset`
   - **Value Type**: `Linear`
   - **Linear**:
     - **Condition**: Same as step 3
     - **FromMin**: `0`
     - **FromMax**: `50` (if health is 50)
     - **ToMin**: `0`
     - **ToMax**: `100` (100% stagger)
     - **Modify Type**: `Added`
5. **Save** the scene

---

## Key Differences Summary

| Feature | Purpose | Triggers Reaction? | Value Storage |
|---------|---------|---------------------|---------------|
| **Condition** | Boolean check | Yes | No |
| **Value** | Record data | No | Yes |
| **Accumulate** | Sum data | Yes | Yes (summed) |

| ValueType | When Calculated | Rerolls? |
|-----------|-----------------|----------|
| **Fixed** | Always (compile time) | N/A |
| **Range** | Entity init only | No |
| **Linear** | Every activation | Yes (reads ConditionValues) |

---

## Required Schemas

The following schemas have been pre-created in `Assets/Settings/Schemas/`:

### Stats
- `Stats/AttackPower.asset` (key: 100)
- `Stats/MovementSpeed.asset` (key: 101)
- `Stats/ChargeDamage.asset` (key: 102)
- `Stats/DamageMultiplier.asset` (key: 103)
- `Stats/ThornsArmor.asset` (key: 104)

### Intrinsics
- `Intrinsics/CurrentHealth.asset` (key: 200)
- `Intrinsics/ChargeTime.asset` (key: 201)
- `Intrinsics/BloodParticleCount.asset` (key: 202)
- `Intrinsics/DamageTaken.asset` (key: 203)
- `Intrinsics/StaggerAmount.asset` (key: 204)

### Events
- `Events/Reactions/OnDamaged.asset`
- `Events/Reactions/OnChargeComplete.asset`
- `Events/Reactions/OnHit.asset`
- `Events/Reactions/OnStagger.asset`

---

## Additional Notes

- **Main scenes** contain SubScene references pointing to the example subscenes
- **Subscenes** contain the actual entities with authoring components
- Open the **subscene** in Unity to configure the authoring components
- The **main scene** SubScene component will automatically load the subscene at runtime
