import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { getVariableCompletions } from '../language/variables';

const cursor = '¦';

function completionsAt(textWithCursor: string) {
	const offset = textWithCursor.indexOf(cursor);

	return getVariableCompletions(textWithCursor.replace(cursor, ''), offset);
}

function namesAt(textWithCursor: string) {
	return completionsAt(textWithCursor)?.variables.map(x => x.name);
}

test('no completions unless a variable is being written', () => {
	assert.equal(completionsAt('{ "a": "#valueOf(¦" }'), undefined);
	assert.equal(completionsAt('{ "a": 1¦ }'), undefined);
});

test('the @ and any partial name are replaced', () => {
	assert.equal(completionsAt('{ "a": "@¦" }')?.replaceLength, 1);
	assert.equal(completionsAt('{ "a": "#exists(@fir¦" }')?.replaceLength, 4);
});

test('the implicit @params variable is always offered', () => {
	assert.deepEqual(namesAt('{ "a": "@¦" }'), ['params']);
});

test('declarations in earlier properties are in scope but later ones are not', () => {
	assert.deepEqual(namesAt('{ "@a": "1", "@b": "2", "c": "@¦", "@d": "3" }'), ['b', 'a', 'params']);
});

test('a declaration is not in scope within its own value', () => {
	assert.deepEqual(namesAt('{ "@a": "@¦" }'), ['params']);
});

test('loop variables are in scope within the loop template', () => {
	const text = '{ "#foreach(@x;@i in $.a) into \'r\'": [ { "v": "@¦" } ] }';

	assert.deepEqual(completionsAt(text)?.variables.slice(0, 2), [
		{ name: 'x', description: 'loop item (#foreach)' },
		{ name: 'i', description: 'loop index (#foreach)' }
	]);
});

test('a loop index declared without @ is still recognized', () => {
	assert.deepEqual(namesAt('{ "#foreach(@x;i in $.a) into \'r\'": [ { "v": "@¦" } ] }'), ['x', 'i', 'params']);
});

test('loop variables are not in scope after the loop, but its into variable is', () => {
	const text = '{ "#foreach(@x in $.a) into @temp": [ { "v": "1" } ], "b": "@¦" }';

	assert.deepEqual(completionsAt(text)?.variables[0], { name: 'temp', description: 'result of #foreach' });
	assert.ok(!namesAt(text)?.includes('x'));
});

test('variables declared within a loop template are scoped to it', () => {
	assert.deepEqual(namesAt('{ "#foreach(@x in $.a) into \'r\'": [ { "@s": "1", "v": "@¦" } ] }'), ['s', 'x', 'params']);
	assert.deepEqual(namesAt('{ "#foreach(@x in $.a) into \'r\'": [ { "@s": "1" } ], "w": "@¦" }'), ['params']);
});

test('the #using variable is in scope within its statements', () => {
	assert.deepEqual(namesAt('{ "#using($.a as @x) into \'r\'": [ "#setAt(@¦" ] }'), ['x', 'params']);
});

test('the #match variable and pattern captures are in scope within the case value', () => {
	assert.deepEqual(
		namesAt('{ "#match($.a as @x) into \'r\'": [ { "#is({ \'n\': @name, \'v\': [@first, _] })": "@¦" } ] }'),
		['name', 'first', 'x', 'params']);
});

test('pattern captures are in scope later within the same case key', () => {
	assert.deepEqual(
		namesAt('{ "#match($.a as @x) into \'r\'": [ { "#is({ \'f\': @f }) && #given(#isObject(@¦": 1 } ] }'),
		['f', 'x', 'params']);
});

test('pattern captures from other cases are not in scope', () => {
	assert.deepEqual(
		namesAt('{ "#match($.a as @x) into \'r\'": [ { "#is({ \'f\': @f })": "1" }, { "#default()": "@¦" } ] }'),
		['x', 'params']);
});

test('lambda parameters are in scope within the lambda body only', () => {
	assert.deepEqual(namesAt('{ "a": "#select($.a, @y: @¦" }'), ['y', 'params']);
	assert.deepEqual(namesAt('{ "a": "#select($.a, @y: @y) + @¦" }'), ['params']);
	assert.deepEqual(namesAt('{ "a": "#reduce($.a, @acc;@cur: @acc + @cur, @¦" }'), ['params']);
});

test('nested lambda parameters are offered innermost first', () => {
	assert.deepEqual(
		namesAt('{ "a": "#zip($.a, $.b, @x;@y: { \'k\': #select(@x, @z: @¦" }'),
		['z', 'y', 'x', 'params']);
});

test('inner variables shadow outer variables of the same name', () => {
	const variables = completionsAt('{ "@x": "1", "#foreach(@x in $.a) into \'r\'": [ { "v": "@¦" } ] }')?.variables;

	assert.deepEqual(variables, [
		{ name: 'x', description: 'loop item (#foreach)' },
		{ name: 'params', description: 'transformer parameters (from #transform)' }
	]);
});

test('nothing is offered where a new variable is being named', () => {
	assert.deepEqual(namesAt('{ "@a": "1", "@¦": 2 }'), []);
	assert.deepEqual(namesAt('{ "@a": "1", "#foreach(@x in $.b) into @¦": [] }'), []);
	assert.deepEqual(namesAt('{ "@a": "1", "#using($.b as @¦": [] }'), []);
	assert.deepEqual(namesAt('{ "@a": "1", "#foreach(@¦": [] }'), []);
});

test('existing variables are offered as the source of a loop', () => {
	assert.deepEqual(namesAt('{ "@a": "1", "#foreach(@x in @¦": [] }'), ['a', 'params']);
});

test('variables in other test transformers within a test file are not in scope', () => {
	const text = '{ "tests": [ { "transformer": { "@a": "1" } }, { "transformer": { "b": "@¦" } } ] }';

	assert.deepEqual(namesAt(text), ['params']);
});
