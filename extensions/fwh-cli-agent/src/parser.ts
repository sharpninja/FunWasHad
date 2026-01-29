/**
 * Pure parsing and path utilities for CLI blocks and prompts. No vscode dependency.
 * CR-EXT-1.5.1: extracted for unit testing.
 */

import * as path from 'path';

export const FENCE = '```';
export const CLI_BLOCK_RE = /```cli\s*\n([\s\S]*?)\n```/gi;

export interface ParsedBlock {
  fullMatch: string;
  command: string;
  index: number;
}

export function parseCliBlocks(content: string): ParsedBlock[] {
  const blocks: ParsedBlock[] = [];
  let m: RegExpExecArray | null;
  CLI_BLOCK_RE.lastIndex = 0;
  while ((m = CLI_BLOCK_RE.exec(content)) !== null) {
    const cmd = m[1].trim();
    if (cmd) blocks.push({ fullMatch: m[0], command: cmd, index: m.index });
  }
  return blocks;
}

export function extractPromptFromPromptsSection(content: string, promptName: string): string | null {
  const fence = FENCE;
  const re = new RegExp(
    `### Prompt: ${promptName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\s*\\([^)]*\\)\\s*[\\r\\n]+` +
      `${fence}prompt\\s*[\\r\\n]+([\\s\\S]*?)[\\r\\n]+${fence}`,
    'i'
  );
  const m = content.match(re);
  return m ? m[1].trim() : null;
}

export function isPromptCommand(cmd: string): { name: string } | null {
  const t = cmd.trim().split(/\s+/);
  if (t[0]?.toLowerCase() === 'prompt' && t[1]) return { name: t[1].trim() };
  return null;
}

/** CR-EXT-1.2.5: remove by index to handle duplicate fullMatch. */
export function removeCliBlock(content: string, fullMatch: string, index: number): string {
  return content.slice(0, index) + content.slice(index + fullMatch.length);
}

export function pathsEqual(a: string, b: string): boolean {
  const ra = path.resolve(a);
  const rb = path.resolve(b);
  return process.platform === 'win32' ? ra.toLowerCase() === rb.toLowerCase() : ra === rb;
}

/** Returns true if resolvedPath is under workspaceRoot (CR-EXT-1.1.1 path validation). */
export function isUnderWorkspaceRoot(workspaceRoot: string, resolvedPath: string): boolean {
  const rel = path.relative(workspaceRoot, path.resolve(resolvedPath));
  if (rel.startsWith('..') || path.isAbsolute(rel)) return false;
  return true;
}

// --- MVP-SUPPORT-005: prompts.md and CLI.md list/parse ---

export interface ParsedPromptParam {
  name: string;
  description: string;
  required: boolean;
  default: string;
}

export interface ParsedPrompt {
  name: string;
  description: string;
  template: string;
  parameters: ParsedPromptParam[];
}

/**
 * Parses prompts.md content into a list of prompts with names, descriptions, templates, and parameters.
 * Format: sections separated by ---, each with ## name, body (template with {Param}), and optional ### Parameters table.
 */
export function parsePromptsMd(content: string): ParsedPrompt[] {
  const results: ParsedPrompt[] = [];
  const sections = content.split(/\n---\s*\n/);

  for (const block of sections) {
    const nameMatch = block.match(/^##\s+([^\n#]+)/m);
    if (!nameMatch) continue;
    const name = nameMatch[1].trim();
    if (!name) continue;

    const paramsIdx = block.indexOf('### Parameters');
    let template: string;
    if (paramsIdx >= 0) {
      template = block.slice(nameMatch[0].length, paramsIdx).trim();
    } else {
      template = block.slice(nameMatch[0].length).trim();
    }

    const descMatch = template.match(/^([^\n{]+)/);
    const description = descMatch ? descMatch[1].trim() : '';

    const parameters = parseParametersTable(block, paramsIdx);
    const placeholders = new Set<string>();
    const placeRe = /\{([^}]+)\}/g;
    let m: RegExpExecArray | null;
    while ((m = placeRe.exec(template)) !== null) placeholders.add(m[1].trim());
    for (const p of placeholders) {
      if (!parameters.some((x) => x.name === p)) {
        parameters.push({ name: p, description: '', required: false, default: '' });
      }
    }

    results.push({ name, description, template, parameters });
  }
  return results;
}

function parseParametersTable(block: string, paramsSectionIdx: number): ParsedPromptParam[] {
  const out: ParsedPromptParam[] = [];
  if (paramsSectionIdx < 0) return out;
  const after = block.slice(paramsSectionIdx);
  const lines = after.split(/\r?\n/).filter((l) => /^\|.+\|/.test(l));
  let headerDone = false;
  for (const line of lines) {
    const cells = line
      .split('|')
      .map((c) => c.trim())
      .filter((_, i) => i > 0);
    if (cells.length < 4) continue;
    const first = (cells[0] ?? '').trim();
    const fl = first.toLowerCase();
    if (fl === 'parameter' || /^-+$/.test(first) || first === '') {
      continue;
    }
    const param = (cells[0] ?? '').trim();
    const desc = (cells[1] ?? '').trim();
    const rawReq = (cells[2] ?? '').trim().toLowerCase();
    const req = rawReq === 'yes' || rawReq === 'true' || rawReq === '1';
    const def = (cells[3] ?? '').trim();
    if (param) out.push({ name: param, description: desc, required: req, default: def });
  }
  return out;
}

/**
 * Returns unique prompt names from CLI.md ## Prompts section (### Prompt: name (date) blocks).
 * Used when prompts.md is not available.
 */
export function listPromptNamesFromCliMd(content: string): string[] {
  const re = /###\s+Prompt:\s+([^\s(]+)\s*\([^)]*\)/gi;
  const seen = new Set<string>();
  let m: RegExpExecArray | null;
  while ((m = re.exec(content)) !== null) {
    const n = (m[1] ?? '').trim();
    if (n) seen.add(n);
  }
  return Array.from(seen);
}
