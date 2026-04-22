# Game Schema Reference

Auto-generated catalog of all ConditionEventObject, StatSchemaObject, and IntrinsicSchemaObject assets.

## Stats (StatSchemaObject) — 68 total

Dynamic modifiable attributes. Modified via Added, Additive (%), Multiplicative modifiers.

### Offensive

| Key | Asset | Description |
|-----|-------|-------------|
| 4 | WeaponDamage | Base damage multiplier for LMB/RMB/stabs |
| 5 | ElementalDamage | Base damage multiplier for augment/elemental effects |
| 6 | CriticalChance | % chance to deal critical damage |
| 7 | CriticalDamageMultiplier | Crit damage scale (base 2.0x) |
| 8 | AttackSpeed | Attack animation speed. Slow trait: -20% |
| 9 | HitboxSize | Attack hitbox scale. Winter's Reach: +2% per Chill stack |
| 10 | StaggerDamageMultiplier | Stagger dealt per hit |
| 11 | KnockbackMultiplier | Knockback force. Incremental Distance: +5%/hit after 8, cap 40% |
| 58 | ExecutionDamageMultiplier | Execution damage scale (fixed + combo multiplier) |
| 36 | FireDamageMultiplier | Fire element damage scale |
| 37 | FrostDamageMultiplier | Frost element damage scale |
| 38 | WindDamageMultiplier | Wind element damage scale |
| 39 | EarthDamageMultiplier | Earth element damage scale |
| 40 | LightDamageMultiplier | Light element damage scale |
| 41 | ShadowDamageMultiplier | Shadow element damage scale |
| 42 | DoTDamageMultiplier | Damage-over-time scale. Ghost in the Burn: +100% |
| 54 | ArmorPenetration | Bypass target armor %. Sunder: -5% armor/stack |

### Defensive

| Key | Asset | Description |
|-----|-------|-------------|
| 1 | Max Health | HP cap. Vitality gives +10 HP/point |
| 13 | Vitality | 1 point = 10 MaxHealth |
| 14 | Resilience | % damage reduction |
| 49 | Lifesteal | % damage returned as Provisional HP |
| 50 | HealingReceivedMultiplier | Healing scale. Iron Resolve: reduced |
| 51 | DamageTakenMultiplier | Incoming damage scale. Glass Mirage: 2x |
| 52 | DamageReflection | % melee damage rebound. Elastic Resolve: 25% chance, 50% reflect |
| 53 | ShieldDecayRate | Shield decay %/sec (base 1%) |
| 28 | MaxShield | Shield capacity cap |
| 15 | MaxStaggerResistance | Stagger threshold for enemies |

### Status Chances

| Key | Asset | Description |
|-----|-------|-------------|
| 45 | BurnChance | % to apply Burn on fire hit. Boss Infusion: fire boss bonus |
| 46 | FreezeChance | % to apply Freeze on frost hit. Boss Infusion: ice boss bonus |
| 47 | StatusChance | Generic status effect application rate |
| 48 | StaggerChance | % to apply stagger. Neurothorn: +20% |

### Duration

| Key | Asset | Description |
|-----|-------|-------------|
| 43 | BuffDurationMultiplier | Buff duration scale. Fuel Source: +10% |
| 44 | DebuffDurationMultiplier | Debuff duration scale. Fuel Source: +10% |
| 59 | ComboDuration | Seconds before combo drops (target recovers/techs) |
| 60 | StaggerRegenerationRate | Stagger meter fade %/sec (base 20%). Deepfreeze: 0 for frozen |

### Utility & Mobility

| Key | Asset | Description |
|-----|-------|-------------|
| 2 | Speed | Generic speed stat |
| 16 | MovementSpeed | Movement scale. Adrenaline: +25-50%, Tornado: +2%/sec |
| 17 | DashDistance | Dash distance (base 1/4 screen). Slingshot Wind augment |
| 18 | CooldownReduction | Ability cooldown scale |

### Caps & Limits

