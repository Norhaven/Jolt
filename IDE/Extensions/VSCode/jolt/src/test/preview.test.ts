import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import {
	createInputTemplate,
	getCalledMethodNames,
	getInputName,
	getPreviewLenses,
	getTransformerPrefix,
	normalizeInputName,
	previewLensLine,
	renderPreview,
	validateInputName
} from '../language/preview';

test('a transformer\'s prefix is its file name without the transformer ending', () => {
	assert.equal(getTransformerPrefix('orders.jolt.json'), 'orders');
	assert.equal(getTransformerPrefix('orders.v2.jolt.json'), 'orders.v2');
	assert.equal(getTransformerPrefix('Orders.JOLT.JSON'), 'Orders');
	assert.equal(getTransformerPrefix('.jolt.json'), undefined);
	assert.equal(getTransformerPrefix('orders.json'), undefined);
});

test('an input file\'s name is its file name without the input ending', () => {
	assert.equal(getInputName('multi-item-order.input.json'), 'multi-item-order');
	assert.equal(getInputName('methods.json'), undefined);
	assert.equal(getInputName('.input.json'), undefined);
});

test('a typed input name may include the input ending', () => {
	assert.equal(normalizeInputName('  multi-item-order.input.json '), 'multi-item-order');
	assert.equal(normalizeInputName('multi-item-order'), 'multi-item-order');
});

test('input names must be usable as file names and not already exist', () => {
	assert.equal(validateInputName('multi-item-order', ['single-item']), undefined);
	assert.match(validateInputName('  ', [])!, /Enter a name/);
	assert.match(validateInputName('a/b', [])!, /can't contain/);
	assert.match(validateInputName('what?', [])!, /can't contain/);
	assert.match(validateInputName('trailing.', [])!, /can't be used/);
	assert.match(validateInputName('con', [])!, /can't be used/);
	assert.match(validateInputName('Single-Item.input.json', ['single-item'])!, /already exists/);
});

test('the preview\'s lenses switch inputs when there are several and always offer a new one', () => {
	assert.deepEqual(getPreviewLenses(undefined, 0), []);
	assert.deepEqual(getPreviewLenses('only', 1).map(x => x.action), ['open', 'create']);
	assert.match(getPreviewLenses('only', 1)[0].title, /Input: only/);

	const several = getPreviewLenses('second', 3);

	assert.deepEqual(several.map(x => x.action), ['choose', 'create']);
	assert.match(several[0].title, /Input: second/);
	assert.match(several[0].tooltip, /3 input files/);
});

test('a preview with lenses leaves a blank line below them', () => {
	const lines = renderPreview({ inputPath: 'a.input.json', state: { kind: 'running' }, hasLenses: true }).split('\n');

	assert.equal(previewLensLine, 0);
	assert.deepEqual(lines.slice(0, 2), ['', '// Input: a.input.json']);
	assert.equal(renderPreview({ state: { kind: 'noInput' } }).split('\n')[0].startsWith('//'), true);
});

test('called methods are found once each, piped or not', () => {
	const text = JSON.stringify({ a: '#lookupRegion($.country)', b: '#valueOf($.a)->#lookupRegion()', c: '#tagsFor ($.items, @x: @x)' });

	assert.deepEqual(getCalledMethodNames(text), ['lookupRegion', 'valueOf', 'tagsFor']);
});

test('the input template has an empty input to fill in and a mock for each method', () => {
	const empty = createInputTemplate([]);

	assert.deepEqual(JSON.parse(empty.text), { input: {}, mocks: {} });
	assert.equal(empty.text.substring(empty.inputOffset - 1, empty.inputOffset + 1), '{}');

	const withMocks = createInputTemplate(['lookupRegion', 'tagsFor'], '\r\n');

	assert.deepEqual(JSON.parse(withMocks.text), { input: {}, mocks: { lookupRegion: { returns: null }, tagsFor: { returns: null } } });
	assert.ok(!withMocks.text.replace(/\r\n/g, '').includes('\n'));
});

test('a successful run shows the input, the time taken and the output', () => {
	const text = renderPreview({
		inputPath: '.jolt/orders/multi-item-order.input.json',
		state: { kind: 'result', result: { outcome: 'success', output: '{\n  "a": 1\n}', elapsedMilliseconds: 3.4, warnings: ['Mock ignored.'] } }
	});

	assert.equal(text, [
		'// Input: .jolt/orders/multi-item-order.input.json',
		'// Ran in 3 ms.',
		'// Warning: Mock ignored.',
		'{',
		'  "a": 1',
		'}',
		''
	].join('\n'));
});

test('a failed run shows the error and its causes, with the last successful output', () => {
	const text = renderPreview({
		state: {
			kind: 'result',
			result: { outcome: 'error', error: { code: 'JLT100', message: 'Outer.', innerErrors: [{ message: 'Inner\nline two.' }] } }
		},
		lastOutput: '{}'
	});

	assert.equal(text, [
		'// Error: Outer. (JLT100)',
		'//   Caused by: Inner',
		'// line two.',
		'// Showing the output from the last successful run.',
		'{}',
		''
	].join('\n'));
});

test('a skipped run counts the transformer\'s errors', () => {
	const one = renderPreview({ state: { kind: 'result', result: { outcome: 'skipped', transformerErrorCount: 1 } } });
	const two = renderPreview({ state: { kind: 'result', result: { outcome: 'skipped', transformerErrorCount: 2 } } });

	assert.match(one, /has 1 error\./);
	assert.match(two, /has 2 errors\./);
});

test('a successful run without output shows null', () => {
	assert.equal(renderPreview({ state: { kind: 'result', result: { outcome: 'success' } } }).split('\n').at(-2), 'null');
});

test('without an input file or a running server, the preview says why', () => {
	assert.match(renderPreview({ state: { kind: 'noInput' } }), /Choose Preview Input/);
	assert.match(renderPreview({ state: { kind: 'validationDisabled' } }), /jolt\.validation\.enabled/);
	assert.match(renderPreview({ state: { kind: 'serverFailed', message: 'No .NET.' } }), /isn't running: No \.NET\./);
});
