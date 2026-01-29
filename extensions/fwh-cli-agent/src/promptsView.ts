/**
 * MVP-SUPPORT-005: TreeView for prompt list and Webview for parameter form + Invoke.
 */

import * as vscode from 'vscode';
import * as fs from 'fs';
import type { ParsedPrompt } from './parser';
import { parsePromptsMd, listPromptNamesFromCliMd } from './parser';

export interface PromptListItem {
  name: string;
  prompt?: ParsedPrompt;
}

export type LoadPromptListFn = () => PromptListItem[];

/**
 * Builds the list from prompts.md (and optionally CLI.md names not in prompts.md).
 * Sync; uses fs. Call from extension with paths from getPromptsMdPath/getCliMdPath.
 */
export function loadPromptList(
  promptsMdPath: string,
  cliMdPath: string
): PromptListItem[] {
  const result: PromptListItem[] = [];
  const byName = new Map<string, PromptListItem>();

  if (fs.existsSync(promptsMdPath)) {
    try {
      const content = fs.readFileSync(promptsMdPath, 'utf8');
      const parsed = parsePromptsMd(content);
      for (const p of parsed) {
        const item: PromptListItem = { name: p.name, prompt: p };
        byName.set(p.name, item);
        result.push(item);
      }
    } catch {
      // ignore
    }
  }

  if (fs.existsSync(cliMdPath)) {
    try {
      const content = fs.readFileSync(cliMdPath, 'utf8');
      const names = listPromptNamesFromCliMd(content);
      for (const n of names) {
        if (!byName.has(n)) {
          const item: PromptListItem = { name: n };
          byName.set(n, item);
          result.push(item);
        }
      }
    } catch {
      // ignore
    }
  }

  return result;
}

export class PromptsTreeDataProvider implements vscode.TreeDataProvider<vscode.TreeItem> {
  private _onDidChangeTreeData = new vscode.EventEmitter<void>();
  readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

  constructor(private load: LoadPromptListFn) {}

  refresh(): void {
    this._onDidChangeTreeData.fire();
  }

  getTreeItem(element: vscode.TreeItem): vscode.TreeItem {
    return element;
  }

  getChildren(_element?: vscode.TreeItem): vscode.ProviderResult<vscode.TreeItem[]> {
    const items = this.load();
    return items.map((i) => {
      const ti = new vscode.TreeItem(i.name, vscode.TreeItemCollapsibleState.None);
      ti.id = i.name;
      ti.contextValue = i.prompt ? 'promptWithParams' : 'promptFromCli';
      ti.tooltip = i.prompt?.description ?? `Prompt from CLI.md: ${i.name}`;
      ti.command = { command: 'fwhCliAgent.openPromptForm', title: 'Open prompt form' };
      return ti;
    });
  }
}

/** Substitute {ParamName} in template with values. */
export function substituteTemplate(template: string, params: Record<string, string>): string {
  return template.replace(/\{([^}]+)\}/g, (_, k) => params[k.trim()] ?? '');
}

const FORM_VIEW_TYPE = 'fwhCliAgent.promptForm';

function getWebviewContent(p: ParsedPrompt): string {
  const paramsJson = JSON.stringify(p.parameters);
  const nameHtml = escapeHtml(p.name);
  const descHtml = escapeHtml(p.description);
  const rows = p.parameters
    .map(
      (r) => `
    <div class="row">
      <label for="p-${escapeHtml(r.name)}">${escapeHtml(r.name)}</label>
      <input id="p-${escapeHtml(r.name)}" data-param="${escapeHtml(r.name)}" placeholder="${escapeHtml(r.default)}" />
    </div>`
    )
    .join('');

  return `<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <meta http-equiv="Content-Security-Policy" content="default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline';">
  <style>
    body { font-family: var(--vscode-font-family); font-size: 13px; padding: 12px; }
    h2 { margin-top: 0; }
    .row { margin-bottom: 10px; }
    label { display: inline-block; width: 180px; }
    input { width: 280px; }
    button { margin-top: 12px; padding: 6px 14px; }
  </style>
</head>
<body>
  <h2>${nameHtml}</h2>
  <p>${descHtml || '(No description)'}</p>
  <form id="form">
    ${rows}
    <div class="row"><button type="submit">Invoke</button></div>
  </form>
  <script>
    (function() {
      var params = ${paramsJson};
      var form = document.getElementById('form');
      form.onsubmit = function(e) {
        e.preventDefault();
        var vs = {};
        params.forEach(function(r) {
          var el = document.querySelector('[data-param="' + r.name + '"]');
          vs[r.name] = el ? el.value : '';
        });
        var promptName = ${JSON.stringify(p.name)};
        var template = ${JSON.stringify(p.template)};
        var filled = template.replace(/\\{([^}]+)\\}/g, function(_, k) { return vs[k.trim()] || ''; });
        (function(){ var api = typeof acquireVsCodeApi === 'function' ? acquireVsCodeApi() : null; if (api && api.postMessage) api.postMessage({ type: 'invoke', promptName: promptName, filled: filled }); })();
      };
    })();
  </script>
</body>
</html>`;
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

let _formPanel: vscode.WebviewPanel | undefined;

/**
 * Creates or reveals the prompt form webview and sets the given prompt.
 * The webview will post { type: 'invoke', promptName, filled } on Invoke.
 */
export function createOrShowPromptFormPanel(
  prompt: ParsedPrompt,
  onInvoke: (promptName: string, filledText: string) => void
): void {
  if (_formPanel) {
    _formPanel.reveal();
    _formPanel.webview.html = getWebviewContent(prompt);
    _formPanel.title = `Prompt: ${prompt.name}`;
    return;
  }
  _formPanel = vscode.window.createWebviewPanel(FORM_VIEW_TYPE, `Prompt: ${prompt.name}`, vscode.ViewColumn.Beside, {
    enableScripts: true,
    retainContextWhenHidden: true,
  });
  _formPanel.webview.html = getWebviewContent(prompt);
  _formPanel.onDidDispose(() => {
    _formPanel = undefined;
  });
  _formPanel.webview.onDidReceiveMessage((m: { type?: string; promptName?: string; filled?: string }) => {
    if (m?.type === 'invoke' && typeof m.promptName === 'string' && typeof m.filled === 'string') {
      onInvoke(m.promptName, m.filled);
    }
  });
}
