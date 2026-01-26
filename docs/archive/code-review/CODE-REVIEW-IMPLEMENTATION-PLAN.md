# Code Review Implementation Plan

Implementation plan for all findings in [CODE-REVIEW-AGGREGATED.md](./CODE-REVIEW-AGGREGATED.md). Items are grouped by phase, component, and priority. IDs use the form **CR-{Component}-{ReviewID}** (e.g. CR-EXT-1.1.1).

**Components:** EXT = extensions/fwh-cli-agent, PSM = scripts/modules/FWH.Prompts, PUM = tools/PlantUmlRender, CFG = cli-agent.json. *FWH.CLI.Agent and CR-CLI are omitted (removed MVP-SUPPORT-007).*

---

## Agent Assignments & Parallel Execution

### Principles

- **One agent per component** (EXT, PSM, PUM, CFG). Each agent may edit only the paths in its roster. This avoids merge conflicts and allows parallel work.
- **Respect dependency order** within an agent’s batch: e.g. CR-PSM-2.1.1 before CR-PSM-2.1.3.
- **Phases are sequential** (1 → 2 → 3 → 4 → 5). An agent implements its Phase N items only after Phase N−1 is done (or in one run, in that order).
- **Agent CFG** has work only in Phase 4 (schema + doc). It can run in parallel with EXT, PSM, PUM in Phase 4.

### Agent Roster

| Agent | Component | Paths (allowed to edit) |
|-------|-----------|--------------------------|
| **Agent EXT** | extensions/fwh-cli-agent | `extensions/fwh-cli-agent/**` (src/extension.ts, package.json, tsconfig, etc.) |
| **Agent PSM** | scripts/modules/FWH.Prompts | `scripts/modules/FWH.Prompts/**` (FWH.Prompts.psm1, .psd1, FWH.Prompts.Tests.ps1, etc.) |
| **Agent PUM** | tools/PlantUmlRender | `tools/PlantUmlRender/**`, `tests/PlantUmlRender.Tests/**` (create if needed) |
| **Agent CFG** | cli-agent config & schema | `cli-agent.schema.json` (repo root), `docs/Project/cli-agent-config.md` (new or existing), `cli-agent.json` (example only; do not change project’s real config) |

### Phase × Agent Matrix

| Phase | Agent EXT | Agent PSM | Agent PUM | Agent CFG |
|-------|-----------|-----------|-----------|-----------|-----------|
| **1** | 1.1.1, 1.1.3, 1.2.5 | 2.1.1 → 2.1.3, 2.2.3 | 4.1.1 | — |
| **2** | 1.2.1, 1.2.2, 1.2.3, 1.2.4 | 2.2.1, 2.2.2, 2.2.4, 2.2.5 | 4.2.1, 4.2.2, 4.2.3, 4.2.4 | — |
| **3** | 1.3.1, 1.3.2 | 2.3.1, 2.3.2 | 4.3.1 | — |
| **4** | 1.4.1, 1.4.2, 1.4.3 | 2.1.2, 2.4.1, 2.4.2, 2.4.3, 2.4.4 | 4.1.2, 4.4.1, 4.4.2, 4.4.3 | **5.1.1** |
| **5** | 1.5.1, 1.5.2 | 2.5.1, 2.5.2, 2.5.3 | 4.5.1 | — |

*(→ means “after”: e.g. 2.1.1 → 2.1.3 = do 2.1.1 first.)*

---

### Preparing Agents: Shared Context (give to every agent)

Provide this once to each agent before the agent-specific prompt:

```
You are implementing code review remediation for the FunWasHad repo. Work only in your assigned paths. Do not edit files in other agents’ paths.

References:
- [CODE-REVIEW-AGGREGATED.md](./CODE-REVIEW-AGGREGATED.md) — full findings and actions
- [CODE-REVIEW-IMPLEMENTATION-PLAN.md](./CODE-REVIEW-IMPLEMENTATION-PLAN.md) — phases, IDs, and agent matrix

For each CR-{Component}-{ID}, the Action column in the plan (and the matching review section) is the source of truth. After editing, run the relevant build/test for your component (e.g. `npm run compile` in the extension, `Invoke-Pester` for PSM, `dotnet build`/`dotnet test` for PUM).
```

