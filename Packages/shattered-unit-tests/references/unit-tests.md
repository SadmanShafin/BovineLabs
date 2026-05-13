# Unit Tests Reference

Use this reference when writing or refactoring unit tests in BovineLabs packages.

## Read Order

1. `Packages/com.bovinelabs.core/BovineLabs.Testing/ECSTestsFixture.cs`
2. Example with ECS fixture: `Packages/com.bovinelabs.core/BovineLabs.Core.Tests/Utility/EntityLockTests.cs`
3. Example without ECS fixture: `Packages/com.bovinelabs.core/BovineLabs.Core.Tests/Utility/PooledNativeListTests.cs`
4. Existing test asmdefs listed in `Asmdef Pattern Matrix`.

## Choose Test Base

Default to plain NUnit test classes.  
Use `ECSTestsFixture` only when tests require Unity ECS world data.

Default test assembly target: Editor (`includePlatforms: ["Editor"]`).
PlayMode/runtime test assemblies are uncommon in this repository right now.

Use `ECSTestsFixture` when the test needs one or more of:

- `World`, `WorldUnmanaged`, or `EntityManager`
- creating entities/archetypes/components in a test world
- creating or updating ECS systems (`this.World.CreateSystem<T>()`, `system.Update(...)`)
- `BlobAssetStore` setup from fixture
- player loop/default world reset behavior from fixture setup

Do not use `ECSTestsFixture` for:

- pure math/function/algorithm tests
- collection tests that do not touch ECS world data
- Burst/job tests that only use native containers and no world/entity state

## Test Templates

### Plain NUnit Template (preferred default)

```csharp
namespace BovineLabs.SomePackage.Tests
{
    using NUnit.Framework;

    public class ExampleTests
    {
        [Test]
        public void DoesWork()
        {
            // Arrange
            var value = 2;

            // Act
            var result = value + 3;

            // Assert
            Assert.AreEqual(5, result);
        }
    }
}
```

### ECS Fixture Template (only for Unity ECS data access)

```csharp
namespace BovineLabs.SomePackage.Tests
{
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    public class ExampleEcsTests : ECSTestsFixture
    {
        [Test]
        public void SystemWritesExpectedComponent()
        {
            var archetype = this.Manager.CreateArchetype(typeof(TestData));
            using var entities = this.Manager.CreateEntity(archetype, 1, Allocator.Temp);

            var system = this.World.CreateSystem<ExampleSystem>();
            system.Update(this.WorldUnmanaged);

            var value = this.Manager.GetComponentData<TestData>(entities[0]).Value;
            Assert.AreEqual(1, value);
        }

        private struct TestData : IComponentData
        {
            public int Value;
        }
    }
}
```

## Asmdef Setup Checklist

For package unit tests (`<Package>.Tests.asmdef`), defaulting to editor tests:

1. Place asmdef in package test assembly folder (for example `BovineLabs.<Package>.Tests/`).
2. Set `"name"` to `<Package>.Tests`.
3. Set `"includePlatforms": ["Editor"]` for edit-mode tests.
4. Set `"overrideReferences": true` (required baseline).
5. Set `"autoReferenced": false`.
6. Set `"allowUnsafeCode": true` (matches existing test assemblies).
7. Keep `"noEngineReferences": false`.
8. Add `UnityEngine.TestRunner` and `UnityEditor.TestRunner` to `"references"`.
9. Add `"precompiledReferences": ["nunit.framework.dll"]`.
10. Add `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`.
11. Add or update `AssemblyInfo.cs` in the same test assembly folder with `using Unity.Entities;` and `[assembly: DisableAutoCreation]`.
12. If `AssemblyInfo.cs` already exists, keep existing attributes (for example `InternalsVisibleTo`) and append `[assembly: DisableAutoCreation]`.
13. For new package test assemblies, use this default test reference baseline in `"references"`:
    - `"BovineLabs.Core"`
    - `"BovineLabs.Testing"`
    - `"Unity.Burst"`
    - `"Unity.Collections"`
    - `"Unity.Entities"`
    - `"Unity.Mathematics"`
14. If `"Unity.Entities"` is present, `"Unity.Collections"` must also be present (required rule for this repo; baseline already satisfies this).
15. Existing assemblies may keep intentional narrower references, but new assemblies should start from the default baseline.
16. Add package/data assemblies under test explicitly on top of the baseline (minimum extras only).
17. Use `"versionDefines": []` unless a package gate is explicitly required.

### Editor ECS Test Asmdef Template (current baseline)

```json
{
  "name": "BovineLabs.Example.Tests",
  "rootNamespace": "",
  "references": [
    "BovineLabs.Core",
    "BovineLabs.Example",
    "BovineLabs.Example.Data",
    "BovineLabs.Testing",
    "Unity.Burst",
    "Unity.Collections",
    "Unity.Entities",
    "Unity.Mathematics",
    "Unity.Transforms",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "allowUnsafeCode": true,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false
}
```

## Test AssemblyInfo Template

```csharp
// <copyright file="AssemblyInfo.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using Unity.Entities;

[assembly: DisableAutoCreation]
```

## Asmdef Pattern Matrix

Find current examples with `rg --files -g "*.Tests.asmdef" Assets/Scripts Packages`.