| Key | Asset | Description |
|-----|-------|-------------|
| 19 | MaxBladeTempest | BladeTempest resource cap (3 or 5) |
| 20 | MaxAugmentCapacity | Max augments Arvex can hold |
| 21 | MaxItemCapacity | Max item slots |
| 29 | MaxComboPoints | Combo point cap |
| 30 | MaxArmorStacks | Armor stack cap |
| 31 | MaxFatigue | Companion fatigue cap |
| 61 | MaxWeaponThrows | Weapon throw charge cap (base 4) |
| 62 | MaxParries | Parry charge cap (base 3) |
| 63 | MaxExecutes | Execute charge cap (base 3) |
| 64 | MaxDrive | Drive gauge cap |
| 65 | MaxBleedStacks | Bleed stack cap |
| 66 | MaxScorchedStacks | Scorched stack cap |
| 67 | MaxInjuries | Companion injury/trauma cap |
| 68 | MaxLitStacks | Lit debuff stack cap |
| 69 | MaxStyleScore | Style meter cap |
| 70 | MaxDashCharges | Dash charge cap (base 3, whiteboard) |
| 71 | MaxMomentum | Momentum cap (base 100) |
| 72 | MaxClones | Active shadow clone cap (base 3) |
| 73 | MaxBlazeStacks | Fire stack cap (base 5) |
| 74 | MaxChillStacks | Frost stack cap (base 5) |
| 75 | MaxGustStacks | Wind stack cap (base 5) |
| 76 | MaxRetributionStacks | Light stack cap (base 5) |
| 77 | MaxResolveStacks | Earth stack cap (base 10) |
| 78 | MaxShadeStacks | Shadow stack cap (base 5) |
| 79 | MaxSunderStacks | Sunder debuff cap (base 99) |
| 80 | MaxMarkStacks | Mark stack cap (base 99) |
| 81 | MaxImpaleClimaxStored | Earth climax impale storage cap (base 3) |
| 82 | MaxCompanionMorale | Companion morale cap (base 100) |
| 83 | MaxEnemyResilience | Enemy resilience meter cap (base 100) |
| 84 | MaxInfluence | Faction influence cap for Nyx/Immrit (base 100) |
| 85 | MaxDeityFavor | Deity favor cap (base 100) |
| 86 | MaxElixirs | Elixir slot cap |
| 87 | MaxPotions | Potion slot cap |
| 3 | Fire Stack | [NOTE] Possibly misplaced — elemental stacks are Intrinsics. Consider renaming to MaxFireStacks or removing |

### Enemy

| Key | Asset | Description |
|-----|-------|-------------|
| 32 | CompanionMaxHp | Companion HP cap |
| 33 | EnemyMaxHp | Enemy HP cap |
| 34 | EnemyMaxStagger | Enemy stagger cap |
| 35 | DoomThreshold | Doom timer cap before Nimble/Berserk triggers |

### Companion & Base

| Key | Asset | Description |
|-----|-------|-------------|
| 22 | BaseWorkPerformance | Base task speed. Graveyard Shift: +30% |
| 23 | FatigueRate | Fatigue gain multiplier. Burnout: 2.0x |
| 24 | FatigueRecoveryRate | Fatigue heal multiplier |
| 25 | CompanionAggression | AI aggression. Hothead: +20% |
| 26 | ResourceOutputMultiplier | Base output. Farsoul: +50% |
| 27 | InjuryDevelopmentRate | Injury speed. Glass Cannon: +100% |

### Progression

| Key | Asset | Description |
|-----|-------|-------------|
| 55 | ExperienceMultiplier | XP gain scale. Always Itching: +Plague XP. Fluxlink: bonus from elementals |
| 56 | LootDropMultiplier | Loot scale. Soloist: bonus when alone |
| 57 | Luck | Luck stat for random outcomes |

---

## Intrinsics (IntrinsicSchemaObject) — 73 total

Direct integer pools, resources, currencies, stacks. Modified via ActionIntrinsic (Add/Set/Subtract).

### Combat Pools & Meters

