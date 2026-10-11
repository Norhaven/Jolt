import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { formatVariableDocumentation, getVariableCompletions, getVariableOccurrences, getVariableReferenceAt, isValidVariableName } from '../language/variables';

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

test('a loop index declared without @ is not a valid binding', () => {
	assert.ok(!namesAt('{ "#foreach(@x;i in $.a) into \'r\'": [ { "v": "@¦" } ] }')?.includes('i'));
});

test('nothing is offered where a loop index is being named', () => {
	assert.deepEqual(namesAt('{ "@a": "1", "#foreach(@x;@¦": [] }'), []);
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

/** Finds the offset of the nth (zero-based) occurrence of a substring. */
function nth(text: string, search: string, n = 0): number {
	let index = -1;

	for (let i = 0; i <= n; i++) {
		index = text.indexOf(search, index + 1);
	}

	return index;
}

/** Gets the text that a reference's binding declares, e.g. "x" at its declaration. */
function declarationOf(text: string, offset: number) {
	const declaration = getVariableReferenceAt(text, offset)?.binding?.declaration;

	return declaration && { offset: declaration.start, name: text.slice(declaration.start, declaration.end) };
}

test('a reference is found on its @ and its name, but not elsewhere', () => {
	const text = '{ "a": "#valueOf(@xyz)" }';

	assert.deepEqual(getVariableReferenceAt(text, nth(text, '@xyz'))?.span, { start: nth(text, 'xyz'), end: nth(text, 'xyz') + 3 });
	assert.equal(getVariableReferenceAt(text, nth(text, 'yz'))?.name, 'xyz');
	assert.equal(getVariableReferenceAt(text, nth(text, 'valueOf')), undefined);
	assert.equal(getVariableReferenceAt(text, 1), undefined);
});

test('variables within string literals are not references', () => {
	const text = `{ "a": "#concat('@name', @x)" }`;

	assert.equal(getVariableReferenceAt(text, nth(text, 'name')), undefined);
});

test('a reference resolves to a declaration in an earlier property', () => {
	const text = '{ "@total": "1", "b": "@total" }';

	assert.deepEqual(declarationOf(text, nth(text, '@total', 1)), { offset: nth(text, 'total'), name: 'total' });
});

test('a declaration resolves to itself', () => {
	const text = '{ "@total": "1", "b": "#select($.a, @y: @y)" }';

	assert.deepEqual(declarationOf(text, nth(text, 'total')), { offset: nth(text, 'total'), name: 'total' });
	assert.deepEqual(declarationOf(text, nth(text, '@y')), { offset: nth(text, 'y:'), name: 'y' });
});

test('loop, alias, into and lambda variables resolve to their declarations', () => {
	const text = '{ "#foreach(@x;@i in $.a) into @r": [ { "v": "@x + @i" } ], "#using($.b as @u) into \'w\'": [ "#setAt(@u, @r)" ], "l": "#reduce($.c, @acc;@cur: @acc + @cur)" }';

	assert.equal(declarationOf(text, nth(text, '@x', 1))?.offset, nth(text, 'x;'));
	assert.equal(declarationOf(text, nth(text, '@i', 1))?.offset, nth(text, 'i in'));
	assert.equal(declarationOf(text, nth(text, '@u', 1))?.offset, nth(text, 'u)'));
	assert.equal(declarationOf(text, nth(text, '@r', 1))?.offset, nth(text, 'r"'));
	assert.equal(declarationOf(text, nth(text, '@cur', 1))?.offset, nth(text, 'cur:'));
});

test('pattern captures resolve to their first use in the pattern', () => {
	const text = '{ "#match($.a as @m) into \'r\'": [ { "#is({ \'f\': @f }) && #given(@f > 1)": "@f" } ] }';
	const capture = nth(text, 'f }');

	assert.equal(declarationOf(text, nth(text, '@f', 1))?.offset, capture);
	assert.equal(declarationOf(text, nth(text, '@f', 2))?.offset, capture);
});

test('the innermost declaration is the one that is referred to', () => {
	const text = '{ "@x": "1", "#foreach(@x in $.a) into \'r\'": [ { "v": "@x" } ], "w": "@x" }';

	assert.equal(declarationOf(text, nth(text, '@x', 2))?.offset, nth(text, 'x in'));
	assert.equal(declarationOf(text, nth(text, '@x', 3))?.offset, nth(text, 'x"'));
});

test('@params and undeclared variables have no declaration', () => {
	const text = '{ "a": "@params.p + @missing" }';

	assert.equal(getVariableReferenceAt(text, nth(text, 'params'))?.binding?.declaration, undefined);
	assert.ok(getVariableReferenceAt(text, nth(text, 'params'))?.binding);
	assert.equal(getVariableReferenceAt(text, nth(text, 'missing'))?.binding, undefined);
});

test('occurrences include the declaration and its references, but not other variables of the same name', () => {
	const text = '{ "@x": "1", "a": "@x + #select($.b, @x: @x)", "b": "@x" }';
	const binding = getVariableReferenceAt(text, nth(text, 'x'))!.binding!;
	const occurrences = getVariableOccurrences(text, binding).map(x => x.start);

	assert.deepEqual(occurrences, [nth(text, 'x'), nth(text, '@x', 1) + 1, nth(text, '@x', 4) + 1]);
});

test('occurrences of a loop index include its declaration', () => {
	const text = '{ "#foreach(@x;@i in $.a) into \'r\'": [ { "v": "@i" } ] }';
	const binding = getVariableReferenceAt(text, nth(text, '@i', 1))!.binding!;

	assert.deepEqual(getVariableOccurrences(text, binding).map(x => x.start), [nth(text, '@i') + 1, nth(text, '@i', 1) + 1]);
});

test('a reference to a loop index declared without @ is undeclared', () => {
	const text = '{ "#foreach(@x;i in $.a) into \'r\'": [ { "v": "@i" } ] }';

	assert.equal(getVariableReferenceAt(text, nth(text, '@i'))?.binding, undefined);
});

test('the documentation describes the variable and where it is declared', () => {
	const text = '{ "#foreach(@x in $.a) into \'r\'": [ { "v": "@x" } ] }';
	const documentation = formatVariableDocumentation(getVariableReferenceAt(text, nth(text, '@x', 1))!.binding!);

	assert.equal(documentation, "```\n@x\n```\n\nLoop item (#foreach)\n\n*Declared in:*\n```\n#foreach(@x in $.a) into 'r'\n```");
});

test('valid variable names', () => {
	assert.ok(isValidVariableName('total_2'));
	assert.ok(!isValidVariableName('2total'));
	assert.ok(!isValidVariableName('@total'));
	assert.ok(!isValidVariableName('a b'));
});
