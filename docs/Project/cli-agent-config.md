# cli-agent.json Configuration

Configuration file for the FWH.Prompts module and the fwh-cli-agent VS Code extension. Place `cli-agent.json` in the project root (directory containing `FunWasHad.sln`).

## JSON Shape

All keys are under `CliAgent`. All properties are optional.

| Key | Type | Consumers | Description |
|-----|------|-----------|-------------|
| `CliMdPath` | string | EXT, PSM | Path to `CLI.md`. Relative to project root or absolute. Must resolve under project/workspace root. |
| `PromptsMdPath` | string | PSM | Path to prompts markdown (e.g. `prompts.md`). Relative or absolute; must be under project root. |
| `ExecuteMode` | `"composer"` \| `"agent-cli"` | EXT | How to run prompts: `composer` (Composer + clipboard) or `agent-cli` (runs `agent -p`). |
| `ComposerCommand` | string | EXT | VS Code command ID to open Composer when `ExecuteMode` is `composer` (e.g. `composer.new`). |
| `ReinitOnStart` | string | — | Legacy (FWH.CLI.Agent removed). Ignored. |
| `RunTimeoutSeconds` | string | — | Legacy (FWH.CLI.Agent removed). Ignored. |
| `AgentTimeoutMinutes` | string | — | Legacy (FWH.CLI.Agent removed). Ignored. |
| `AgentPath` | string | — | Legacy (FWH.CLI.Agent removed). Ignored. |

## Where It Is Read

- **extensions/fwh-cli-agent** (VS Code): `getCliAgentConfig`; uses `CliMdPath`, `ExecuteMode`, `ComposerCommand`. Config is cached and invalidated when `cli-agent.json` changes.
- **scripts/modules/FWH.Prompts**: `Read-CcliAgentConfig`; uses `CliMdPath`, `PromptsMdPath` for path resolution in `Write-CcliPromptToCli`, `Invoke-CcliClean`, and module init.

## Schema and Validation

- A JSON schema is provided at **`cli-agent.schema.json`** (repository root). You can add `"$schema": "./cli-agent.schema.json"` to `cli-agent.json` for editor support.
- Validation in each consumer (extension, PSM, CLI) is **optional** and can be added later. Today, unknown keys are ignored; typos (e.g. `climdpath`) may fall back to defaults.

## Example

```json
{
  "$schema": "./cli-agent.schema.json",
  "CliAgent": {
    "CliMdPath": "CLI.md",
    "PromptsMdPath": "scripts/modules/FWH.Prompts/prompts.md",
    "ExecuteMode": "agent-cli",
    "ReinitOnStart": "false",
    "RunTimeoutSeconds": "60",
    "AgentTimeoutMinutes": "10"
  }
}
```
