// The preview, which runs a transformer against an input file with the language server and shows its output.
import { strict as assert } from 'node:assert';
import * as vscode from 'vscode';
import { activateExtension, closeAllEditors, waitFor, workspaceFile } from './helpers';

let transformerCount = 0;

/**
 * Writes a transformer with a single input file (so that the preview uses it without asking) and opens it.
 */
async function openWithInput(transformer: string, inputFile: unknown): Promise<{ transformer: vscode.TextDocument; inputUri: vscode.Uri }> {
	const prefix = `Preview${++transformerCount}`;
	const transformerUri = workspaceFile(`${prefix}.jolt.json`);
	const inputUri = workspaceFile('.jolt', prefix, 'example.input.json');

	await vscode.workspace.fs.writeFile(transformerUri, new TextEncoder().encode(transformer));
	await vscode.workspace.fs.writeFile(inputUri, new TextEncoder().encode(JSON.stringify(inputFile, null, 4)));

	const document = await vscode.workspace.openTextDocument(transformerUri);

	await vscode.window.showTextDocument(document);
	await activateExtension();

	return { transformer: document, inputUri };
}

function findPreview(transformer: vscode.TextDocument): vscode.TextDocument | undefined {
	return vscode.workspace.textDocuments.find(x => x.uri.scheme === 'jolt-preview' && x.uri.query === transformer.uri.toString());
}

/** Waits for the preview's output (the text after its comments) to satisfy the check. */
async function waitForPreview(transformer: vscode.TextDocument, check: (text: string) => boolean, message: string): Promise<string> {
	return waitFor(() => {
		const text = findPreview(transformer)?.getText();

		return text !== undefined && check(text) && text;
	}, message);
}

function outputOf(previewText: string): unknown {
	return JSON.parse(previewText.split(/\r?\n/).filter(x => !x.startsWith('//')).join('\n'));
}

async function replaceAll(document: vscode.TextDocument, text: string): Promise<void> {
	const edit = new vscode.WorkspaceEdit();

	edit.replace(document.uri, new vscode.Range(document.positionAt(0), document.positionAt(document.getText().length)), text);
	assert.ok(await vscode.workspace.applyEdit(edit));
}

suite('Preview', () => {
	teardown(closeAllEditors);

	test('shows the output, and runs again when the transformer or the unsaved input file changes', async () => {
		const { transformer, inputUri } = await openWithInput('{ "name": "#valueOf($.customer.name)" }', { input: { customer: { name: 'Ada' } } });

		await vscode.commands.executeCommand('jolt.openPreview', transformer.uri);

		const first = await waitForPreview(transformer, x => x.includes('Ran in'), 'the first run');

		assert.match(first, /^\/\/ Input: .*example\.input\.json/);
		assert.deepEqual(outputOf(first), { name: 'Ada' });

		await replaceAll(transformer, '{ "customer": "#valueOf($.customer.name)" }');
		await waitForPreview(transformer, x => x.includes('"customer"'), 'the output of the changed transformer');

		const input = await vscode.workspace.openTextDocument(inputUri);

		await replaceAll(input, JSON.stringify({ input: { customer: { name: 'Grace' } } }));

		const changed = await waitForPreview(transformer, x => x.includes('Grace'), 'the output with the unsaved input file');

		assert.deepEqual(outputOf(changed), { customer: 'Grace' });
	});

	test('custom methods return their mocks, and calling one without a mock is reported', async () => {
		const { transformer, inputUri } = await openWithInput(
			'{ "greeting": "#greet($.name)" }',
			{ input: { name: 'Ada' }, mocks: { greet: { returns: 'Hello, Ada' } } });

		await vscode.commands.executeCommand('jolt.openPreview', transformer.uri);

		const mocked = await waitForPreview(transformer, x => x.includes('Ran in'), 'the run with the mock');

		assert.deepEqual(outputOf(mocked), { greeting: 'Hello, Ada' });

		// The methods file lists #greet, so without a mock the preview can't call it.
		const input = await vscode.workspace.openTextDocument(inputUri);

		await replaceAll(input, JSON.stringify({ input: { name: 'Ada' } }));

		const missing = await waitForPreview(transformer, x => x.includes('has no mock'), 'the missing mock to be reported');

		assert.match(missing, /Showing the output from the last successful run/);
	});

	test('a transformer with errors is not run', async () => {
		const { transformer } = await openWithInput('{ "a": "@missing" }', { input: {} });

		await vscode.commands.executeCommand('jolt.openPreview', transformer.uri);

		await waitForPreview(transformer, x => x.includes('Not run, since the transformer has 1 error'), 'the run to be skipped');
	});
});