| Key | Asset | Range | Default | Max Stat | Description |
|-----|-------|-------|---------|----------|-------------|
| 2 | CurrentHealth | 0..999999 | 100 | Max Health | Actual HP pool |
| 3 | ProvisionalHealth | 0..999999 | 0 | Max Health | White health pool; life-stealable back |
| 4 | CurrentShield | 0..999999 | 0 | MaxShield | Shield points. Decays 1%/sec |
| 5 | BladeTempest | 0..5 | 0 | MaxBladeTempest | Combat resource for Charge/Billion Slash |
| 6 | ComboPoints | 0..100 | 0 | MaxComboPoints | Earned via parries/dashes. Spent for abilities |
| 7 | Momentum | 0..100 | 0 | MaxMomentum | Gained by moving. At 100: next dash = AoE counterstrike |
| 8 | StaggerMeter | 0..999999 | 0 | EnemyMaxStagger | Enemy stagger buildup. Resets after 5s no-hit |
| 9 | DashCharges | 0..10 | 1 | MaxDashCharges | Dash uses available |
| 50 | RecentDamagePool | 0..99999 | 0 | — | Accumulator for damage burst tracking (Provisional HP returns, Overkill) |
| 18 | ArmorStacks | 0..100 | 0 | MaxArmorStacks | Consumed to prevent flinch; converts damage to Provisional HP |

### Combat Charges & Ammo

| Key | Asset | Range | Default | Max Stat | Description |
|-----|-------|-------|---------|----------|-------------|
| 57 | WeaponThrowCharges | 0..99 | 4 | MaxWeaponThrows | Q4 ability charges (whiteboard: 4) |
| 58 | ParryCharges | 0..99 | 3 | MaxParries | Block/parry uses (whiteboard: 3) |
| 59 | ExecuteCharges | 0..99 | 3 | MaxExecutes | Execution uses (whiteboard: 3) |
| 60 | ActiveClones | 0..99 | 0 | MaxClones | Currently spawned shadow clones (cap via MaxClones) |

### Elemental & Status Stacks

| Key | Asset | Range | Default | Max Stat | Description |
|-----|-------|-------|---------|----------|-------------|
| 10 | BlazeStacks | 0..5 | 0 | MaxBlazeStacks | Fire stacks. At 5: explosion + reset |
| 11 | ChillStacks | 0..5 | 0 | MaxChillStacks | Frost stacks. At 4: Root. At 5: Freeze + reset |
| 12 | RetributionStacks | 0..5 | 0 | MaxRetributionStacks | Light stacks. At 5: Chain Lightning + reset |
| 13 | GustStacks | 0..5 | 0 | MaxGustStacks | Wind stacks. At 5: Tornado spawn + reset |
| 14 | ResolveStacks | 0..10 | 0 | MaxResolveStacks | Earth stacks. At 5: Armor. At 10: Impale + reset |
| 15 | ShadeStacks | 0..5 | 0 | MaxShadeStacks | Shadow stacks. At 5: Shadow Dash + reset |
| 16 | SunderStacks | 0..99 | 0 | MaxSunderStacks | -5% armor per stack |
| 17 | LitStacks | 0..99 | 0 | MaxLitStacks | +5% damage taken from Arvex per stack |
| 52 | MarkStacks | 0..99 | 0 | MaxMarkStacks | Consumed to guarantee Critical Hit |
| 51 | ImpaleClimaxStored | 0..3 | 0 | MaxImpaleClimaxStored | Stored Earth climax impale charges |
| 66 | BleedStacks | 0..99 | 0 | MaxBleedStacks | Bleed: 20% damage/5sec. Grab deals +50% to bleeding |
| 67 | ScorchedStacks | 0..99 | 0 | MaxScorchedStacks | Hellfall double-hit debuff |

### Companion