---

### Agent Prompt: Agent EXT

*Copy-paste this into a new agent/chat. Use a fresh workspace or branch so EXT changes do not conflict with PSM/CLI/PUM/CFG.*

```
You are **Agent EXT**. Implement all code review items for **extensions/fwh-cli-agent** in Phases 1–5, in order. You may edit only under `extensions/fwh-cli-agent/`.

**Your CR- IDs in order (respect →):**
- Phase 1: CR-EXT-1.1.1, CR-EXT-1.1.3, CR-EXT-1.2.5
- Phase 2: CR-EXT-1.2.1, CR-EXT-1.2.2, CR-EXT-1.2.3, CR-EXT-1.2.4
- Phase 3: CR-EXT-1.3.1, CR-EXT-1.3.2
- Phase 4: CR-EXT-1.4.1, CR-EXT-1.4.2, CR-EXT-1.4.3
- Phase 5: CR-EXT-1.5.1, CR-EXT-1.5.2

**Do not edit:** `scripts/`, `tools/PlantUmlRender/`, `cli-agent.schema.json`, `docs/Project/cli-agent-config.md`, or any path outside `extensions/fwh-cli-agent/`.

Use CODE-REVIEW-AGGREGATED.md and CODE-REVIEW-IMPLEMENTATION-PLAN.md for the exact action for each ID. After implementation, run `npm run compile` in `extensions/fwh-cli-agent` and fix any errors. If you add tests (Phase 5), run the test script you set up.
```

---

### Agent Prompt: Agent PSM

*Copy-paste this into a new agent/chat.*

```
You are **Agent PSM**. Implement all code review items for **scripts/modules/FWH.Prompts** in Phases 1–5, in order. You may edit only under `scripts/modules/FWH.Prompts/`.

**Your CR- IDs in order (respect →):**
- Phase 1: CR-PSM-2.1.1, then CR-PSM-2.1.3, then CR-PSM-2.2.3
- Phase 2: CR-PSM-2.2.1, CR-PSM-2.2.2, CR-PSM-2.2.4, CR-PSM-2.2.5
- Phase 3: CR-PSM-2.3.1, CR-PSM-2.3.2
- Phase 4: CR-PSM-2.1.2, CR-PSM-2.4.1, CR-PSM-2.4.2, CR-PSM-2.4.3, CR-PSM-2.4.4
- Phase 5: CR-PSM-2.5.1, CR-PSM-2.5.2, CR-PSM-2.5.3

**Do not edit:** `extensions/`, `tools/PlantUmlRender/`, `cli-agent.schema.json`, `docs/Project/cli-agent-config.md`, or any path outside `scripts/modules/FWH.Prompts/`.

Use CODE-REVIEW-AGGREGATED.md and CODE-REVIEW-IMPLEMENTATION-PLAN.md for the exact action for each ID. After implementation, run `Invoke-Pester` on `FWH.Prompts.Tests.ps1` (and any new tests) and fix failures.
```

---

### Agent Prompt: Agent PUM

*Copy-paste this into a new agent/chat.*

```
You are **Agent PUM**. Implement all code review items for **tools/PlantUmlRender** in Phases 1–5, in order. You may edit only `tools/PlantUmlRender/` and create/edit `tests/PlantUmlRender.Tests/` if needed.

**Your CR- IDs in order:**
- Phase 1: CR-PUM-4.1.1
- Phase 2: CR-PUM-4.2.1, CR-PUM-4.2.2, CR-PUM-4.2.3, CR-PUM-4.2.4
- Phase 3: CR-PUM-4.3.1
- Phase 4: CR-PUM-4.1.2, CR-PUM-4.4.1, CR-PUM-4.4.2, CR-PUM-4.4.3
- Phase 5: CR-PUM-4.5.1

**Do not edit:** `extensions/`, `scripts/modules/FWH.Prompts/`, `cli-agent.schema.json`, `docs/Project/cli-agent-config.md`, or any path outside `tools/PlantUmlRender/` and `tests/PlantUmlRender.Tests/`.

Use CODE-REVIEW-AGGREGATED.md and CODE-REVIEW-IMPLEMENTATION-PLAN.md for the exact action for each ID. After implementation, run `dotnet build` and `dotnet test` for PlantUmlRender and PlantUmlRender.Tests.
```

