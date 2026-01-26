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