| Key | Asset | Range | Default | Max Stat | Description |
|-----|-------|-------|---------|----------|-------------|
| 19 | CompanionFatigue | 0..100 | 0 | MaxFatigue | Fatigue bar. At 100: collapse (or Phantom State if Driftworn) |
| 20 | CompanionFriendship | -3..3 | 0 | — | Friendship spectrum. Drives positive/negative trait development |
| 21 | CompanionMorale | 0..100 | 50 | MaxCompanionMorale | Morale meter (Neurotic trait logic) |
| 53 | CompanionCurrentHp | 0..999999 | 100 | CompanionMaxHp | Companion HP pool |
| 68 | CompanionTraumas | 0..99 | 0 | MaxInjuries | Injury/trauma count. Fears Conquered: cleanse 4. Glass Cannon: +100% rate |
| 70 | ConsecutiveMissionsSurvived | 0..9999 | 0 | — | Streak without KO. Mythcoil/Scar Collector: permanent damage per mission |

### Enemy

| Key | Asset | Range | Default | Max Stat | Description |
|-----|-------|-------|---------|----------|-------------|
| 54 | EnemyCurrentHp | 0..999999 | 100 | EnemyMaxHp | Enemy HP pool |
| 55 | EnemyResilienceMeter | 0..100 | 100 | MaxEnemyResilience | Enemy armor/defense meter. Shredded by Frost/Sunder |
| 47 | DoomTimer | 0..9999 | 0 | DoomThreshold | Ticks toward Nimble/Berserk properties |
| 49 | BossPhase | 0..10 | 1 | — | Boss phase tracker (Phase 2: Living Flame) |

### Currencies — Combat & Shop

| Key | Asset | Range | Default | Description |
|-----|-------|-------|---------|-------------|
| 22 | Dreamshards | 0..999999 | 0 | Combat/shop currency |

### Currencies — Base Construction

| Key | Asset | Range | Default | Description |
|-----|-------|-------|---------|-------------|
| 23 | Salvage | 0..999999 | 0 | Building/upgrading currency |
| 24 | PowerShards | 0..999999 | 0 | Base power currency |
| 44 | GeneratedPower | 0..999 | 0 | Power Tower output for unmanned operations |

### Currencies — Scryers' Spire & Map

| Key | Asset | Range | Default | Description |
|-----|-------|-------|---------|-------------|
| 30 | Intel | 0..999999 | 0 | Scryers' Spire map manipulation |
| 31 | Scrolls | 0..999999 | 0 | Secondary map resource |
| 32 | Memories | 0..999999 | 0 | Rerolls, Nexus of Fates |

### Currencies — Raw Materials

| Key | Asset | Range | Default | Description |
|-----|-------|-------|---------|-------------|
| 25 | Gems | 0..999999 | 0 | Tier 1/3 material |
| 26 | Stones | 0..999999 | 0 | Tier 1 material |
| 27 | Metals | 0..999999 | 0 | Tier 1 material |
| 28 | Soulbark | 0..999999 | 0 | Tier 1 material |
| 29 | Husks | 0..999999 | 0 | Used for Minions |

### Currencies — Crafted Materials

| Key | Asset | Range | Default | Description |
|-----|-------|-------|---------|-------------|
| 33 | SoulInfusedGems | 0..999999 | 0 | Tier 3 crafted |
| 34 | RefinedMineralBars | 0..999999 | 0 | Tier 3 crafted |
| 35 | SoulOrbs | 0..999999 | 0 | Special orb |
| 36 | RerollOrbs | 0..999999 | 0 | Reroll currency |
| 37 | PowerCores | 0..999999 | 0 | Power equipment |
| 38 | GoldenOrbs | 0..999999 | 0 | Golden currency |
| 39 | EnhanceOrbs | 0..999999 | 0 | Enhancement currency |
| 40 | ExchangeOrbs | 0..999999 | 0 | Exchange currency |

### Currencies — Progression

| Key | Asset | Range | Default | Description |
|-----|-------|-------|---------|-------------|
| 41 | TalentPoints | 0..999 | 0 | Talent unlock currency |
| 42 | SkillPoints | 0..999 | 0 | Skill upgrade currency |
| 43 | HealingPoolLiquid | 0..999999 | 0 | Healing station resource |
| 62 | DriveGauge | 0..9999 | 0 | Combat/health drive meter |
| 63 | Fuel | 0..999999 | 0 | Base resource #12 |

