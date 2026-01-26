import * as vscode from 'vscode';
import * as fs from 'fs';
import * as path from 'path';
import { spawn } from 'child_process';
import {
  parseCliBlocks,
  extractPromptFromPromptsSection,
  isPromptCommand,
  removeCliBlock,
  pathsEqual,
} from './parser';
import { resolveCliMdPath, resolveExecuteOptions } from './resolver';
import * as runner from './runner';

/** CR-EXT-1.3.2: max CLI.md size to avoid DoS from huge files. */
const MAX_CLI_MD_BYTES = 1_000_000;

let _output: vscode.OutputChannel | undefined;
/** CR-EXT-1.3.1: cache for cli-agent.json per workspace; cleared when cli-agent.json changes. */
let _configCache: { root: string; config: CliAgentJson['CliAgent'] | undefined } | null = null;
let _configWatcher: vscode.FileSystemWatcher | undefined;

/** CR-EXT-1.4.1: Reliable only after activate() sets _output. CR-EXT-1.4.3: only logs when fwhCliAgent.debug is true. */
function debug(msg: string): void {
  if (!_output) return;
  if (vscode.workspace.getConfiguration('fwhCliAgent').get<boolean>('debug') !== true) return;
  _output.appendLine(`[Cursor CLI] ${msg}`);
}

function getWorkspaceRoot(): string | undefined {
  const folder = vscode.workspace.workspaceFolders?.[0];
  return folder?.uri.fsPath;
}

function readJsonAt<T>(fsPath: string): T | undefined {
  try {
    if (!fs.existsSync(fsPath)) {
      debug(`readJsonAt: file not found: ${fsPath}`);
      return undefined;
    }
    const raw = fs.readFileSync(fsPath, 'utf8');
    return JSON.parse(raw) as T;
  } catch (e) {
    debug(`readJsonAt: error for ${fsPath}: ${e instanceof Error ? e.message : String(e)}`);
    return undefined;
  }
}

interface CliAgentJson {
  CliAgent?: {
    CliMdPath?: string;
    PromptsMdPath?: string;
    ExecuteMode?: 'composer' | 'agent-cli';
    ComposerCommand?: string;
  };
}

function getCliAgentConfig(workspaceRoot: string): CliAgentJson['CliAgent'] {
  if (_configCache && _configCache.root === workspaceRoot) {
    return _configCache.config;
  }
  const p = path.join(workspaceRoot, 'cli-agent.json');
  const obj = readJsonAt<CliAgentJson>(p);
  const config = obj?.CliAgent;
  _configCache = { root: workspaceRoot, config };
  return config;
}

function getCliMdPath(workspaceRoot: string): string {
  const rootResolved = path.resolve(workspaceRoot);
  const cliAgent = getCliAgentConfig(workspaceRoot);
  const vsc = vscode.workspace.getConfiguration('fwhCliAgent').get<string>('cliMdPath');
  const res = resolveCliMdPath(rootResolved, cliAgent?.CliMdPath, vsc);
  debug(`getCliMdPath: => ${res}`);
  return res;
}

function getExecuteOptions(workspaceRoot: string): { mode: 'composer' | 'agent-cli'; composerCommand: string } {
  const cliAgent = getCliAgentConfig(workspaceRoot);
  const vsc = vscode.workspace.getConfiguration('fwhCliAgent');
  const { mode, composerCommand } = resolveExecuteOptions(
    cliAgent?.ExecuteMode,
    cliAgent?.ComposerCommand,
    vsc.get<string>('executeMode'),
    vsc.get<string>('composerCommand')
  );
  debug(`getExecuteOptions: mode=${mode}, composerCommand=${composerCommand}`);
  return { mode, composerCommand };
}

