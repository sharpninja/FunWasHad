/**
 * CR-EXT-1.5.1: Unit tests for parseCliBlocks, extractPromptFromPromptsSection,
 * isPromptCommand, removeCliBlock, pathsEqual, isUnderWorkspaceRoot.
 */

import { strict as assert } from 'assert';
import {
  CLI_BLOCK_RE,
  parseCliBlocks,
  extractPromptFromPromptsSection,
  isPromptCommand,
  removeCliBlock,
  pathsEqual,
  isUnderWorkspaceRoot,
  parsePromptsMd,
  listPromptNamesFromCliMd,
} from './parser';

describe('parser', () => {
  describe('CLI_BLOCK_RE', () => {
    it('matches ```cli block with content', () => {
      const s = 'x\n```cli\nfoo\n```\ny';
      CLI_BLOCK_RE.lastIndex = 0;
      const m = CLI_BLOCK_RE.exec(s);
      assert.ok(m);
      assert.strictEqual(m![0], '```cli\nfoo\n```');
      assert.strictEqual(m![1], 'foo');
    });

    it('matches multiple blocks', () => {
      CLI_BLOCK_RE.lastIndex = 0;
      const s = '```cli\na\n```\n```cli\nb\n```';
      const blocks: string[] = [];
      let m: RegExpExecArray | null;
      while ((m = CLI_BLOCK_RE.exec(s)) !== null) blocks.push(m[1]);
      assert.deepStrictEqual(blocks, ['a', 'b']);
    });
  });

  describe('parseCliBlocks', () => {
    it('returns empty for no cli blocks', () => {
      assert.deepStrictEqual(parseCliBlocks('hello\nworld'), []);
      assert.deepStrictEqual(parseCliBlocks('```js\ncode\n```'), []);
    });

    it('returns one block with fullMatch, command, index', () => {
      const c = 'pre\n```cli\nprompt x\n```\npost';
      const b = parseCliBlocks(c);
      assert.strictEqual(b.length, 1);
      assert.strictEqual(b[0].fullMatch, '```cli\nprompt x\n```');
      assert.strictEqual(b[0].command, 'prompt x');
      assert.strictEqual(b[0].index, 4);
    });

    it('skips empty command', () => {
      const c = '```cli\n   \n```';
      assert.deepStrictEqual(parseCliBlocks(c), []);
    });

    it('returns multiple blocks with correct indices', () => {
      const c = 'a\n```cli\nprompt p1\n```\nb\n```cli\nprompt p2\n```';
      const b = parseCliBlocks(c);
      assert.strictEqual(b.length, 2);
      assert.strictEqual(b[0].command, 'prompt p1');
      assert.strictEqual(b[1].command, 'prompt p2');
      assert.ok(b[0].index < b[1].index);
    });
  });

  describe('extractPromptFromPromptsSection', () => {
    it('returns prompt body when found', () => {
      const content = [
        '## Prompts',
        '### Prompt: code-review (File, Feature)',
        '```prompt',
        'Review {File} for {Feature}.',
        '```',
      ].join('\n');
      const r = extractPromptFromPromptsSection(content, 'code-review');
      assert.strictEqual(r, 'Review {File} for {Feature}.');
    });

    it('returns null when prompt name not found', () => {
      const content = '### Prompt: other (x)\n```prompt\nbody\n```';
      assert.strictEqual(extractPromptFromPromptsSection(content, 'code-review'), null);
    });

    it('escapes regex-special chars in prompt name', () => {
      const content = '### Prompt: a.b (x)\n```prompt\nok\n```';
      assert.strictEqual(extractPromptFromPromptsSection(content, 'a.b'), 'ok');
    });

    it('is case-insensitive for header', () => {
      const content = '### PROMPT: x ()\n```prompt\nbody\n```';
      assert.strictEqual(extractPromptFromPromptsSection(content, 'x'), 'body');
    });
  });

  describe('isPromptCommand', () => {
    it('returns { name } for "prompt <name>"', () => {
      assert.deepStrictEqual(isPromptCommand('prompt code-review'), { name: 'code-review' });
      assert.deepStrictEqual(isPromptCommand('  prompt   x  '), { name: 'x' });
    });

    it('returns null for non-prompt', () => {
      assert.strictEqual(isPromptCommand('help'), null);
      assert.strictEqual(isPromptCommand('list'), null);
      assert.strictEqual(isPromptCommand('prompt'), null);
    });

    it('returns null for empty or only prompt', () => {
      assert.strictEqual(isPromptCommand('prompt'), null);
      assert.strictEqual(isPromptCommand('prompt   '), null);
    });
  });

  describe('removeCliBlock', () => {
    it('removes block by index', () => {
      const c = 'a\n```cli\nx\n```\nb';
      const block = '```cli\nx\n```';
      const idx = 2;
      const r = removeCliBlock(c, block, idx);
      assert.strictEqual(r, 'a\n\nb');
    });

    it('handles duplicate fullMatch by using index', () => {
      const c = '```cli\nx\n```\n```cli\nx\n```';
      const block = '```cli\nx\n```';
      const r1 = removeCliBlock(c, block, 0);
      assert.strictEqual(r1, '\n```cli\nx\n```');
      const r2 = removeCliBlock(r1, block, 1);
      assert.strictEqual(r2, '\n');
    });
  });

  describe('pathsEqual', () => {
    it('same path is equal', () => {
      assert.strictEqual(pathsEqual('/a/b', '/a/b'), true);
    });

    it('resolved equivalent is equal', () => {
      assert.strictEqual(pathsEqual('/a/b/../b', '/a/b'), true);
    });

    it('different path is not equal', () => {
      assert.strictEqual(pathsEqual('/a', '/b'), false);
    });

    it('on win32, case-insensitive', function () {
      if (process.platform !== 'win32') this.skip();
      assert.strictEqual(pathsEqual('C:\\A\\B', 'c:\\a\\b'), true);
    });
  });

  describe('isUnderWorkspaceRoot', () => {
    it('child path is under', () => {
      assert.strictEqual(isUnderWorkspaceRoot('/ws', '/ws/foo'), true);
      assert.strictEqual(isUnderWorkspaceRoot('/ws', '/ws/a/b'), true);
    });

    it('path with .. escaping root or absolute outside is not under', () => {
      assert.strictEqual(isUnderWorkspaceRoot('/ws', '/ws/../other'), false);
      assert.strictEqual(isUnderWorkspaceRoot('/ws', '/other'), false);
    });

    it('workspace root itself is under', () => {
      assert.strictEqual(isUnderWorkspaceRoot('/ws', '/ws'), true);
    });
  });

  describe('parsePromptsMd (MVP-SUPPORT-005)', () => {
    it('parses one prompt with parameters', () => {
      const md = [
        '## foo',
        'Do {X} with {Y}.',
        '### Parameters',
        '| Parameter | Description | Required | Default |',
        '|-----------|-------------|----------|---------|',
        '| X | thing | Yes | |',
        '| Y | other | No | y-default |',
      ].join('\n');
      const r = parsePromptsMd(md);
      assert.strictEqual(r.length, 1);
      assert.strictEqual(r[0].name, 'foo');
      assert.ok(r[0].template.includes('{X}') && r[0].template.includes('{Y}'));
      assert.strictEqual(r[0].parameters.length, 2);
      assert.strictEqual(r[0].parameters[0].name, 'X');
      assert.strictEqual(r[0].parameters[0].required, true);
      assert.strictEqual(r[0].parameters[1].default, 'y-default');
    });

    it('skips blocks without ## name', () => {
      const md = '# Title\n\nNo ## here.\n\n---\n\n## ok\nBody.';
      const r = parsePromptsMd(md);
      assert.strictEqual(r.length, 1);
      assert.strictEqual(r[0].name, 'ok');
    });

    it('extracts placeholders not in Parameters table', () => {
      const md = '## p\nText {A} and {B}.\n### Parameters\n| Parameter | Description | Required | Default |\n| A | a | No | |';
      const r = parsePromptsMd(md);
      assert.strictEqual(r.length, 1);
      const names = r[0].parameters.map((x) => x.name).sort();
      assert.deepStrictEqual(names, ['A', 'B']);
    });
  });

  describe('listPromptNamesFromCliMd (MVP-SUPPORT-005)', () => {
    it('returns unique names from ### Prompt: name (date)', () => {
      const c = '### Prompt: code-review (2026-01-01)\n```prompt\nx\n```\n### Prompt: foo (x)\n```\n### Prompt: code-review (2026-01-02)';
      const r = listPromptNamesFromCliMd(c);
      assert.deepStrictEqual(r.sort(), ['code-review', 'foo']);
    });

    it('returns empty when no matches', () => {
      assert.deepStrictEqual(listPromptNamesFromCliMd('## Prompts\n\nNothing here.'), []);
    });
  });
});