### Consumables

| Key | Asset | Range | Default | Max Stat | Description |
|-----|-------|-------|---------|----------|-------------|
| 61 | ItemCharges | 0..99 | 0 | MaxItemCapacity | Item use tracker |
| 64 | Elixirs | 0..99 | 0 | MaxElixirs | Elixir count. Alchemist trait: crafts in 0.5 days |
| 65 | Potions | 0..99 | 0 | MaxPotions | Potion count |

### Run & Meta

| Key | Asset | Range | Default | Max Stat | Description |
|-----|-------|-------|---------|----------|-------------|
| 45 | BaseTime | 0..999999 | 0 | — | Day cycle tracker for construction/healing |
| 46 | CheckpointsPassed | 0..999999 | 0 | — | Checkpoint counter |
| 48 | CurrentAugmentCount | 0..20 | 0 | MaxAugmentCapacity | Active augment count |
| 69 | PlagueXP | 0..999999 | 0 | — | Plague experience. Always Itching trait |
| 71 | InfluenceNyx | 0..100 | 0 | MaxInfluence | Nyx faction influence |
| 72 | InfluenceImmrit | 0..100 | 0 | MaxInfluence | Immrit faction influence |
| 73 | StyleScore | 0..100 | 0 | MaxStyleScore | Style meter (DMC-style). Drains over time, spikes on parries/combos |
| 56 | DeityFavor | 0..100 | 0 | MaxDeityFavor | God alignment tracker for door UI |

### Internal/System

| Key | Asset | Range | Default | Description |
|-----|-------|-------|---------|-------------|
| 1 | Fire Tick This Frame | 0..2B | 0 | Internal: fire DoT tick signal |

---

## Events (ConditionEventObject) — 148 total

Frame-triggered conditions via ConditionEventWriter. Auto-reset each frame.

### Input & Movement

| Key | Asset | Description |
|-----|-------|-------------|
| 25 | OnMoveInput | Directional movement triggered |
| 56 | OnCombatStateChanged | Entered/exited combat (sprint speed changes) |
| 26 | OnDashInitiated | Dash (Shift) triggered. 5s cooldown, costs 1 augment stack |
| 39 | OnDashCompleted | Dash end. Evaluates OnDashThroughTornado |
| 32 | OnDashThroughTornado | Dash through tornado: refunds charge or increases distance |
| 60 | OnHookLaunched | Q ability hook fired |
| 49 | OnHookAttached | Hook attached 1s: pull Arvex or heavy enemy |
| 8 | OnWeaponThrown | Q4 ability: weapon throw |
| 14 | OnTeleportToWeapon | RMB while weapon thrown: teleport to weapon |

### Core Combat

| Key | Asset | Description |
|-----|-------|-------------|
| 4 | OnLightAttack | LMB attack animation, hitbox, Sunder stacks |
| 62 | OnHeavyAttack | RMB attack animation, hitbox, Sunder stacks |
| 121 | OnMediumAttack | RMB1/RMB2 base attacks |
| 130 | OnChargeAttackInitiated | Holding LMB charge start |
| 142 | OnChargeAttackReleased | Crescent Attack execution |
| 133 | OnBillionSlashTriggered | RMBH flurry attack |
| 100 | OnGrabInitiated | F grab state start |
| 94 | OnGrabCompleted | Grab/throw success |
| 23 | OnComboIncrement | Consecutive hit tracked. >8 hits: +5% knockback/hit |
| 2 | OnComboBroken | Combo broken: took damage (unless Armored) or target recovered |
| 145 | OnCriticalHit | Critical damage dealt (6 ways to crit, Marked mechanic) |
| 144 | OnJuggleInitiated | Enemy knocked airborne, juggle state begins |
| 143 | OnEnemyOTG | Enemy hit while Off The Ground |
| 98 | OnEnemyStunned | Stun from full chill/freeze or shield break |
| 128 | OnCounterStrikeAvailable | Earth armored hit or post-parry: counter window opens |
| 126 | OnSlowMotionTriggered | Perfect parry/dodge slowdown (scales 1-100% by weight) |
| 111 | OnComboPointGained | Successful parry/dash awards combo point |

