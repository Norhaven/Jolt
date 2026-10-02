import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { ExpressionContext } from '../language/context';
import { libraryMethods, MethodTarget } from '../language/library';
import { formatSignature, getMethodCompletions } from '../language/methodCompletions';

function complete(target: MethodTarget, textBeforeCursor: string, includeUnsafeMethods = false) {
	const context: ExpressionContext = { target, textBeforeCursor };

	return getMethodCompletions(context, libraryMethods, { includeUnsafeMethods });
}

function names(target: MethodTarget, textBeforeCursor: string, includeUnsafeMethods = false) {
	return complete(target, textBeforeCursor, includeUnsafeMethods)?.map(x => x.method.name);
}

function snippetFor(target: MethodTarget, textBeforeCursor: string, name: string) {
	return complete(target, textBeforeCursor)?.find(x => x.method.name === name)?.snippet;
}

test('no completions unless a method name is being written', () => {
	assert.equal(complete('propertyValue', '$.some.path'), undefined);
	assert.equal(complete('propertyValue', '@x > '), undefined);
});

test('property values offer value methods but not property name methods', () => {
	const offered = names('propertyValue', '#');

	assert.ok(offered?.includes('toUpperCase'));
	assert.ok(offered?.includes('valueOf'));
	assert.ok(!offered?.includes('foreach'));
	assert.ok(!offered?.includes('is'));
});

test('unsafe methods are only offered when enabled', () => {
	assert.ok(!names('propertyValue', '#')?.includes('eval'));
	assert.ok(names('propertyValue', '#', true)?.includes('eval'));
});

test('property names offer key methods but not match arms', () => {
	const offered = names('propertyName', '#');

	assert.ok(offered?.includes('foreach'));
	assert.ok(offered?.includes('nameOf'));
	assert.ok(!offered?.includes('is'));
	assert.ok(!offered?.includes('toUpperCase'));
});

test('match cases offer only match arms', () => {
	assert.deepEqual(names('matchBlock', '#')?.sort(), ['default', 'given', 'is']);
});

test('statement blocks offer only statements', () => {
	assert.deepEqual(names('statementBlock', '#')?.sort(), ['removeAt', 'setAt', 'when']);
});

test('value generators in property names include the into clause', () => {
	assert.equal(snippetFor('propertyName', '#', 'foreach'), 'foreach(${1:@x} in ${2:\\$.path}) into \'${3:name}\'$0');
	assert.equal(snippetFor('propertyName', '#', 'using'), 'using(${1:\\$.path} as ${2:@x}) into \'${3:name}\'$0');
});

test('optional parameters are left out of the snippet', () => {
	assert.equal(snippetFor('propertyValue', '#', 'orderBy'), 'orderBy(${1:value})$0');
});

test('the partially typed method name is replaced', () => {
	assert.equal(complete('propertyValue', '#toU')?.[0].replaceLength, 3);
});

test('piped methods leave out the first parameter', () => {
	assert.equal(snippetFor('propertyValue', '$.a->#', 'select'), 'select(${1:@x}: ${2:expression})$0');
	assert.equal(snippetFor('propertyValue', '$.a -> #', 'toUpperCase'), 'toUpperCase()$0');
});

test('piped methods exclude methods without a value to pipe into', () => {
	const offered = names('propertyValue', '$.a->#');

	assert.ok(!offered?.includes('currentDateTime'));
	assert.ok(!offered?.includes('foreach'));
	assert.ok(offered?.includes('where'));
});

test('completing directly after the pipe operator inserts the hash', () => {
	const completion = complete('propertyValue', '@x->')?.find(x => x.method.name === 'length');

	assert.equal(completion?.snippet, '#length()$0');
	assert.equal(completion?.replaceLength, 0);
});

test('signatures mark optional and variadic parameters', () => {
	const method = (name: string) => libraryMethods.find(x => x.name === name)!;

	assert.equal(formatSignature(method('reduce')), 'reduce(value, lambda, seed?)');
	assert.equal(formatSignature(method('append')), 'append(value, ...additionalValues)');
	assert.equal(formatSignature(method('append'), true), 'append(...additionalValues)');
});