async function runInComposer(
  promptText: string,
  composerCommand: string,
  output: vscode.OutputChannel
): Promise<void> {
  debug(`runInComposer: invoking executeCommand('${composerCommand}')`);
  await runner.runInComposer(
    {
      executeCommand: (cmd) => Promise.resolve(vscode.commands.executeCommand(cmd)),
      clipboardWrite: (t) => Promise.resolve(vscode.env.clipboard.writeText(t)),
      showWarning: (m) => void vscode.window.showWarningMessage(m),
      showInfo: (m) => void vscode.window.showInformationMessage(m),
    },
    promptText,
    composerCommand,
    output
  );
}

async function runWithAgentCli(
  promptText: string,
  workspaceRoot: string,
  output: vscode.OutputChannel
): Promise<void> {
  debug(`runWithAgentCli: spawn cwd=${workspaceRoot}`);
  await runner.runWithAgentCli({ spawn }, promptText, workspaceRoot, output);
}

async function processContent(
  content: string,
  workspaceRoot: string,
  output: vscode.OutputChannel
): Promise<{ newContent: string; changed: boolean }> {
  if (content.length > MAX_CLI_MD_BYTES) {
    output.appendLine(`[Cursor CLI] CLI.md exceeds ${MAX_CLI_MD_BYTES / 1e6}MB; skipping processing to limit load.`);
    return { newContent: content, changed: false };
  }
  const blocks = parseCliBlocks(content);
  debug(`processContent: ${blocks.length} \`\`\`cli block(s) to consider`);
  let newContent = content;
  /** CR-EXT-1.2.5: collect removals and apply end-to-start so indices stay valid. */
  const toRemove: { fullMatch: string; index: number }[] = [];

  const { mode, composerCommand } = getExecuteOptions(workspaceRoot);

  for (const b of blocks) {
    const pr = isPromptCommand(b.command);
    if (!pr) {
      debug(`processContent: block at index ${b.index} is not a prompt command, skipping`);
      continue;
    }

    const promptText = extractPromptFromPromptsSection(content, pr.name);
    if (!promptText) {
      output.appendLine(`[Cursor CLI] No populated prompt in ## Prompts for: ${pr.name}`);
      void vscode.window.showWarningMessage(
        `FWH CLI Agent: No populated prompt in CLI.md for "${pr.name}". Use Write-PromptToCli -Name '${pr.name}' to add one.`
      );
      continue;
    }

    output.appendLine(`[Cursor CLI] Parsed prompt (${pr.name}):`);
    output.appendLine('---');
    output.appendLine(promptText);
    output.appendLine('---');
    output.appendLine(`[Cursor CLI] Running prompt: ${pr.name} (mode: ${mode})`);

    if (mode === 'composer') {
      await runInComposer(promptText, composerCommand, output);
    } else {
      await runWithAgentCli(promptText, workspaceRoot, output);
    }

    toRemove.push({ fullMatch: b.fullMatch, index: b.index });
    debug(`processContent: queued removal of \`\`\`cli block for prompt '${pr.name}'`);
  }

  // Apply removals from highest index to lowest so positions remain valid
  toRemove.sort((a, b) => b.index - a.index);
  for (const r of toRemove) {
    newContent = removeCliBlock(newContent, r.fullMatch, r.index);
  }

  return { newContent, changed: toRemove.length > 0 };
}

let watcher: vscode.FileSystemWatcher | undefined;
let lastProcessed = 0;
const DEBOUNCE_MS = 800;