### Health, Armor & Defense

| Key | Asset | Description |
|-----|-------|-------------|
| 45 | OnDamageTaken | Core damage calc: actual vs Provisional (2/3 actual). Armored: 3x |
| 7 | OnArmorHit | Attacked while Armored: no flinch, opens counterstrike |
| 104 | OnProvisionalHealthGained | Provisional HP gained |
| 119 | OnProvisionalHealthConsumed | Provisional HP consumed on 2nd hit |
| 127 | OnArmorStackGained | Armor stack added |
| 108 | OnArmorStackConsumed | Armor stack consumed to prevent flinch |
| 92 | OnShieldDamaged | Shield took damage |
| 139 | OnShieldBroken | Shield depleted to 0 |
| 95 | OnShieldDecayTick | Shield 1%/sec decay tick |
| 58 | OnHealReceived | HP restored (actual or provisional). Green glow |
| 101 | OnRageModeEntered | Rage/Soulforged Pact activation |
| 105 | OnRageModeExited | Rage mode ended |

### Parry & Dodge

| Key | Asset | Description |
|-----|-------|-------------|
| 54 | OnParryTriggered | Block/parry frames active |
| 40 | OnPerfectParry | Slow-motion (scales by weight) + 1 combo point |
| 27 | OnDodgePerfect | Slow-motion + counter-attack window |

### Stagger & Execute

| Key | Asset | Description |
|-----|-------|-------------|
| 3 | OnStaggerApplied | Stagger damage added. Resets 5s no-hit (fade 20%/sec) |
| 48 | OnStaggerBroken | Stagger full: enemy stunned, vulnerable to execution |
| 46 | OnExecutionTriggered | F on staggered/downed enemy. Camera pan, teleport, fixed+combo damage |
| 53 | OnExecutionPrompt | Skull UI flash when enemy in execution threshold |

### Combo & Overkill

| Key | Asset | Description |
|-----|-------|-------------|
| 20 | OnOverkillTriggered | Crit damage beyond kill threshold: bonus soul drops |
| 59 | OnWallSplat | Enemy knocked into wall: freeze 0.12s, bounce |

### Elemental & Augment

| Key | Asset | Description |
|-----|-------|-------------|
| 18 | OnElementStackApplied | Any element stack added (Blaze/Chill/Retribution/Gust/Resolve/Shade). Refreshes 3s duration |
| 65 | OnClimaxReached | Element hits 5 stacks (or 10 Earth) |
| 12 | OnEarthClimaxLevelTwo | 10 Earth stacks: impale spike on juggled enemies |
| 24 | OnSynergyTriggered | Two elemental conditions meet (Magmatic Impale, Wildfire, Steam Discharge) |
| 28 | OnShatter | Hit Frozen enemy with Shatter keyword: 250% damage |
| 124 | OnBlazeExplosion | Fire climax explosion |
| 97 | OnGasCloudSpawned | Fire aftermath gas cloud |
| 117 | OnTargetRooted | Frost 4-stack root |
| 140 | OnTargetFrozen | Frost climax freeze |
| 113 | OnImpaleSpawned | Earth climax impale pillar |
| 107 | OnImpaleBroken | Impale shattered into debris |
| 103 | OnLightTetherCreated | Wrath/Bouncers light tether |
| 147 | OnLightTetherBroken | Light tether broken |
| 35 | OnCloneSpawned | Shadow clone spawned |
| 17 | OnCloneExpired | Shadow clone expired (4s or Ashen Wraith explosion) |
| 106 | OnCloneAttack | Shadow clone autonomous attack |
| 33 | OnChainLightningBounce | Lightning bounce between enemies/pillars/clones |

