import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { applyEdits } from 'jsonc-parser';
import { FormattingOptions, getFormattingEdits, OffsetRange } from '../language/formatting';

const options: FormattingOptions = { tabSize: 2, insertSpaces: true, eol: '\n' };

function format(text: string, range?: OffsetRange, formatting = options): string {
	return applyEdits(text, getFormattingEdits(text, range, formatting));
}

test('a document is formatted as JSON without changing its expressions', () => {
	const text = `{"#foreach(@x in $.items)":[{"name":"@x.name  ->  #toUpperCase()"}],"total":"#sum( $.a , $.b )"}`;

	assert.equal(format(text), [
		'{',
		'  "#foreach(@x in $.items)": [',
		'    {',
		'      "name": "@x.name  ->  #toUpperCase()"',
		'    }',
		'  ],',
		'  "total": "#sum( $.a , $.b )"',
		'}'
	].join('\n'));
});

test('the indentation and line endings follow the options', () => {
	assert.equal(format('{"a":1}', undefined, { tabSize: 4, insertSpaces: false, eol: '\r\n' }), '{\r\n\t"a": 1\r\n}');
});

test('only the given range is formatted', () => {
	const text = '{"a":{"b":1},   "c":{"d":2}}';
	const start = text.indexOf('{"d"');

	assert.equal(format(text, { offset: start, length: '{"d":2}'.length }).slice(0, start), text.slice(0, start));
});
