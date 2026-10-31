import { strict as assert } from 'node:assert';
import * as vscode from 'vscode';
import { closeAllEditors, extensionId, openWorkspaceFile } from './helpers';

suite('Activation', () => {
	teardown(closeAllEditors);

	test('.jolt.json files are Jolt transformers', async () => {
		const document = await openWorkspaceFile('Transformer.jolt.json');

		assert.equal(document.languageId, 'jolt');
	});

	test('opening a transformer activates the extension', async () => {
		await openWorkspaceFile('Transformer.jolt.json');

		assert.equal(vscode.extensions.getExtension(extensionId)?.isActive, true);
	});

	test('the commands are registered', async () => {
		await openWorkspaceFile('Transformer.jolt.json');

		const commands = await vscode.commands.getCommands(true);

		assert.ok(commands.includes('jolt.restartLanguageServer'));
		assert.ok(commands.includes('jolt.showLanguageServerOutput'));
		assert.ok(commands.includes('jolt.openPreview'));
		assert.ok(commands.includes('jolt.choosePreviewInput'));
		assert.ok(commands.includes('jolt.openPreviewInput'));
	});
});