### Debuffs & Status

| Key | Asset | Description |
|-----|-------|-------------|
| 129 | OnDebuffApplied | Generic debuff applied |
| 110 | OnDebuffExpired | Debuff timer expired |
| 135 | OnMarkApplied | Mark: next attack is crit |
| 114 | OnMarkConsumed | Mark consumed for guaranteed crit |
| 134 | OnBleedTick | Bleed tick: 20% damage every 5 seconds |

### Enemy AI & Behavior

| Key | Asset | Description |
|-----|-------|-------------|
| 43 | OnEnemySpawn | Spawn: evaluate weight class, assign behavior tokens |
| 13 | OnTargetAcquired | AI pathing start (Bruiser rush, Flanker strafe, Ranged kite) |
| 9 | OnDoomTimerThresholdReached | Timer triggers Nimble or Berserk properties |
| 41 | OnEnemyHealthLow | Phase 2 mechanics (Fire berserk, boss ultimate) |
| 21 | OnEnemyDeath | Post-death effects (fire explode, ghoul consume) |
| 42 | OnNimbleRecovery | Agile enemy: roll on knockdown, wall-bounce lunge |
| 52 | OnBreakawayTriggered | Boss/enemy escapes infinite combo (HP threshold/ICD) |
| 137 | OnBossPhaseChanged | Boss phase transition |
| 136 | OnEnemyBerserkEntered | Berserk state (doom timer or low HP) |
| 141 | OnEnemyNimbleRoll | Nimble property dodge/recovery |
| 123 | OnEnemyBarrierDeployed | Bruiser barrier shield |
| 112 | OnEnemyTeleport | Flanker/Shadow evasive teleport |

### Leveling, Progression & Base

| Key | Asset | Description |
|-----|-------|-------------|
| 44 | OnRunStarted | Run begin |
| 57 | OnRunEnded | Soul Harvest summary, Performance Rank (S/A/B/C), unlocks |
| 5 | OnNodeConquered | Map mission complete. Partial steals/downgrades nearby nodes |
| 6 | OnAugmentForged | Spend gems/gold: upgrade augment rarity |
| 47 | OnAugmentFused | 3 same-rarity → 1 higher rarity |
| 29 | OnShrineInteracted | Upgrade augment + 20% corruption chance |
| 31 | OnAugmentCorrupted | Augment gains debuff/mana cost |
| 81 | OnPardehUpTriggered | Tavern massive cooldown (requires boss defeat) |
| 30 | OnOverchargeSacrificed | Sacrifice HP/Mana for instant Mythic upgrade |
| 66 | OnFlawlessRoomClear | No damage taken: auto-upgrade random augment |
| 67 | OnBaseDayPassed | Day cycle: fatigue recovery, crafting completion |
| 69 | OnPlayerLevelUp | Level up: static stat boosts (Damage+, HP+, Memory, Talent Points) |

### Base Construction & Power

| Key | Asset | Description |
|-----|-------|-------------|
| 89 | OnBuildingConstructed | Salvage → new structure on platform |
| 88 | OnBuildingUpgraded | Intel/Salvage → building tier upgrade (Skill 1→2→3) |
| 87 | OnPlatformUnlocked | Bridge built with Salvage: new platform + random reward |
| 68 | OnPowerAllocated | Power Tower assigns Generated Power to adjacent building |
| 79 | OnMaterialRefined | Raw → Refined materials (1 checkpoint/day) |

### Building Interactions

| Key | Asset | Description |
|-----|-------|-------------|
| 75 | OnGearCrafted | Forge: Refined Minerals + Gems → gear |
| 76 | OnGearUpgraded | Forge gear upgrade |
| 71 | OnGadgetUpgraded | Elementis Engineer: hook upgrade etc. |
| 86 | OnIntelGathered | Daily Tavern bartender intel |
| 84 | OnHealingGenerated | Infirmary/Healing Springs: 25/50 passive healing |
| 85 | OnAugmentBanished | Nexus of Fates: spend Memories to remove augment from loot pool |
| 77 | OnAugmentChanceControlled | Nexus of Fates: manipulate RNG weight |
| 82 | OnRuneShrineInteracted | Rune Shrine: augment purification/corruption |

