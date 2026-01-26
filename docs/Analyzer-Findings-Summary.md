# Analyzer Findings Summary

**Build:** `FunWasHad.sln` — Debug, AnalysisLevel `9.0-all`  
**Date:** 2026-01-28  
**Source:** `dotnet build -c Debug --no-incremental` (output in `analyzer-build.txt`)

---

## Build Result

| | Count |
|---|:---:|
| **Errors** | 0 |
| **Warnings** | 1,880 |

**Build succeeded.** All warnings are from code analyzers (CA rules) and the compiler. `TreatWarningsAsErrors` is `false`, so the solution builds successfully. The counts by rule and by project below were parsed from the same log.

---

## Findings by Rule (CA)

| Count | Rule | Brief description |
|:---:|:---|:---|
| 1,020 | **CA1848** | Use `LoggerMessage` source generators instead of `ILogger` extension methods (LogInformation, LogDebug, etc.) |
| 796 | **CA2007** | Call `ConfigureAwait(false)` (or `true` where needed) on awaited tasks |
| 506 | **CA1707** | Identifiers should not contain underscores (e.g. test method names like `Method_Scenario_Expected`) |
| 242 | **CA1031** | Do not catch generic `Exception`; catch a more specific type or rethrow |
| 112 | **CA1062** | Validate public API parameters are non-null before use; throw `ArgumentNullException` when null |
| 106 | **CA1307** | Use `string.Contains(string, StringComparison)` for clarity |
| 102 | **CA1515** | Types in application assemblies can be made internal (not referenced from outside) |
| 80 | **CA2234** | Prefer `HttpClient.GetAsync(Uri)` (and similar) over string overloads |
| 58 | **CA1056** | URI-like properties should be `System.Uri` instead of `string` |
| 56 | **CA2000** | Ensure `IDisposable` is disposed before all references go out of scope |
| 46 | **CA1849** | Prefer `WriteLineAsync` over `WriteLine` to avoid blocking |
| 46 | **CA1852** | Seal internal/private types that have virtual members |
| 34 | **CA2227** | Collection properties should be read-only (remove setter or use init) |
| 34 | **CA1310** | Specify `StringComparison` for correctness |
| 32 | **CA1002** | Use `Collection<T>`, `ReadOnlyCollection<T>`, or `KeyedCollection` instead of `List<T>` for public members |
| 30 | **CA1822** | Mark members as `static` when they do not access instance data |
| 30 | **CA1305** | Use `ToString(IFormatProvider)` or culture-safe overloads |
| 30 | **CA1861** | Prefer `static readonly` for array arguments passed repeatedly |
| 30 | **CA1860** | Prefer `Count > 0` over `Any()` for collections |
| 30 | **CA1510** | Use `ArgumentNullException.ThrowIfNull(param)` instead of manually throwing |
| 28 | **CA1063** | Implement `IDisposable` correctly (Dispose pattern) |
| 22 | **CA1812** | Instantiate or remove internal class that is never instantiated |
| 18 | **CA1003** | Use `EventHandler<T>` (or similar) for generic event handlers |
| 18 | **CA1051** | Do not declare visible instance fields; use properties |
| 16 | **CA1819** | Properties should not return arrays; use collections or `ReadOnlySpan` where appropriate |
| 16 | **CA1826** | Use `Index` and `Range` or property instead of `ElementAt` pattern where applicable |
| 14 | **CA1805** | Do not initialize explicitly to default value |
| 14 | **CA1311** | Specify a culture or use invariant overload for string operations |
| 14 | **CA1816** | Call `GC.SuppressFinalize` in `Dispose` when using finalizer |
| 14 | **CA1866** | Use `IndexOf` / `Contains` with `char` instead of single-char string when appropriate |
| 14 | **CA1304** | Use culture-safe overloads for `ToLower` / `ToUpper` |
| 14 | **CA1708** | Identifiers should differ by more than case |
| 12 | **CA1308** | Use `ToUpperInvariant` instead of `ToLowerInvariant` for normalization |
| 12 | **CA1054** | URI parameters should not be strings |
| 10 | **CA1845** | Use `Span`-based overloads where available |
| 10 | **CA1034** | Nested types should not be visible |
| 8 | **CA1847** | Use `string.Contains(char)` instead of `string.Contains(string)` for single character |
| 8 | **CA1854** | Prefer `Dictionary.TryGetValue` to avoid double lookup |
| 8 | **CA2263** | Prefer `string` / `ReadOnlySpan` in attribute arguments |
| 8 | **CA2100** | Review SQL query for security (e.g. injection) |
| 6 | **CA1859** | Use concrete types to improve performance |
| 6 | **CA2213** | Dispose `IDisposable` fields in `Dispose` |
| 6 | **CA1032** | Implement standard exception constructors |
| 4 | **CA1721** | Property names should not match `get_` methods |
| 4 | **CA1850** | Prefer `static abstract` / `static virtual` where applicable |
| 4 | **CA2254** | Template/format string should be a static constant |
| 4 | **CA1055** | URI return values should not be strings |
| 4 | **CA1724** | Type names should not match namespaces |
| 2 | **CA2201** | Do not raise `ReservedException` types |
| 2 | **CA2016** | Forward cancellation token to called methods |
| 2 | **CA1001** | Types that own `IDisposable` should implement `IDisposable` |
| 2 | **CA1725** | Parameter names should match base declaration |
| 2 | **CA1716** | Identifiers should not match keywords |
| 2 | **CA1512** | Use `ArgumentOutOfRangeException.ThrowIf…` helpers |
| 2 | **CA1309** | Use ordinal `StringComparison` |
| 2 | **CA1040** | Avoid empty interfaces |
| 2 | **CA1030** | Use appropriate event-raising methods |
| 2 | **CA1024** | Use properties where appropriate |
| 2 | **CA1862** | Prefer `Length` / `Count` property when available |
| 2 | **CA5394** | Do not disable security analyzers |

