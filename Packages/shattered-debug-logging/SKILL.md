---
name: shattered-debug-logging
description: "Use when adding, changing, or reviewing logging in this workspace, including migrating `Debug.Log*` calls to BovineLabs logging and selecting the ECS singleton logger in systems versus `BLGlobalLogger` outside systems."
---

# Shattered Debug Logging

Use this skill for runtime/editor logging in Shattered code.

## Workflow

1. Read `references/debug-logging.md`.
2. Classify logging context first:
   - In ECS systems, acquire `BLLogger` with `SystemAPI.GetSingleton<BLLogger>()`.
   - Outside ECS systems, use static `BLGlobalLogger`.
3. Replace `Debug.Log*` calls with BovineLabs logger APIs while preserving severity.
4. Prefer `*String` overloads when the message originates as `string`; use fixed-string overloads when code already works with `FixedString*`.

## Routing

- `references/debug-logging.md`: context rules, severity mapping, examples, and quick audit command.
