/**
 * CR-EXT-1.5.1: Unit tests for resolveCliMdPath and resolveExecuteOptions (mocked config).
 */

import { strict as assert } from 'assert';
import * as path from 'path';
import { resolveCliMdPath, resolveExecuteOptions } from './resolver';

describe('resolver', () => {
  const ws = path.resolve('/workspace');

  describe('resolveCliMdPath', () => {
    it('default when no config or vsc', () => {
      assert.strictEqual(resolveCliMdPath(ws, undefined, undefined), path.join(ws, 'CLI.md'));
    });

    it('uses config CliMdPath when under root', () => {
      assert.strictEqual(resolveCliMdPath(ws, 'out/CLI.md', undefined), path.join(ws, 'out', 'CLI.md'));
      assert.strictEqual(resolveCliMdPath(ws, 'CLI.md', 'other.md'), path.join(ws, 'CLI.md'));
    });

    it('uses vsc when config not set', () => {
      assert.strictEqual(resolveCliMdPath(ws, undefined, 'docs/CLI.md'), path.join(ws, 'docs', 'CLI.md'));
    });

    it('rejects path outside workspace and returns default', () => {
      const outside = path.resolve(ws, '..', 'other', 'CLI.md');
      assert.strictEqual(resolveCliMdPath(ws, outside, undefined), path.join(ws, 'CLI.md'));
      assert.strictEqual(resolveCliMdPath(ws, undefined, '/absolute/outside'), path.join(ws, 'CLI.md'));
    });

    it('accepts absolute path when under workspace', () => {
      const under = path.join(ws, 'nested', 'CLI.md');
      assert.strictEqual(resolveCliMdPath(ws, under, undefined), under);
    });
  });

  describe('resolveExecuteOptions', () => {
    it('default when no config or vsc', () => {
      assert.deepStrictEqual(resolveExecuteOptions(undefined, undefined, undefined, undefined), {
        mode: 'composer',
        composerCommand: 'composer.new',
      });
    });

    it('uses config ExecuteMode and ComposerCommand', () => {
      assert.deepStrictEqual(
        resolveExecuteOptions('agent-cli', 'aichat.new', undefined, undefined),
        { mode: 'agent-cli', composerCommand: 'aichat.new' }
      );
    });

    it('uses vsc when config not set', () => {
      assert.deepStrictEqual(
        resolveExecuteOptions(undefined, undefined, 'agent-cli', 'aichat.new'),
        { mode: 'agent-cli', composerCommand: 'aichat.new' }
      );
    });

    it('config overrides vsc', () => {
      assert.deepStrictEqual(
        resolveExecuteOptions('agent-cli', 'cmd.a', 'composer', 'cmd.b'),
        { mode: 'agent-cli', composerCommand: 'cmd.a' }
      );
    });
  });
});