---

### Agent Prompt: Agent CFG

*Copy-paste this into a new agent/chat. Agent CFG runs only in Phase 4; it can be started in parallel with EXT/PSM/PUM when they begin Phase 4.*

```
You are **Agent CFG**. Implement **CR-CFG-5.1.1** (Phase 4 only): add `cli-agent.schema.json` and document the exact shape of `cli-agent.json`.

**Your work:**
1. Create `cli-agent.schema.json` in the repository root. The schema should cover `CliAgent` and its known properties: `CliMdPath`, `PromptsMdPath`, `ExecuteMode`, `ComposerCommand`, and any keys documented in the plan (e.g. `ReinitOnStart`, `RunTimeoutSeconds`, `AgentTimeoutMinutes`, `AgentPath` if/when used). Mark optional/required appropriately.
2. Create or update `docs/Project/cli-agent-config.md` that documents: (a) the exact JSON shape, (b) where it is read (extension, FWH.Prompts, FWH.CLI.Agent), (c) that validation in each consumer is optional and can be added later.

**You may edit/create only:** `cli-agent.schema.json` (repo root), `docs/Project/cli-agent-config.md`. Do not edit `extensions/`, `scripts/`, `src/`, `tools/`, or `cli-agent.json` (the project’s real config). Optionally add `"$schema": "./cli-agent.schema.json"` to `cli-agent.json` in documentation only (e.g. in cli-agent-config.md as an example); do not change the repository’s actual `cli-agent.json` unless the whole team agrees.

Use CODE-REVIEW-AGGREGATED.md section 5 and CODE-REVIEW-IMPLEMENTATION-PLAN.md Phase 4.5 for context. No build/test required for schema or doc; ensure JSON is valid and the doc is clear.
```

---

### Running Agents in Parallel

1. **Phases 1–3 and 5:** Start **Agent EXT**, **Agent PSM**, and **Agent PUM** in three separate agent/chat sessions (or three Cursor composer/agent tabs). Give each the **Shared context** and its **Agent prompt** above. Each implements its phases in order. These three can run fully in parallel.
2. **Phase 4:** Start **Agent EXT**, **Agent PSM**, **Agent PUM**, and **Agent CFG** in four sessions. All four can run in parallel. Agent CFG only does CR-CFG-5.1.1; the others do their Phase 4 IDs.
3. **Merge:** After each phase (or at the end), merge each agent’s changes. Because paths are disjoint, git should merge cleanly. Resolve any conflicts in `cli-agent.json` or shared docs if more than one agent documented the same file.
4. **Verification:** Run `dotnet build`, `dotnet test`, `npm run compile` in `extensions/fwh-cli-agent`, and `Invoke-Pester` for FWH.Prompts after merging.

---

## Phase 1: High Priority — Security & Critical Bugs

**Goal:** Fix security issues and critical bugs that can cause data loss, injection, or incorrect behavior.

**Estimated effort:** 5–7 days

### 1.1 Path validation (security)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-EXT-1.1.1 | EXT | 1.1.1 | Validate resolved `CliMdPath`/`PromptsMdPath` are under workspace root in `getCliMdPath`; reject or normalize. | — |
| CR-PSM-2.1.1 | PSM | 2.1.1 | Validate resolved paths under project root in `Read-CcliAgentConfig` and all `CliMdPath`/`PromptsMdPath` resolvers. | — |
| CR-PSM-2.1.3 | PSM | 2.1.3 | Apply same “under project root” rule; consider allow-list for relative `CliMdPath` (e.g. `CLI.md`, `CLI-history.md`). | CR-PSM-2.1.1 |
| CR-PUM-4.1.1 | PUM | 4.1.1 | Validate `outputDir` under a safe base (e.g. `Environment.CurrentDirectory` or `--root`); validate input files under allowed dir or extension. | — |