---

## Findings by Project

| Count | Project |
|:---:|:---|
| 752 | `src/FWH.Mobile/FWH.Mobile` |
| 390 | `src/FWH.MarketingApi` |
| 334 | `tests/FWH.Common.Workflow.Tests` |
| 310 | `src/FWH.Common.Workflow` |
| 260 | `src/FWH.Mobile.Data` |
| 248 | `tests/FWH.MarketingApi.Tests` |
| 188 | `src/FWH.Location.Api` |
| 186 | `src/FWH.Orchestrix.Mediator.Remote` |
| 176 | `src/FWH.Common.Location` |
| 170 | `src/FWH.Common.Chat` |
| 150 | `src/FWH.Documentation.Sync` |
| 144 | `tests/FWH.Common.Chat.Tests` |
| 142 | `tests/PlantUmlRender.Tests` |
| 108 | `src/FWH.Mobile/FWH.Mobile.Android` |
| 58 | `tests/FWH.Mobile.Services.Tests` |
| 40 | `tests/FWH.Mobile.Data.Tests` |
| 40 | `tools/PlantUmlRender` |
| 36 | `src/FWH.Orchestrix.Contracts` |
| 26 | `src/FWH.Mobile/FWH.Mobile.Desktop` |
| 20 | `tests/FWH.Common.Imaging.Tests` |
| 20 | `tests/FWH.Common.Location.Tests` |
| 10 | `src/FWH.ServiceDefaults` |
| 8 | `src/FWH.Common.Imaging` |

---

## Suggested Remediation Order

1. **High impact, often straightforward**
   - **CA1062** (null validation): Add `ArgumentNullException.ThrowIfNull` or explicit checks at public API boundaries.
   - **CA1031** (generic catch): Catch specific exceptions or rethrow; avoid swallowing `Exception`.
   - **CA2000** / **CA2213** (disposal): Fix `IDisposable` usage and `Dispose` implementation.

2. **Medium effort, consistency**
   - **CA2007** (ConfigureAwait): Suppressed globally in `.editorconfig` (`dotnet_diagnostics.CA2007.severity = none`). `FWH.Mobile.Data` and `tools/PlantUmlRender` were updated to use `.ConfigureAwait(false)` before suppression. For new code in libraries and I/O, prefer `.ConfigureAwait(false)`; in UI or test methods, omit or use `.ConfigureAwait(true)` (xUnit1030 prefers no ConfigureAwait(false) in tests).
   - **CA1307** / **CA1310** / **CA1311** (string/culture): Use `StringComparison.Ordinal` or `InvariantCulture` where intent is not locale-dependent.

3. **Lower priority or optional**
   - **CA1848** (LoggerMessage): Improves logging performance; can be done incrementally.
   - **CA1707** (underscores): Suppressed globally in `.editorconfig` (`dotnet_diagnostics.CA1707.severity = none`).
   - **CA1056** / **CA2234** (URI): Improve type safety where URLs are central; can be deferred.

4. **Tuning via .editorconfig**
   - To **suppress** a rule (e.g. CA1707 in tests):  
     `dotnet_diagnostics.CA1707.severity = none`
   - To **promote** to error (e.g. CA1062):  
     `dotnet_diagnostics.CA1062.severity = error`

---

## Raw Build Output

Full build log: `analyzer-build.txt` (in repo root). To regenerate:

```powershell
dotnet build FunWasHad.sln -c Debug --no-incremental 2>&1 | Out-File -FilePath analyzer-build.txt -Encoding utf8
```
