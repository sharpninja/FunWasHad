/**
 * Pure resolution of CLI path and execute options from config/vsc values. No vscode dependency.
 * CR-EXT-1.5.1: extracted for unit testing with mocked config.
 */

import * as path from 'path';
import { isUnderWorkspaceRoot } from './parser';

/** Default path to prompts.md when not configured. Matches FWH.Prompts and cli-agent.json. */
export const DEFAULT_PROMPTS_MD = 'scripts/modules/FWH.Prompts/prompts.md';

export function resolveCliMdPath(
  workspaceRoot: string,
  configCliMdPath?: string,
  vscCliMdPath?: string
): string {
  const rootResolved = path.resolve(workspaceRoot);
  const defaultPath = path.join(workspaceRoot, 'CLI.md');

  if (configCliMdPath) {
    const s = configCliMdPath;
    const res = path.isAbsolute(s) ? s : path.join(workspaceRoot, s);
    if (isUnderWorkspaceRoot(rootResolved, res)) return res;
    return defaultPath;
  }
  if (vscCliMdPath) {
    const res = path.isAbsolute(vscCliMdPath) ? vscCliMdPath : path.join(workspaceRoot, vscCliMdPath);
    if (isUnderWorkspaceRoot(rootResolved, res)) return res;
    return defaultPath;
  }
  return defaultPath;
}

export function resolveExecuteOptions(
  configExecuteMode?: string,
  configComposer?: string,
  vscExecuteMode?: string,
  vscComposer?: string
): { mode: 'composer' | 'agent-cli'; composerCommand: string } {
  const mode = (configExecuteMode ?? vscExecuteMode ?? 'composer') as 'composer' | 'agent-cli';
  const composerCommand = configComposer ?? vscComposer ?? 'composer.new';
  return { mode, composerCommand };
}

export function resolvePromptsMdPath(
  workspaceRoot: string,
  configPromptsMdPath?: string,
  vscPromptsMdPath?: string
): string {
  const rootResolved = path.resolve(workspaceRoot);
  const defaultPath = path.join(rootResolved, DEFAULT_PROMPTS_MD);

  if (configPromptsMdPath) {
    const s = configPromptsMdPath.trim();
    const res = path.isAbsolute(s) ? s : path.join(rootResolved, s);
    if (isUnderWorkspaceRoot(rootResolved, res)) return res;
    return defaultPath;
  }
  if (vscPromptsMdPath) {
    const s = vscPromptsMdPath.trim();
    const res = path.isAbsolute(s) ? s : path.join(rootResolved, s);
    if (isUnderWorkspaceRoot(rootResolved, res)) return res;
    return defaultPath;
  }
  return defaultPath;
}