async function onCliMdChange(uri: vscode.Uri, output: vscode.OutputChannel): Promise<void> {
  output.appendLine(`[Cursor CLI] File event: ${uri.fsPath}`);

  if (Date.now() - lastProcessed < DEBOUNCE_MS) {
    debug(`onCliMdChange: debounced (within ${DEBOUNCE_MS}ms)`);
    return;
  }
  lastProcessed = Date.now();

  const root = getWorkspaceRoot();
  if (!root) {
    debug('onCliMdChange: no workspace root');
    return;
  }

  const cliPath = getCliMdPath(root);
  if (!pathsEqual(uri.fsPath, cliPath)) {
    debug(`onCliMdChange: path mismatch, ignoring. uri=${uri.fsPath} expected=${cliPath}`);
    return;
  }

  let content: string;
  try {
    content = fs.readFileSync(uri.fsPath, 'utf8');
  } catch (e) {
    debug(`onCliMdChange: failed to read file: ${e instanceof Error ? e.message : String(e)}`);
    return;
  }

  const when = new Date().toISOString();
  output.appendLine(`[Cursor CLI] CLI.md changed: ${uri.fsPath} — ${when} — processing...`);
  output.show();
  const { newContent, changed } = await processContent(content, root, output);
  if (changed) output.appendLine(`[Cursor CLI] Processed and removed prompt block(s).`);
  if (changed) {
    const doc = await vscode.workspace.openTextDocument(uri);
    // CR-EXT-1.2.4: only replace if current document text matches content we used for processContent
    const currentText = doc.getText();
    if (currentText !== content) {
      debug(`onCliMdChange: doc changed since read; skipping applyEdit to avoid overwriting concurrent edits.`);
      return;
    }
    const fullRange =
      doc.lineCount === 0
        ? new vscode.Range(0, 0, 0, 0)
        : (() => {
            const last = doc.lineAt(doc.lineCount - 1);
            return new vscode.Range(0, 0, last.range.end.line, last.range.end.character);
          })();
    const edit = new vscode.WorkspaceEdit();
    edit.replace(uri, fullRange, newContent);
    await vscode.workspace.applyEdit(edit);
  }
}

const OUTPUT_CHANNEL_NAME = 'Cursor CLI';

