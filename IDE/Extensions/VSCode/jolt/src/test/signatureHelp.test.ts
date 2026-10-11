import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { getCallContext } from '../language/signatureHelp';

test('no call context outside of a call', () => {
	assert.equal(getCallContext('$.a.b'), undefined);
	assert.equal(getCallContext('#valueOf($.a)'), undefined);
	assert.equal(getCallContext('#valueOf'), undefined);
});

test('the first argument is active after the opening parenthesis', () => {
	assert.deepEqual(getCallContext('#valueOf('), { name: 'valueOf', isPiped: false, argumentIndex: 0 });
});

test('commas advance the active argument', () => {
	assert.deepEqual(getCallContext('#substring($.a, 1, '), { name: 'substring', isPiped: false, argumentIndex: 2 });
});

test('a piped call is recognized', () => {
	assert.deepEqual(getCallContext('$.a->#take(3, '), { name: 'take', isPiped: true, argumentIndex: 1 });
	assert.deepEqual(getCallContext('$.a -> #take('), { name: 'take', isPiped: true, argumentIndex: 0 });
});

test('the innermost unclosed call is used', () => {
	assert.deepEqual(getCallContext('#outer($.a, #inner('), { name: 'inner', isPiped: false, argumentIndex: 0 });
	assert.deepEqual(getCallContext('#outer(#inner($.a, $.b), '), { name: 'outer', isPiped: false, argumentIndex: 1 });
});

test('commas within literals and nested brackets do not separate arguments', () => {
	assert.equal(getCallContext(`#join('a, b', `)?.argumentIndex, 1);
	// Within the JSON, \\' is a backslash followed by a quote, which escapes the quote within the literal.
	assert.equal(getCallContext(`#join('it\\\\'s, here', `)?.argumentIndex, 1);
	assert.equal(getCallContext(`#join('ends with \\\\\\\\', `)?.argumentIndex, 1);
	assert.equal(getCallContext('#f([1, 2, 3], ')?.argumentIndex, 1);
	assert.equal(getCallContext(`#f({ 'a': 1, 'b': 2 }, `)?.argumentIndex, 1);
	assert.equal(getCallContext('#f((1, 2), ')?.argumentIndex, 1);
});

test('the call is found from within a nested bracket in its arguments', () => {
	assert.deepEqual(getCallContext('#f($.a, [1, '), { name: 'f', isPiped: false, argumentIndex: 1 });
});

test('a lambda argument does not hide the call', () => {
	assert.deepEqual(getCallContext('#orderBy($.items, @x: @x.'), { name: 'orderBy', isPiped: false, argumentIndex: 1 });
});
