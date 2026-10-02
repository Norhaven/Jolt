import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { getExpressionContext } from '../language/context';

const cursor = '¦';

function contextAt(textWithCursor: string) {
	const offset = textWithCursor.indexOf(cursor);

	return getExpressionContext(textWithCursor.replace(cursor, ''), offset);
}

test('property key is a property name target', () => {
	assert.deepEqual(contextAt('{ "#fore¦": 1 }'), { target: 'propertyName', textBeforeCursor: '#fore' });
});

test('string value is a property value target', () => {
	assert.deepEqual(contextAt('{ "a": "#val¦" }'), { target: 'propertyValue', textBeforeCursor: '#val' });
});

test('method arguments within a property key are a property value target', () => {
	assert.equal(contextAt('{ "#foreach(@x in #¦) into \'r\'": [] }')?.target, 'propertyValue');
});

test('parentheses within string literals do not count as method arguments', () => {
	assert.equal(contextAt('{ "\'(\' #¦": 1 }')?.target, 'propertyName');
});

test('strings within a #using array are a statement block target', () => {
	assert.equal(contextAt('{ "#using($.a as @x) into \'r\'": [ "#¦" ] }')?.target, 'statementBlock');
});

test('method arguments within a statement are a property value target', () => {
	assert.equal(contextAt('{ "#using($.a as @x) into \'r\'": [ "#when(#¦)" ] }')?.target, 'propertyValue');
});

test('keys within a #match case list are a match block target', () => {
	assert.equal(contextAt('{ "#match($.a as @x) into \'r\'": [ { "#¦": "\'a\'" } ] }')?.target, 'matchBlock');
});

test('strings within an ordinary array are a property value target', () => {
	assert.equal(contextAt('{ "a": [ "#¦" ] }')?.target, 'propertyValue');
});

test('nested transformers inside a test file are detected', () => {
	assert.equal(contextAt('{ "tests": [ { "transformer": { "#¦": 1 } } ] }')?.target, 'propertyName');
});

test('unterminated string while typing is still detected', () => {
	assert.deepEqual(contextAt('{ "a": "#to¦\n}'), { target: 'propertyValue', textBeforeCursor: '#to' });
});

test('cursor outside of a string has no context', () => {
	assert.equal(contextAt('{ "a": 1¦ }'), undefined);
});

test('cursor just after a closing quote has no context', () => {
	assert.equal(contextAt('{ "a"¦: 1 }'), undefined);
});

test('cursor just before an opening quote has no context', () => {
	assert.equal(contextAt('{ ¦"a": 1 }'), undefined);
});
