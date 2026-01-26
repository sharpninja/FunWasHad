# FWH CLI Agent (VS Code Extension)

Monitors `CLI.md`, parses prompts from the **## Prompts** section, and runs them in a new Cursor agent—either in **Composer** or via the **agent CLI** in a terminal.

## How it works

1. **Watch** – The extension watches `CLI.md` (or the path from `cli-agent.json` / `fwhCliAgent.cliMdPath`).
2. **Parse** – When you add a ` ```cli ` block with `prompt <name>`, it finds the matching `### Prompt: <name>` block and the ` ``` ` ` ```prompt ` fenced body in **## Prompts**.
3. **Run** – It runs that prompt in a new Cursor agent:
   - **Composer** (default): opens Composer and copies the prompt to the clipboard so you can paste (Ctrl+V).
   - **agent-cli**: runs `agent -p "..." --output-format text` in a new terminal (requires [Cursor’s agent CLI](https://cursor.com/install) on PATH).
4. **Clean** – The ` ```cli prompt <name> ` block is removed from `CLI.md` after it’s processed so it isn’t run again.

## Setup

- **CLI.md** in the project root (or path from `cli-agent.json` / settings).
- **## Prompts** with blocks like:

  ````markdown
  ### Prompt: code-review (2025-01-25 12:00:00)

  ```prompt
  Your populated prompt text here...
  ```
  ````

  Add these with FWH.Prompts: `Write-PromptToCli -Name 'code-review' -Parameters @{...}`

- **```cli** block under **## Commands** (or anywhere in the file):

  ````markdown
  ```cli
  prompt code-review
  ```
  ````

  When you save `CLI.md`, the extension picks it up and runs the prompt.

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `fwhCliAgent.cliMdPath` | `CLI.md` | Path to CLI.md relative to workspace root. Overrides `cli-agent.json` when set. |
| `fwhCliAgent.executeMode` | `composer` | `composer` = open Composer + copy prompt to clipboard; `agent-cli` = run `agent -p "..."` in a new terminal. |
| `fwhCliAgent.composerCommand` | `composer.new` | Command ID to open Cursor Composer when mode is `composer`. Adjust if Cursor uses a different ID (e.g. `aichat.new`). |

## Commands

- **FWH CLI Agent: Process CLI.md now** – Re-runs parsing and execution on the current `CLI.md` (e.g. after editing).
- **FWH CLI Agent: Run prompt from CLI.md** – Asks for a prompt name, finds it in **## Prompts**, and runs it with the current `executeMode`.

## Requirements

- **Cursor** (or a VS Code build that provides Composer / `agent` CLI).
- For **agent-cli** mode: `agent` on PATH (install from [cursor.com/install](https://cursor.com/install)).

## Development

```bash
cd extensions/fwh-cli-agent
npm install
npm run compile
```

 then **Run > Run Extension** or **F5** in VS Code/Cursor with the `extensions/fwh-cli-agent` folder opened, or **Install from VSIX** after `vsce package`.

## License

Part of FunWasHad. All rights reserved.
