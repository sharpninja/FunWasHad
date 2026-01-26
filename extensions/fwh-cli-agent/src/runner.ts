/**
 * Run prompt in Composer or via agent-cli. Accepts injected deps for testing.
 * CR-EXT-1.5.2: runInComposer and runWithAgentCli testable with mocked
 * vscode.env.clipboard, vscode.commands.executeCommand, spawn.
 */

import * as fs from 'fs';
import * as path from 'path';
import { ChildProcess } from 'child_process';

export interface RunInComposerDeps {
  executeCommand: (command: string) => Promise<unknown>;
  clipboardWrite: (text: string) => Promise<void>;
  showWarning: (msg: string) => void;
  showInfo: (msg: string) => void;
}

export interface RunWithAgentCliDeps {
  spawn: (command: string, args: string[], options: { cwd: string }) => ChildProcess;
}

export interface OutputChannelLike {
  append: (s: string) => void;
  appendLine: (s: string) => void;
}

export async function runInComposer(
  deps: RunInComposerDeps,
  promptText: string,
  composerCommand: string,
  output: OutputChannelLike
): Promise<void> {
  try {
    await deps.executeCommand(composerCommand);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    output.appendLine(`[Cursor CLI] Composer command failed: ${msg}. Prompt will still be copied to clipboard.`);
    deps.showWarning(`FWH CLI Agent: Composer command failed (${msg}). Prompt will be copied to clipboard.`);
  }
  await deps.clipboardWrite(promptText);
  output.appendLine(
    '[Cursor CLI] Composer opened; prompt copied to clipboard. Paste (Ctrl+V). Composer reply is not captured.'
  );
  deps.showInfo('FWH CLI Agent: Prompt copied to clipboard. Paste into Composer (Ctrl+V).');
}

export async function runWithAgentCli(
  deps: RunWithAgentCliDeps,
  promptText: string,
  workspaceRoot: string,
  output: OutputChannelLike
): Promise<void> {
  const dir = path.join(workspaceRoot, '.cursor');
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  const promptFile = path.join(dir, `fwh-cli-agent-prompt-${Date.now()}.txt`);
  fs.writeFileSync(promptFile, promptText, { encoding: 'utf8', mode: 0o600 });

  const isWin = process.platform === 'win32';
  const escapedPath = promptFile.replace(/'/g, "''");
  const winCmd = `& { $p = Get-Content -Raw -LiteralPath '${escapedPath}'; agent -p $p --output-format text 2>&1 }`;
  const unixCmd = `agent -p "$(cat '${promptFile.replace(/'/g, "'\\''")}')" --output-format text 2>&1`;

  output.appendLine('[Cursor CLI] Running agent-cli...');

  const p = deps.spawn(isWin ? 'powershell' : 'sh', isWin ? ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', winCmd] : ['-c', unixCmd], {
    cwd: workspaceRoot,
  });

  if (p.stdout) { p.stdout.setEncoding('utf8'); p.stdout.on('data', (chunk: string | Buffer) => output.append(chunk.toString())); }
  if (p.stderr) { p.stderr.setEncoding('utf8'); p.stderr.on('data', (chunk: string | Buffer) => output.append(chunk.toString())); }

  try {
    await new Promise<void>((resolve, reject) => {
      p.on('close', (code) => {
        output.appendLine(`\n[Cursor CLI] Agent finished (exit ${code != null ? code : '?'})`);
        resolve();
      });
      p.on('error', (err) => reject(err));
    });
  } finally {
    try {
      if (fs.existsSync(promptFile)) fs.unlinkSync(promptFile);
    } catch {
      /* ignore */
    }
  }
}