### Strategic Map & Scryers' Spire

| Key | Asset | Description |
|-----|-------|-------------|
| 78 | OnRitualSabotaged | Intel: stop ritual (would increase enemy difficulty) |
| 70 | OnEnemyAdvanceStopped | Intel: freeze enemy map movement |
| 83 | OnLegionFrozen | Intel: freeze legion on map |
| 74 | OnNodeRewardRevealed | Scryers' Spire: reveal hidden map rewards |

### Companions

| Key | Asset | Description |
|-----|-------|-------------|
| 63 | OnCompanionDispatched | Send companion on base mission |
| 37 | OnCompanionSummoned | Q2/Q3: companion combat assist (Static Clone Burst) |
| 148 | OnCompanionReturned | Dispatch completed |
| 11 | OnCompanionFatigueChanged | Fatigue bar update |
| 50 | OnCompanionCollapse | Fatigue 100%: collapse (or Phantom State if Driftworn) |
| 38 | OnCompanionFriendshipChanged | Friendship spectrum shift (-3 to +3) |
| 120 | OnCompanionMoraleChanged | Morale update (Neurotic trait) |
| 36 | OnTraitDeveloped | Random positive/negative/mixed trait assigned |
| 102 | OnCompanionTraitAdded | Trait gained (injury or random) |
| 93 | OnCompanionTraitRemoved | Trait cleansed/purified |
| 96 | OnCompanionHealed | Companion healed |
| 132 | OnCompanionPassiveTriggered | Passive ability fires (One-Man Army etc.) |
| 10 | OnFatalBlowIntercepted | Companion takes fatal hit for Arvex: 2s i-frames |

### Items & Loot

| Key | Asset | Description |
|-----|-------|-------------|
| 72 | OnLootCollected | Picked up currencies, augment cards, power shards |
| 80 | OnMemoryConsolidated | Gaseous area → Memory shard |
| 73 | OnShopPurchase | Buy augments/items/rerolls with Dreamshards |
| 146 | OnItemUsed | Combat/health potion used |
| 91 | OnItemChargesChanged | Item charge count changed |
| 90 | OnElixirCrafted | Alchemist: elixir crafted |
| 99 | OnSoulHarvested | Soul collected (elite/corrupted/boss) |
| 131 | OnChestOpened | Chest opened |
| 115 | OnTrapUpgraded | Engineering building: trap upgrade |
| 109 | OnGeneratedPowerTick | Power Tower: unmanned automation resource tick |
| 138 | OnMemoryConverted | Gaseous memory → Intel at Scryers' Spire |

### Quest, Decision & UI

| Key | Asset | Description |
|-----|-------|-------------|
| 34 | OnDialogueChoiceSelected | Shift god/faction influence, trigger traits (Hollow Vow/Silent Loyalty) |
| 61 | OnRoomRewardRerolled | Arvex door reroll |
| 19 | OnInfluenceShifted | Deity Favor Bar adjustment |
| 64 | OnLoreDiscovered | Pause Menu Lore Log entry unlock |
| 125 | OnTalentUnlocked | Talent point spent |
| 122 | OnTalentUpgraded | Talent upgraded |

### Spawning, Hazards & World

| Key | Asset | Description |
|-----|-------|-------------|
| 55 | OnHazardTriggered | Spikes, trains, blades, arrow switches |
| 30 | OnBarrelDestroyed | Barrel type: Fire=explosion, Wind=cyclone, Lightning=chain |
| 22 | OnEnvironmentalPuzzleSolved | Torch order etc. → hidden chest |
| 51 | OnDestructibleWallBroken | Hidden pathway for loot/lore |
| 118 | OnBiomeEntered | Entered new biome |
| 116 | OnBiomeCleared | Biome cleared |