### 1.2 CreateInitialCliFile overwrite (data loss)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-CLI-3.1.4 | CLI | 3.1.4 | Add `--init` or `CliAgent:ReinitOnStart`; by default only create `CLI.md` if missing, or reset only if empty/corrupt. | — |
| CR-CLI-3.2.3 | CLI | 3.2.3 | Same as above: make re-initialization opt-in. | CR-CLI-3.1.4 |

### 1.3 Command/shell injection (security)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-CLI-3.1.1 | CLI | 3.1.1 | `ExecutePowerShell`: use `ArgumentList` with `-Command` and command as separate element, or temp file + `-Command (Get-Content -Raw $path)`. | — |
| CR-CLI-3.1.2 | CLI | 3.1.2 | `ExecuteShellCommand`: document as trusted use; use `ArgumentList` (or equiv) to pass command as single argument. | — |

### 1.4 Temp file and sensitive data

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-EXT-1.1.3 | EXT | 1.1.3 | `runWithAgentCli`: `fs.unlinkSync` in `finally` or `p.on('error')`; `fs.writeFileSync` with mode `0o600`; document/warn that prompts should not contain secrets. | — |

### 1.3 Critical bugs

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-PSM-2.2.3 | PSM | 2.2.3 | `Watch-CcliResults`: replace `GetHashCode()` with deterministic hash (e.g. SHA256 of UTF8 bytes) or direct string compare for Results section. | — |
| CR-EXT-1.2.5 | EXT | 1.2.5 | `removeCliBlock`: replace by index (end-to-start) or unique id to handle duplicate `fullMatch`; avoid re-execution. | — |

---

## Phase 2: Bugs & Error Handling

**Goal:** Fix remaining bugs and improve error messages and consistency.

**Estimated effort:** 3–4 days

### 2.1 Extension (EXT)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-EXT-1.2.1 | EXT | 1.2.1 | `runPrompt`: add `debug('runPrompt: user cancelled (no name)')` before `return` when `!name`. | — |
| CR-EXT-1.2.2 | EXT | 1.2.2 | `runPrompt`: add `debug(\`runPrompt: failed to read ${cliPath}: ${e}\`)` in `catch` for `fs.readFileSync`. | — |
| CR-EXT-1.2.3 | EXT | 1.2.3 | `showOutput`: reuse existing `output` from module-level or context; call `output.show()` instead of creating new channel. | — |
| CR-EXT-1.2.4 | EXT | 1.2.4 | `onCliMdChange`: when applying `WorkspaceEdit`, compare `TextDocument.getText()` with content used for `processContent`; only replace if match, or document that CLI.md is “owned” during edits. | — |

### 2.2 FWH.Prompts (PSM)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-PSM-2.2.1 | PSM | 2.2.1 | `Read-CcliPromptsFile`: comment that `$null` = file missing; `@{ PromptTemplates=@{}; SharedContext='' }` = empty or no parseable sections. Ensure `Initialize-CcliPromptTemplates` doesn’t error on empty templates if that’s desired. | — |
| CR-PSM-2.2.2 | PSM | 2.2.2 | `Get-CcliPrompt` with `-PromptsFile` and missing template: use “Prompt template 'X' not found in [file]. Use Get-AvailablePrompts -PromptsFile '...' to list templates.” | — |
| CR-PSM-2.2.4 | PSM | 2.2.4 | `Read-CcliAgentConfig`: use `-LiteralPath` for `Get-Content $configPath -Raw`. Audit and use `-LiteralPath` elsewhere for resolved paths. | — |
| CR-PSM-2.2.5 | PSM | 2.2.5 | `Invoke-CcliPrompt` when `-OutputToFile` and no `-OutputPath`: sanitize `$Name` (replace `[\/:*?"<>|]` with `_`) in default filename, or require `-OutputPath`. | — |

