/**
 * CR-EXT-1.5.2: Tests for runInComposer and runWithAgentCli with mocked
 * executeCommand, clipboardWrite, spawn. Asserts args and error handling.
 */

import { strict as assert } from 'assert';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { runInComposer, runWithAgentCli } from './runner';

describe('runner', () => {
  describe('runInComposer', () => {
    it('calls executeCommand with composerCommand and clipboardWrite with promptText', async () => {
      const executed: string[] = [];
      const clipped: string[] = [];
      const output = { append: () => {}, appendLine: () => {} };

      await runInComposer(
        {
          executeCommand: async (cmd) => { executed.push(cmd); },
          clipboardWrite: async (t) => { clipped.push(t); },
          showWarning: () => {},
          showInfo: () => {},
        },
        'the prompt',
        'composer.new',
        output
      );

      assert.deepStrictEqual(executed, ['composer.new']);
      assert.deepStrictEqual(clipped, ['the prompt']);
    });

    it('still calls clipboardWrite when executeCommand throws', async () => {
      const clipped: string[] = [];
      const output = { append: () => {}, appendLine: () => {} };

      await runInComposer(
        {
          executeCommand: async () => { throw new Error('Composer failed'); },
          clipboardWrite: async (t) => { clipped.push(t); },
          showWarning: () => {},
          showInfo: () => {},
        },
        'prompt text',
        'composer.new',
        output
      );

      assert.deepStrictEqual(clipped, ['prompt text']);
    });
  });

  describe('runWithAgentCli', () => {
    it('spawns with correct executable and args (cwd, shell, agent command)', async () => {
      const isWin = process.platform === 'win32';
      let spawnCmd: string | undefined;
      let spawnArgs: string[] | undefined;
      let spawnOpts: { cwd: string } | undefined;

      const mockSpawn = (cmd: string, args: string[], opts: { cwd: string }) => {
        spawnCmd = cmd;
        spawnArgs = args;
        spawnOpts = opts;
        const handlers: Record<string, (arg?: unknown) => void> = {};
        const proc = {
          stdout: { setEncoding: () => {}, on: () => {} },
          stderr: { setEncoding: () => {}, on: () => {} },
          on: (ev: string, cb: (arg?: unknown) => void) => { handlers[ev] = cb; return proc; },
        };
        setTimeout(() => handlers['close']?.(undefined), 0);
        return proc as unknown as ReturnType<typeof import('child_process').spawn>;
      };

      const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'fwh-runner-'));
      try {
        const out = { append: () => {}, appendLine: () => {} };
        await runWithAgentCli(
          { spawn: mockSpawn },
          'my prompt',
          tmp,
          out
        );

        assert.strictEqual(spawnCmd, isWin ? 'powershell' : 'sh');
        assert.ok(Array.isArray(spawnArgs));
        if (isWin) {
          assert.strictEqual(spawnArgs![0], '-NoProfile');
          assert.strictEqual(spawnArgs![1], '-ExecutionPolicy');
          assert.strictEqual(spawnArgs![2], 'Bypass');
          assert.strictEqual(spawnArgs![3], '-Command');
          assert.ok(spawnArgs![4].includes('agent -p'));
          assert.ok(spawnArgs![4].includes('--output-format text'));
        } else {
          assert.strictEqual(spawnArgs![0], '-c');
          assert.ok(spawnArgs![1].includes('agent -p'));
          assert.ok(spawnArgs![1].includes('--output-format text'));
        }
        assert.strictEqual(spawnOpts!.cwd, tmp);
      } finally {
        try { fs.rmSync(tmp, { recursive: true }); } catch { /* ignore */ }
      }
    });

    it('rejects when spawn emits error', async () => {
      const err = new Error('spawn ENOENT');
      const mockSpawn = () => {
        const p = {
          stdout: { setEncoding: () => {}, on: () => {} },
          stderr: { setEncoding: () => {}, on: () => {} },
          on: (ev: string, cb: (e?: Error) => void) => {
            if (ev === 'error') setTimeout(() => cb(err), 0);
            return p;
          },
        } as unknown as ReturnType<typeof import('child_process').spawn>;
        return p;
      };

      const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'fwh-runner2-'));
      try {
        await assert.rejects(
          async () =>
            runWithAgentCli(
              { spawn: mockSpawn },
              'p',
              tmp,
              { append: () => {}, appendLine: () => {} }
            ),
          /spawn ENOENT/
        );
      } finally {
        try { fs.rmSync(tmp, { recursive: true }); } catch { /* ignore */ }
      }
    });
  });
});
