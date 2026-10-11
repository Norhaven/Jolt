import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { applyEdits, parse } from 'jsonc-parser';
import { addToMethodsFile, getDeclarationInsertion, getSimilarNames, inferParameters, MethodsFileEntry } from '../language/quickFixes';

const entry: MethodsFileEntry = { name: 'custom', description: '', parameters: [{ name: 'arg1', kind: 'value' }] };

test('similar names are misspellings and case differences, closest first', () => {
	const candidates = ['valueOf', 'valuesOf', 'toUpperCase', 'toLowerCase', 'count'];

	assert.deepEqual(getSimilarNames('valeuOf', candidates), ['valueOf']);
	assert.deepEqual(getSimilarNames('ValueOf', candidates), ['valueOf', 'valuesOf']);
	assert.deepEqual(getSimilarNames('valuOf', candidates), ['valueOf']);
	assert.deepEqual(getSimilarNames('toUppercase', candidates), ['toUpperCase']);
	assert.deepEqual(getSimilarNames('somethingElse', candidates), []);
});

test('a name is not similar to itself, and short names allow one difference', () => {
	assert.deepEqual(getSimilarNames('x', ['x', 'y', 'xy', 'abc']), ['xy', 'y']);
});

test('longer names allow a difference for every four characters', () => {
	assert.deepEqual(getSimilarNames('selectMny', ['selectMany', 'select']), ['selectMany']);
	assert.deepEqual(getSimilarNames('slctMny', ['selectMany']), []);
});

test('parameters are inferred from the call arguments', () => {
	assert.deepEqual(inferParameters('($.a, 2)'), [{ name: 'arg1', kind: 'value' }, { name: 'arg2', kind: 'value' }]);
	assert.deepEqual(inferParameters('()'), []);
	assert.deepEqual(inferParameters(''), []);
	assert.deepEqual(inferParameters(' + 1'), []);
});

test('lambda arguments become lambda parameters binding the same variables', () => {
	assert.deepEqual(inferParameters('($.a, @x: @x.b, @acc;@cur: @acc + @cur)'), [
		{ name: 'arg1', kind: 'value' },
		{ name: 'arg2', kind: 'lambda', lambdaVariables: ['x'] },
		{ name: 'arg3', kind: 'lambda', lambdaVariables: ['acc', 'cur'] }
	]);
});

test('commas within nested calls, literals and brackets do not separate arguments', () => {
	assert.equal(inferParameters(`(#f(1, 2), 'a, b', [1, 2], { 'k': 1, 'j': 2 }) + #g(3, 4)`).length, 4);
});

test('an unterminated call infers the arguments written so far', () => {
	assert.equal(inferParameters('($.a, $.b').length, 2);
});

test('an entry is appended to an existing methods array, keeping the indentation', () => {
	const text = '{\n  "methods": [\n    { "name": "first" }\n  ]\n}\n';
	const change = addToMethodsFile(text, entry)!;

	assert.deepEqual(parse(change.newText).methods.map((x: MethodsFileEntry) => x.name), ['first', 'custom']);
	assert.ok(change.newText.includes('\n      "name": "custom"'));
	assert.equal(change.newText.substr(change.nameOffset, 'custom'.length), 'custom');
	assert.equal(applyEdits(text, change.edits), change.newText);
});

test('a methods array is added when the file has none', () => {
	const change = addToMethodsFile('{\n\t"root": true\n}', entry)!;

	assert.deepEqual(parse(change.newText), { root: true, methods: [entry] });
	assert.ok(change.newText.includes('\n\t"methods"'));
});

test('a new methods file is created with the entry', () => {
	const change = addToMethodsFile(undefined, entry)!;

	assert.deepEqual(parse(change.newText), { methods: [entry] });
	assert.equal(change.newText.substr(change.nameOffset, 'custom'.length), 'custom');
});

test('an invalid methods file is not changed', () => {
	assert.equal(addToMethodsFile('{ "methods": [', entry), undefined);
	assert.equal(addToMethodsFile('[]', entry), undefined);
});

function declare(text: string, reference: string, name = 'x') {
	const insertion = getDeclarationInsertion(text, text.indexOf(reference), name);

	return insertion && text.slice(0, insertion.offset) + insertion.text + text.slice(insertion.offset);
}

test('a declaration is inserted before the property containing the reference, on its own line', () => {
	const text = '{\n    "a": 1,\n    "b": "@x + 1"\n}';

	assert.equal(declare(text, '@x'), '{\n    "a": 1,\n    "@x": "",\n    "b": "@x + 1"\n}');
});

test('a declaration shares the line of a property that shares one', () => {
	assert.equal(declare('{ "a": 1, "b": "@x" }', '@x'), '{ "a": 1, "@x": "", "b": "@x" }');
});

test('a declaration for a reference in a property name goes before that property', () => {
	assert.equal(
		declare('{ "#foreach(@y in @x) into \'r\'": [] }', '@x'),
		'{ "@x": "", "#foreach(@y in @x) into \'r\'": [] }');
});

test('a declaration for a reference within a #match case goes before the #match property', () => {
	assert.equal(
		declare('{ "#match($.a) into \'r\'": [ { "#default()": "@x" } ] }', '@x'),
		'{ "@x": "", "#match($.a) into \'r\'": [ { "#default()": "@x" } ] }');
});

test('line endings are kept for a declaration on its own line', () => {
	assert.equal(declare('{\r\n  "b": "@x"\r\n}', '@x'), '{\r\n  "@x": "",\r\n  "b": "@x"\r\n}');
});

test('no declaration without an enclosing property', () => {
	assert.equal(declare('"@x"', '@x'), undefined);
});
