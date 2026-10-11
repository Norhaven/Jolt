import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { getMethodReferenceAt } from '../language/hover';

const text = `{ "result": "#valueOf($.a)->#toUpperCase()" }`;

test('finds the method under the cursor', () => {
	const start = text.indexOf('#valueOf');

	assert.deepEqual(getMethodReferenceAt(text, text.indexOf('valueOf') + 2), { name: 'valueOf', start, end: start + '#valueOf'.length });
});

test('finds a piped method', () => {
	assert.equal(getMethodReferenceAt(text, text.indexOf('toUpperCase'))?.name, 'toUpperCase');
});

test('the hash and the end of the name are part of the reference', () => {
	assert.equal(getMethodReferenceAt(text, text.indexOf('#valueOf'))?.name, 'valueOf');
	assert.equal(getMethodReferenceAt(text, text.indexOf('('))?.name, 'valueOf');
});

test('no reference outside of a method name', () => {
	assert.equal(getMethodReferenceAt(text, text.indexOf('$.a') + 1), undefined);
	assert.equal(getMethodReferenceAt(text, 4), undefined);
});

test('no reference outside of a JSON string', () => {
	const outside = '{ "a": "#valueOf($.a)", "b": 1 }';

	assert.equal(getMethodReferenceAt(outside, outside.indexOf('1')), undefined);
});

test('a method name within a string literal is not a reference', () => {
	const literal = `{ "a": "#contains($.tags, '#count') && #count($.b) > 1" }`;

	assert.equal(getMethodReferenceAt(literal, literal.indexOf('count')), undefined);
	assert.equal(getMethodReferenceAt(literal, literal.lastIndexOf('count'))?.name, 'count');
});

test('an escaped quote within a string literal does not end it', () => {
	const literal = `{ "a": "#concat('it\\\\'s #count', $.b)" }`;

	assert.equal(getMethodReferenceAt(literal, literal.indexOf('count')), undefined);
});