### 2.3 PlantUmlRender (PUM)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-PUM-4.2.1 | PUM | 4.2.1 | `RenderAll`: return `(bool success, int failedCount)` or similar; `Main` returns `failedCount > 0 ? 1 : 0`. | — |
| CR-PUM-4.2.2 | PUM | 4.2.2 | If no valid `@startuml`/`@enduml`: skip file with clear `Console.Error` or pass whole file and document. | — |
| CR-PUM-4.2.3 | PUM | 4.2.3 | Wrap `outputDir.Create()` in try/catch for `UnauthorizedAccessException`/`IOException`; `Console.Error` and non-zero exit. | — |
| CR-PUM-4.2.4 | PUM | 4.2.4 | For `-f` invalid format: `Console.Error.WriteLine("Unknown format: ...")` and keep default (svg+png), or treat as error. | — |

---

## Phase 3: Performance

**Goal:** Caching, polling alternatives, and configurability.

**Estimated effort:** 2–3 days

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-EXT-1.3.1 | EXT | 1.3.1 | Cache `cli-agent.json` (and optionally `getCliMdPath`/`getExecuteOptions`) per workspace; invalidate via `FileSystemWatcher` on `cli-agent.json`. | — |
| CR-EXT-1.3.2 | EXT | 1.3.2 | Optional: reject or truncate if `content.length` > 1MB in `parseCliBlocks`/`processContent` to limit DoS. | — |
| CR-PSM-2.3.1 | PSM | 2.3.1 | Cache `Read-CcliPromptsFile` result keyed by `$Path` and `LastWriteTime`; invalidate on change. Reuse for `-PromptsFile` callers. | — |
| CR-PSM-2.3.2 | PSM | 2.3.2 | `Watch-CcliResults`: consider `FileSystemWatcher` (PS 6+) or document polling and `-Timeout`. | — |
| CR-PUM-4.3.1 | PUM | 4.3.1 | Use `Parallel.ForEachAsync` or `Task.WhenAll` with `MaxDegreeOfParallelism` (e.g. 4) if PlantUml.Net is thread-safe. | — |

---

## Phase 4: Code Quality & Structure

**Goal:** Reduce duplication, centralize config, improve structure.

**Estimated effort:** 4–5 days

### 4.1 Extension (EXT)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-EXT-1.4.1 | EXT | 1.4.1 | Document that `debug()` is only reliable after `activate` sets `_output`; optionally guard `debug` with valid-state check. | — |
| CR-EXT-1.4.2 | EXT | 1.4.2 | `runInComposer`: if `executeCommand` throws, show warning that Composer failed but prompt was copied, or optionally skip clipboard when command failed. | — |
| CR-EXT-1.4.3 | EXT | 1.4.3 | Add `fwhCliAgent.debug` (or `verbose`) and only call `debug()` when true; or split into second “Cursor CLI (debug)” channel. | — |

### 4.2 FWH.Prompts (PSM)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-PSM-2.4.1 | PSM | 2.4.1 | Extract `Resolve-CliFilePath -ProjectRoot` and `Resolve-PromptsFilePath -ProjectRoot`; use in `Write-CcliPromptToCli`, `Invoke-CcliClean`, init. | CR-PSM-2.1.1 |
| CR-PSM-2.4.2 | PSM | 2.4.2 | Consider state-machine or two-pass parser for `Read-CcliPromptsFile`; smaller named regexes; tests for malformed/edge markdown. | — |
| CR-PSM-2.4.3 | PSM | 2.4.3 | Single manifest (Name → Alias → Desc) for `Get-CcliHelp` and `Export-ModuleMember` to avoid divergence. | — |
| CR-PSM-2.4.4 | PSM | 2.4.4 | Single default CLI.md template (script here-string or file); reuse in `Write-CcliPromptToCli` and `Invoke-CcliClean`. | — |

### 4.3 PlantUmlRender (PUM)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-PUM-4.4.1 | PUM | 4.4.1 | Consider `System.CommandLine` for `-o`, `-f`, `--help`. | — |
| CR-PUM-4.4.2 | PUM | 4.4.2 | Extract `RenderOne(FileInfo, string text, OutputFormat, DirectoryInfo, CancellationToken)`; call for Png and Svg when `wantPng`/`wantSvg`. | — |
| CR-PUM-4.4.3 | PUM | 4.4.3 | Document `PlantUmlSettings()` defaults; add `-s`/`--server` when needed. | — |

