import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { getDocumentSymbols, JoltSymbol } from '../language/symbols';

function outline(symbols: JoltSymbol[]): unknown[] {
	return symbols.map(x => x.children.length > 0 ? [x.name, x.kind, outline(x.children)] : [x.name, x.kind]);
}

test('properties and array items are nested as in the document', () => {
	const text = JSON.stringify({ a: { b: 1, c: [true, null] }, d: 'text' });

	assert.deepEqual(outline(getDocumentSymbols(text)), [
		['a', 'object', [['b', 'number'], ['c', 'array', [['[0]', 'boolean'], ['[1]', 'null']]]]],
		['d', 'string']
	]);
});

test('method calls and variable declarations in property names are marked', () => {
	const text = JSON.stringify({ '#foreach(@x in $.items)': [], ' @total ': '#sum($.a)', 'plain': '#valueOf($.a)' });

	assert.deepEqual(getDocumentSymbols(text).map(x => x.kind), ['method', 'variable', 'string']);
});

test('the detail summarizes primitive values', () => {
	const long = 'x'.repeat(100);
	const symbols = getDocumentSymbols(JSON.stringify({ a: '#valueOf($.a)', b: 2, c: {}, d: long }));

	assert.deepEqual(symbols.slice(0, 3).map(x => x.detail), ['#valueOf($.a)', '2', '']);
	assert.equal(symbols[3].detail.length, 60);
	assert.ok(symbols[3].detail.endsWith('…'));
});

test('a property spans its name and value, and selects its name', () => {
	const text = '{ "name": "value" }';
	const [symbol] = getDocumentSymbols(text);

	assert.equal(text.substr(symbol.offset, symbol.length), '"name": "value"');
	assert.equal(text.substr(symbol.selectionOffset, symbol.selectionLength), '"name"');
});

test('incomplete documents still produce an outline', () => {
	assert.deepEqual(getDocumentSymbols('{ "a": { "b": ').map(x => x.name), ['a']);
	assert.deepEqual(getDocumentSymbols(''), []);
});
