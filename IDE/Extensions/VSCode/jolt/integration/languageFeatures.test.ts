// The editor features that the extension provides itself, which don't depend on the language server.
import { strict as assert } from 'node:assert';
import * as vscode from 'vscode';
import { closeAllEditors, getCompletionLabels, hoverText, openTransformer, withSetting } from './helpers';

suite('Completions', () => {
	teardown(closeAllEditors);

	test('a property value offers value methods, but not statements', async () => {
		const { document, position } = await openTransformer('{ "a": "#¦" }');
		const labels = await getCompletionLabels(document, position);

		assert.ok(labels.includes('valueOf'));
		assert.ok(labels.includes('toUpperCase'));
		assert.ok(!labels.includes('foreach'));
	});

	test('custom methods come from the workspace methods file', async () => {
		const { document, position } = await openTransformer('{ "a": "#gr¦" }');

		assert.ok((await getCompletionLabels(document, position)).includes('greet'));
	});

	test('unsafe methods are only offered when enabled', async () => {
		const { document, position } = await openTransformer('{ "a": "#¦" }');

		assert.ok(!(await getCompletionLabels(document, position)).includes('eval'));

		await withSetting('jolt.completion.includeUnsafeMethods', true, async () => {
			assert.ok((await getCompletionLabels(document, position)).includes('eval'));
		});
	});

	test('variables in scope are offered after @', async () => {
		const { document, position } = await openTransformer([
			'{',
			'    "#foreach(@person in $.people) into \'people\'": [',
			'        { "name": "@¦" }',
			'    ]',
			'}'
		].join('\n'));

		assert.ok((await getCompletionLabels(document, position)).includes('@person'));
	});

	test('statements are offered where a property would go', async () => {
		const { document, position } = await openTransformer('{\n    ¦\n}');
		const labels = await getCompletionLabels(document, position);

		assert.ok(labels.includes('#foreach'));
		assert.ok(labels.includes('#using'));
	});
});

suite('Hover', () => {
	teardown(closeAllEditors);

	test('a library method shows its documentation', async () => {
		const { document, position } = await openTransformer('{ "a": "#subst¦ring($.a, 0..2)" }');
		const hovers = await vscode.commands.executeCommand<vscode.Hover[]>('vscode.executeHoverProvider', document.uri, position);

		assert.match(hoverText(hovers), /#substring\(/);
		assert.match(hoverText(hovers), /standard library/);
	});

	test('a custom method shows its description from the methods file', async () => {
		const { document, position } = await openTransformer('{ "a": "#gre¦et($.name)" }');
		const hovers = await vscode.commands.executeCommand<vscode.Hover[]>('vscode.executeHoverProvider', document.uri, position);

		assert.match(hoverText(hovers), /Greets someone by name\./);
		assert.match(hoverText(hovers), /methods\.json/);
	});
});

suite('Signature help', () => {
	teardown(closeAllEditors);

	test('shows the parameters of the method being called', async () => {
		const { document, position } = await openTransformer('{ "a": "#substring($.a, ¦)" }');
		const help = await vscode.commands.executeCommand<vscode.SignatureHelp>(
			'vscode.executeSignatureHelpProvider', document.uri, position, '(');

		assert.equal(help.signatures.length, 1);
		assert.match(help.signatures[0].label, /^#substring\(/);
		assert.equal(help.signatures[0].activeParameter, 1);
	});
});

suite('Navigation', () => {
	teardown(closeAllEditors);

	test('a custom method goes to its entry in the methods file', async () => {
		const { document, position } = await openTransformer('{ "a": "#gre¦et($.name)" }');
		const locations = await vscode.commands.executeCommand<(vscode.Location | vscode.LocationLink)[]>(
			'vscode.executeDefinitionProvider', document.uri, position);
		const target = locations[0] && ('targetUri' in locations[0] ? locations[0].targetUri : locations[0].uri);

		assert.ok(target?.path.endsWith('/.jolt/methods.json'));
	});

	test('a variable goes to its declaration', async () => {
		const text = '{ "#foreach(@person in $.people) into \'people\'": [ { "name": "@per¦son.name" } ] }';
		const { document, position } = await openTransformer(text);
		const locations = await vscode.commands.executeCommand<vscode.Location[]>(
			'vscode.executeDefinitionProvider', document.uri, position);

		assert.equal(locations.length, 1);
		assert.equal(document.offsetAt(locations[0].range.start), text.indexOf('person'));
	});

	test('renaming a variable changes its declaration and its uses', async () => {
		const { document, position } = await openTransformer(
			'{ "#foreach(@person in $.people) into \'people\'": [ { "name": "@per¦son.name", "age": "@person.age" } ] }');
		const edit = await vscode.commands.executeCommand<vscode.WorkspaceEdit>(
			'vscode.executeDocumentRenameProvider', document.uri, position, 'member');

		assert.ok(await vscode.workspace.applyEdit(edit));
		assert.equal(document.getText(),
			'{ "#foreach(@member in $.people) into \'people\'": [ { "name": "@member.name", "age": "@member.age" } ] }');
	});

	test('the outline shows the transformer\'s properties', async () => {
		const { document } = await openTransformer('{ "#foreach(@x in $.items) into \'items\'": [ { "name": "@x.name" } ], "total": "#length($.items)" }');
		const symbols = await vscode.commands.executeCommand<vscode.DocumentSymbol[]>('vscode.executeDocumentSymbolProvider', document.uri);

		assert.deepEqual(symbols.map(x => [x.name, x.kind]), [
			['#foreach(@x in $.items) into \'items\'', vscode.SymbolKind.Function],
			['total', vscode.SymbolKind.String]
		]);
	});
});

suite('Formatting', () => {
	teardown(closeAllEditors);

	test('formats the document as JSON without changing its expressions', async () => {
		const { document } = await openTransformer('{"name":"@x.name  ->  #toUpperCase()"}');
		const edits = await vscode.commands.executeCommand<vscode.TextEdit[]>(
			'vscode.executeFormatDocumentProvider', document.uri, { tabSize: 4, insertSpaces: true });
		const edit = new vscode.WorkspaceEdit();

		edit.set(document.uri, edits);
		assert.ok(await vscode.workspace.applyEdit(edit));
		assert.equal(document.getText().replace(/\r\n/g, '\n'), '{\n    "name": "@x.name  ->  #toUpperCase()"\n}');
	});
});