### 4.5 Config (CFG)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-CFG-5.1.1 | CFG | 5.1.1 | Add `cli-agent.schema.json` and optional validation in extension and PSM; or document exact shape. Consider `ExecuteMode`/`ComposerCommand`. | — |

### 4.6 FWH.Prompts – config read limits

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-PSM-2.1.2 | PSM | 2.1.2 | Optionally limit `Get-Content $configPath -Raw` to first 64KB for `Read-CcliAgentConfig` to avoid DoS. | — |

### 4.7 PlantUmlRender – security doc

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-PUM-4.1.2 | PUM | 4.1.2 | Document that only trusted `.puml` files should be rendered; PlantUml.Net/JVM caveats. | — |

---

## Phase 5: Test Coverage

**Goal:** Add unit, integration, and (where applicable) E2E tests.

**Estimated effort:** 5–7 days

### 5.1 Extension (EXT)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-EXT-1.5.1 | EXT | 1.5.1 | Add tests: `parseCliBlocks`, `extractPromptFromPromptsSection`, `isPromptCommand`, `removeCliBlock`, `pathsEqual`, `getCliMdPath` (mocked config), `getExecuteOptions`. Use `@vscode/test-electron` or similar. | — |
| CR-EXT-1.5.2 | EXT | 1.5.2 | Mock `vscode.env.clipboard`, `vscode.commands.executeCommand`, `spawn` to test `runWithAgentCli` and `runInComposer` args and error handling. | — |

### 5.2 FWH.Prompts (PSM)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-PSM-2.5.1 | PSM | 2.5.1 | Tests: `Invoke-CcliClean` (archive + reset, `CLI-history.md` created/updated); `Watch-CcliResults` with short timeout and mock file change (or E2E). | — |
| CR-PSM-2.5.2 | PSM | 2.5.2 | Tests: `Read-CcliPromptsFile` / `Get-CcliAvailablePrompts` with empty file, only `---`, section without `### Parameters`, shared-context only. | — |
| CR-PSM-2.5.3 | PSM | 2.5.3 | Tests: `Find-CcliProjectRoot` and `Read-CcliAgentConfig` with temp `FunWasHad.sln` and `cli-agent.json` in a test hierarchy. | — |

### 5.3 PlantUmlRender (PUM)

| ID | Component | Review # | Action | Deps |
|----|-----------|----------|--------|------|
| CR-PUM-4.5.1 | PUM | 4.5.1 | Tests: CLI parsing (args → outputDir, wantSvg, wantPng, files); `@startuml`/`@enduml` extraction; exit code (no files, all fail, at least one success). Small `.puml` and mock or real renderer for integration. | — |

---

## Dependency Overview

- **Phase 1** should be completed first; items within it can be **parallelized by agent** (see [Agent Assignments & Parallel Execution](#agent-assignments--parallel-execution)). Each agent respects intra-component Deps (e.g. 2.1.1 before 2.1.3 for PSM).
- **Phase 2** can start once Phase 1 is done for that component; agents continue in parallel.
- **Phase 3** (performance) is largely independent; can overlap with Phase 2. All three component agents (EXT, PSM, PUM) can run in parallel.
- **Phase 4** (structure): CR-PSM-2.4.1 benefits from CR-PSM-2.1.1 (done in P1). **Agent CFG** joins in Phase 4 only; all four agents can run in parallel.
- **Phase 5** (tests) can run in parallel across EXT, PSM, PUM; prefer to add tests after the code under test is stable.

---

## Summary by Component

| Component | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Phase 5 | Total |
|-----------|---------|---------|---------|---------|---------|-------|
| EXT       | 4       | 4       | 2       | 3       | 2       | 15    |
| PSM       | 3       | 4       | 2       | 6       | 3       | 18    |
| PUM       | 1       | 4       | 1       | 3       | 1       | 10    |
| CFG       | 0       | 0       | 0       | 1       | 0       | 1     |
| **Total** | **8**   | **12**  | **5**   | **13**  | **6**   | **44** |

---

*References: [CODE-REVIEW-AGGREGATED.md](./CODE-REVIEW-AGGREGATED.md).*