export function activate(context: vscode.ExtensionContext): void {
  const output = vscode.window.createOutputChannel(OUTPUT_CHANNEL_NAME);
  _output = output;
  output.appendLine('[Cursor CLI] Activating...');
  output.show();

  // CR-EXT-1.2.3: reuse existing output channel (created above) instead of creating a new one
  context.subscriptions.push(
    vscode.commands.registerCommand('fwhCliAgent.showOutput', () => {
      output.show();
    })
  );

  const statusBar = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
  statusBar.text = '$(terminal) Cursor CLI';
  statusBar.tooltip = 'Show Cursor CLI output';
  statusBar.command = 'fwhCliAgent.showOutput';
  statusBar.show();
  context.subscriptions.push(statusBar);

  const folder = vscode.workspace.workspaceFolders?.[0];
  const root = folder?.uri.fsPath;
  if (!root || !folder) {
    output.appendLine('[Cursor CLI] Not activated: no workspace folder. Open a folder (File > Open Folder).');
    return;
  }
  debug(`activate: workspace root=${root}`);

  const cliAgent = getCliAgentConfig(root);
  const cliPath = getCliMdPath(root);

  if (cliAgent) {
    output.appendLine(
      `[Cursor CLI] Config from cli-agent.json: CliMdPath=${cliAgent.CliMdPath ?? '(default)'}, PromptsMdPath=${cliAgent.PromptsMdPath ?? '-'}, ExecuteMode=${cliAgent.ExecuteMode ?? '-'}, ComposerCommand=${cliAgent.ComposerCommand ?? '-'}`
    );
  } else {
    output.appendLine('[Cursor CLI] cli-agent.json not found; using defaults and fwhCliAgent settings.');
  }
  output.appendLine(`[Cursor CLI] Watching: ${cliPath}`);
  output.appendLine(`[Cursor CLI] onDidSaveTextDocument subscribed for: ${path.basename(cliPath)}`);

  const rel = path.relative(root, cliPath).replace(/\\/g, '/') || 'CLI.md';
  debug(`createFileSystemWatcher pattern: ${rel}`);
  watcher = vscode.workspace.createFileSystemWatcher(new vscode.RelativePattern(folder, rel));
  const handler = (u: vscode.Uri) => onCliMdChange(u, output);
  watcher.onDidChange(handler);
  watcher.onDidCreate(handler);
  context.subscriptions.push(watcher);

  // CR-EXT-1.3.1: invalidate config cache when cli-agent.json changes
  _configWatcher = vscode.workspace.createFileSystemWatcher(new vscode.RelativePattern(folder, 'cli-agent.json'));
  _configWatcher.onDidChange(() => { _configCache = null; debug('config cache invalidated: cli-agent.json changed'); });
  _configWatcher.onDidDelete(() => { _configCache = null; debug('config cache invalidated: cli-agent.json deleted'); });
  context.subscriptions.push(_configWatcher);

  context.subscriptions.push(
    vscode.workspace.onDidSaveTextDocument((doc) => {
      if (doc.uri.scheme !== 'file') return;
      const r = getWorkspaceRoot();
      if (!r) {
        debug('onDidSaveTextDocument: no workspace root');
        return;
      }
      const p = getCliMdPath(r);
      if (pathsEqual(doc.uri.fsPath, p)) {
        output.appendLine(`[Cursor CLI] Save detected: ${doc.uri.fsPath}`);
        output.show(true);
        void onCliMdChange(doc.uri, output);
      } else if (path.basename(doc.uri.fsPath).toLowerCase() === 'cli.md') {
        output.appendLine(`[Cursor CLI] Save ignored (path mismatch): doc=${doc.uri.fsPath} expected=${p}`);
        output.show(true);
      }
    })
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('fwhCliAgent.processCliFile', async () => {
      const u = vscode.Uri.file(cliPath);
      await onCliMdChange(u, output);
    })
  );

  context.subscriptions.push(
    vscode.commands.registerCommand('fwhCliAgent.runPrompt', async () => {
      const name = await vscode.window.showInputBox({
        prompt: 'Prompt name (from ## Prompts in CLI.md)',
        placeHolder: 'e.g. code-review',
      });
      if (!name) {
        debug('runPrompt: user cancelled (no name)');
        return;
      }
      let content: string;
      try {
        content = fs.readFileSync(cliPath, 'utf8');
      } catch (e) {
        debug(`runPrompt: failed to read ${cliPath}: ${e instanceof Error ? e.message : String(e)}`);
        void vscode.window.showErrorMessage(`FWH CLI Agent: Could not read CLI.md: ${e}`);
        return;
      }
      if (content.length > MAX_CLI_MD_BYTES) {
        output.appendLine(`[Cursor CLI] CLI.md exceeds ${MAX_CLI_MD_BYTES / 1e6}MB; refusing to process.`);
        void vscode.window.showWarningMessage(`FWH CLI Agent: CLI.md is too large (${(content.length / 1e6).toFixed(1)}MB).`);
        return;
      }
      const promptText = extractPromptFromPromptsSection(content, name);
      if (!promptText) {
        output.appendLine(`[Cursor CLI] No populated prompt for "${name}" in CLI.md.`);
        void vscode.window.showWarningMessage(
          `FWH CLI Agent: No populated prompt for "${name}" in CLI.md.`
        );
        return;
      }
      output.appendLine(`[Cursor CLI] Parsed prompt (${name}):`);
      output.appendLine('---');
      output.appendLine(promptText);
      output.appendLine('---');
      output.appendLine(`[Cursor CLI] Run prompt: ${name} (command)`);
      output.show();
      const { mode, composerCommand } = getExecuteOptions(root);
      if (mode === 'composer') {
        await runInComposer(promptText, composerCommand, output);
      } else {
        await runWithAgentCli(promptText, root, output);
      }
    })
  );
}

export function deactivate(): void {
  debug('Deactivating.');
  _output = undefined;
  _configCache = null;
  _configWatcher?.dispose();
  _configWatcher = undefined;
  watcher?.dispose();
  watcher = undefined;
}
