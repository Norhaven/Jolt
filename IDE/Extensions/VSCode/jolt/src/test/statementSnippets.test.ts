import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { getPropertyPosition, statementSnippets } from '../language/statementSnippets';

const cursor = '¦';

function positionAt(textWithCursor: string) {
	return getPropertyPosition(textWithCursor.replace(cursor, ''), textWithCursor.indexOf(cursor));
}

test('offered where a property name would go', () => {
	assert.deepEqual(positionAt('{ ¦ }'), { replaceLength: 0, replaceAfterLength: 0, labelPrefix: '', leadingCommaOffset: undefined, needsTrailingComma: false });
	assert.ok(positionAt('{\n  "a": 1,\n  ¦\n}'));
	assert.ok(positionAt('{ "a": { ¦ } }'));
	assert.ok(positionAt('[ { ¦ } ]'));
});

test('a partially typed label is replaced', () => {
	assert.equal(positionAt('{ #fore¦ }')?.replaceLength, 5);
	assert.equal(positionAt('{ "a": 1, @va¦ }')?.replaceLength, 3);
	assert.equal(positionAt('{ fore¦ }')?.replaceLength, 4);
});

test('a property name being typed is replaced, quotes included', () => {
	assert.deepEqual(positionAt('{ "#fore¦" }'), { replaceLength: 6, replaceAfterLength: 1, labelPrefix: '"', leadingCommaOffset: undefined, needsTrailingComma: false });
	assert.equal(positionAt('{ "#¦" }')?.replaceLength, 2);
	assert.equal(positionAt('{ "@na¦" }')?.replaceLength, 4);
});

test('a colon typed after the property name is replaced with it', () => {
	assert.equal(positionAt('{ "#fore¦": }')?.replaceAfterLength, 2);
	assert.equal(positionAt('{ "#fore¦" : }')?.replaceAfterLength, 3);
});

test('commas are added around a property name being typed', () => {
	const text = '{\n  "a": 1\n  "#fore¦"\n}';

	assert.equal(positionAt(text)?.leadingCommaOffset, text.indexOf('1') + 1);
	assert.equal(positionAt('{ "a": 1, "#fore¦" }')?.leadingCommaOffset, undefined);
	assert.equal(positionAt('{ "#fore¦" "b": 1 }')?.needsTrailingComma, true);
});

test('not offered within a property name that has a value, or more than a partial label', () => {
	assert.equal(positionAt('{ "#fore¦": 1 }'), undefined);
	assert.equal(positionAt('{ "#foreach(@x in $.a¦" }'), undefined);
	assert.equal(positionAt('{ "#fore¦ach" }'), undefined);
	assert.equal(positionAt('{ "plain¦" }'), undefined);
});

test('not offered within values, arrays, or outside of an object', () => {
	assert.equal(positionAt('{ "a": "#val¦" }'), undefined);
	assert.equal(positionAt('{ "a": ¦ }'), undefined);
	assert.equal(positionAt('{ "a": [ ¦ ] }'), undefined);
	assert.equal(positionAt('¦'), undefined);
});

test('a comma is added after the previous value when it has none', () => {
	const text = '{\n  "a": 1\n  ¦\n}';

	assert.equal(positionAt(text)?.leadingCommaOffset, text.indexOf('1') + 1);
	assert.equal(positionAt('{ "a": { "b": 2 } #fore¦ }')?.leadingCommaOffset, '{ "a": { "b": 2 }'.length);
	assert.equal(positionAt('{ "a": "x" ¦ }')?.leadingCommaOffset, '{ "a": "x"'.length);
});

test('a comma is added after the snippet when a property follows it', () => {
	assert.equal(positionAt('{ ¦ "a": 1 }')?.needsTrailingComma, true);
	assert.equal(positionAt('{\n  ¦\n  "a": 1\n}')?.needsTrailingComma, true);
	assert.equal(positionAt('{ "a": 1, ¦ }')?.needsTrailingComma, false);
});

test('each snippet inserts a single property', () => {
	for (const snippet of statementSnippets) {
		const text = snippet.body
			.replace(/\$\{\d+:((?:[^}\\]|\\.)*)\}/g, '$1')
			.replace(/\\(.)/g, '$1');

		assert.equal(Object.keys(JSON.parse(`{ ${text} }`)).length, 1, snippet.label);
	}
});
