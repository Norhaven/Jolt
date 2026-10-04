import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { CustomMethodsResolver, MethodsFile, parseMethodsFile } from '../language/customMethods';

const builtIns = new Set(['valueOf', 'select']);

function parse(json: unknown) {
	return parseMethodsFile(typeof json === 'string' ? json : JSON.stringify(json, null, 2), 'methods.json', builtIns);
}

function methodsFile(root: boolean | undefined, ...names: string[]) {
	return JSON.stringify({ root, methods: names.map(name => ({ name, description: `${name} described` })) });
}

function createResolver(files: Record<string, string>) {
	const reads: string[] = [];
	const notified: [string, MethodsFile | undefined][] = [];
	const resolver = new CustomMethodsResolver(
		{ readFile: async path => { reads.push(path); return files[path]; } },
		builtIns,
		path => `label:${path}`,
		(path, file) => notified.push([path, file]));

	return { resolver, reads, notified };
}

test('a valid file produces property value methods with their parameters', () => {
	const file = parse({
		methods: [{
			name: 'ReverseString',
			description: 'Reverses a string',
			example: '#ReverseString($.a)',
			parameters: [{ name: 'value' }, { name: 'convert', kind: 'lambda' }]
		}]
	});

	assert.deepEqual(file.problems, []);
	assert.equal(file.isRoot, false);
	assert.deepEqual(file.methods, [{
		name: 'ReverseString',
		description: 'Reverses a string',
		example: '#ReverseString($.a)',
		validOn: ['propertyValue'],
		isValueGenerator: false,
		isUnsafe: false,
		parameters: [
			{ name: 'value', kind: 'value', isOptional: false, isVariadic: false, isLazy: false },
			{ name: 'convert', kind: 'lambda', isOptional: false, isVariadic: false, isLazy: false }
		],
		source: 'methods.json'
	}]);
});

test('the root flag is read', () => {
	assert.equal(parse({ root: true, methods: [] }).isRoot, true);
});

test('invalid JSON is reported once and contributes no methods', () => {
	const file = parse('{ "methods": [ { "name": "a" } ');

	assert.equal(file.methods.length, 0);
	assert.equal(file.problems.length, 1);
	assert.equal(file.problems[0].severity, 'error');
});

test('invalid entries are skipped and reported at their location', () => {
	const file = parse({
		methods: [
			{ name: 'good' },
			{ name: '1bad' },
			{ description: 'no name' },
			'not an object',
			{ name: 'badKind', parameters: [{ name: 'x', kind: 'expression' }] },
			{ name: 'badParameter', parameters: [{}] }
		]
	});

	assert.deepEqual(file.methods.map(x => x.name), ['good']);
	assert.equal(file.problems.length, 5);
	assert.ok(file.problems.every(x => x.severity === 'warning'));
	assert.deepEqual(file.problems[0].start, { line: 6, character: 14 });
});

test('methods that share a name with a standard library method are ignored', () => {
	const file = parse({ methods: [{ name: 'valueOf' }, { name: 'custom' }] });

	assert.deepEqual(file.methods.map(x => x.name), ['custom']);
	assert.match(file.problems[0].message, /standard library method/);
});

test('later duplicates within the same file are ignored', () => {
	const file = parse({ methods: [{ name: 'a', description: 'first' }, { name: 'a', description: 'second' }] });

	assert.deepEqual(file.methods.map(x => x.description), ['first']);
	assert.equal(file.problems.length, 1);
});

test('files up the folder hierarchy are merged, with closer files taking precedence', async () => {
	const { resolver } = createResolver({
		'/p/a/.jolt/methods.json': methodsFile(undefined, 'shared', 'local'),
		'/p/.jolt/methods.json': methodsFile(undefined, 'shared', 'global')
	});

	const methods = await resolver.getMethods(['/p/a/b/.jolt/methods.json', '/p/a/.jolt/methods.json', '/p/.jolt/methods.json']);

	assert.deepEqual(methods.map(x => [x.name, x.source]), [
		['shared', 'label:/p/a/.jolt/methods.json'],
		['local', 'label:/p/a/.jolt/methods.json'],
		['global', 'label:/p/.jolt/methods.json']
	]);
});

test('a root file stops files further up from being merged', async () => {
	const { resolver } = createResolver({
		'/p/a/.jolt/methods.json': methodsFile(true, 'local'),
		'/p/.jolt/methods.json': methodsFile(undefined, 'global')
	});

	const methods = await resolver.getMethods(['/p/a/.jolt/methods.json', '/p/.jolt/methods.json']);

	assert.deepEqual(methods.map(x => x.name), ['local']);
});

test('an invalid file is skipped without stopping files further up from being merged', async () => {
	const { resolver } = createResolver({
		'/p/a/.jolt/methods.json': '{ broken',
		'/p/.jolt/methods.json': methodsFile(undefined, 'global')
	});

	const methods = await resolver.getMethods(['/p/a/.jolt/methods.json', '/p/.jolt/methods.json']);

	assert.deepEqual(methods.map(x => x.name), ['global']);
});

test('files are read once, including files that do not exist', async () => {
	const { resolver, reads } = createResolver({ '/p/.jolt/methods.json': methodsFile(undefined, 'global') });
	const candidates = ['/p/a/.jolt/methods.json', '/p/.jolt/methods.json'];

	await resolver.getMethods(candidates);
	await resolver.getMethods(candidates);
	await Promise.all([resolver.getMethods(candidates), resolver.getMethods(candidates)]);

	assert.deepEqual(reads, candidates);
});

test('a refreshed file is read again and its changes are picked up', async () => {
	const files: Record<string, string> = { '/p/.jolt/methods.json': methodsFile(undefined, 'before') };
	const { resolver, notified } = createResolver(files);
	const candidates = ['/p/a/.jolt/methods.json', '/p/.jolt/methods.json'];

	await resolver.getMethods(candidates);

	files['/p/.jolt/methods.json'] = methodsFile(undefined, 'after');
	files['/p/a/.jolt/methods.json'] = methodsFile(undefined, 'created');
	await resolver.refresh('/p/.jolt/methods.json');
	resolver.invalidate('/p/a/.jolt/methods.json');

	assert.deepEqual((await resolver.getMethods(candidates)).map(x => x.name), ['created', 'after']);
	assert.deepEqual(notified.map(([path, file]) => [path, file !== undefined]), [
		['/p/a/.jolt/methods.json', false],
		['/p/.jolt/methods.json', true],
		['/p/.jolt/methods.json', true],
		['/p/a/.jolt/methods.json', true]
	]);
});
