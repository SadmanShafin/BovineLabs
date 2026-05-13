# Debug Logging Reference

<!-- Shattered Debug Logging reference -->

Use BovineLabs logging APIs by context. Do not add `Debug.Log*` in project/package code.

## Context Rule

- In ECS systems (`ISystem`, `SystemBase`, `OnUpdate` paths), obtain logger from singleton:
  - `var logger = SystemAPI.GetSingleton<BLLogger>();`
  - Log via that `logger` instance.
- Outside ECS systems (authoring, bakers, static helpers, editor utilities, setup/boot code), use static `BLGlobalLogger`.

## Severity Mapping

- `Debug.Log(...)` -> `logger.LogInfoString(...)` or `BLGlobalLogger.LogInfoString(...)`
- `Debug.LogWarning(...)` -> `logger.LogWarningString(...)` or `BLGlobalLogger.LogWarningString(...)`
- `Debug.LogError(...)` -> `logger.LogErrorString(...)` or `BLGlobalLogger.LogErrorString(...)`
- `Debug.LogException(ex)` -> `BLGlobalLogger.LogFatal(ex)` (or log stringified exception if `BLLogger` instance is the only available logger)

## System Example

```csharp
var logger = SystemAPI.GetSingleton<BLLogger>();

logger.LogDebugString($"Input enabled {input}");
logger.LogWarningString($"Missing state for {entity}");
logger.LogErrorString("Input asset not setup");
```

## Non-System Example

```csharp
BLGlobalLogger.LogInfoString($"Loaded {count} settings");
BLGlobalLogger.LogWarningString($"Missing type on {authoring.gameObject.name}");
BLGlobalLogger.LogErrorString("Failed to decompress baked");
```

## Practical Notes

- `LogDebug*` methods are conditional (`UNITY_EDITOR` + `BL_DEBUG`), so use info/warning/error levels for always-on diagnostics.
- Prefer `*String` overloads for interpolated strings. Use fixed-size overloads (`LogInfo`, `LogInfo512`, etc.) only when data is already in `FixedString*`.

## Audit Command

Use this to find direct Unity logging in workspace code:

```powershell
rg -n "Debug\.Log(Exception|Warning|Error)?\(" Assets Packages/com.bovinelabs.* -g "!TestRunner/**"
```
