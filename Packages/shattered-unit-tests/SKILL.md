---
name: shattered-unit-tests
description: "Use when creating or refactoring unit tests in Shattered, including plain NUnit vs ECSTestsFixture decisions and *.Tests.asmdef setup."
---

# Shattered Unit Tests

Use this skill for unit-test authoring and test asmdef setup in Shattered packages, with editor tests as the default path.

## Workflow

1. Default to plain NUnit for pure logic/data tests.
2. Use `ECSTestsFixture` only when the test needs ECS world/entity/system data.
3. Read `references/unit-tests.md` when creating/updating a `*.Tests.asmdef`, adding `AssemblyInfo.cs`, or needing templates/examples.

## Hard Rules

- Default package tests to Editor assemblies.
- Do not use `ECSTestsFixture` for pure math/function/algorithm, collection, or Burst/job tests that do not touch ECS world/entity state.
- For asmdef or `AssemblyInfo.cs` work, follow the reference checklist; preserve existing attributes and intentional narrower assemblies.

## Routing

- `references/unit-tests.md`: fixture decision rules, test templates, and asmdef setup patterns validated against existing `com.bovinelabs.*.Tests.asmdef` files.
