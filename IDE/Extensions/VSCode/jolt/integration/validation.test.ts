// Validation by the language server, and the quick fixes for what it reports.
import { strict as assert } from 'node:assert';
import * as vscode from 'vscode';
import {
	closeAllEditors,
	codeOf,
	getCompletionLabels,
	joltDiagnostics,
	openTransformer,
	openWorkspaceFile,
	waitFor,
	waitForDiagnostic,
	withSetting,
	workspaceFile
} from './helpers';

const unknownMethod = 'JLT108';
const undeclaredVariable = 'JLT562';

async function getQuickFixes(document: vscode.TextDocument, diagnostic: vscode.Diagnostic): Promise<vscode.CodeAction[]> {
	return vscode.commands.executeCommand<vscode.CodeAction[]>(
		'vscode.executeCodeActionProvider', document.uri, diagnostic.range, vscode.CodeActionKind.QuickFix.value);
}

suite('Validation', () => {
	teardown(closeAllEditors);

	test('reports unknown methods and undeclared variables', async () => {
		const document = await openWorkspaceFile('Problems.jolt.json');

		const method = await waitForDiagnostic(document.uri, unknownMethod);
		const variable = await waitForDiagnostic(document.uri, undeclaredVariable);

		assert.equal(document.getText(method.range), '#valuOf');
		assert.match(document.getText(variable.range), /^@?missing$/);
	});

	test('a valid transformer has no problems', async () => {
		const { document } = await openTransformer('{ "a": "#valuOf($.a)" }');

		// Waiting for a problem first shows that the server has validated the document, so the absence of problems
		// after fixing it means something.
		const diagnostic = await waitForDiagnostic(document.uri, unknownMethod);
		const edit = new vscode.WorkspaceEdit();

		edit.replace(document.uri, diagnostic.range, '#valueOf');
		assert.ok(await vscode.workspace.applyEdit(edit));

		await waitFor(() => joltDiagnostics(document.uri).length === 0, 'the problem to be cleared after the edit');
	});

	test('custom methods from the methods file are not reported', async () => {
		const { document } = await openTransformer('{ "a": "#greet($.name)", "b": "#notAMethod($.name)" }');

		await waitForDiagnostic(document.uri, unknownMethod);

		const reported = joltDiagnostics(document.uri).filter(x => codeOf(x) === unknownMethod).map(x => document.getText(x.range));

		assert.deepEqual(reported, ['#notAMethod']);
	});

	test('turning validation off clears its problems, and turning it on again reports them', async () => {
		const document = await openWorkspaceFile('Problems.jolt.json');

		await waitForDiagnostic(document.uri, unknownMethod);

		await withSetting('jolt.validation.enabled', false, async () => {
			await waitFor(() => joltDiagnostics(document.uri).length === 0, 'the problems to be cleared');
		});

		await waitForDiagnostic(document.uri, unknownMethod);
	});

	test('restarting the language server validates again', async () => {
		const document = await openWorkspaceFile('Problems.jolt.json');

		await waitForDiagnostic(document.uri, unknownMethod);
		await vscode.commands.executeCommand('jolt.restartLanguageServer');
		await waitForDiagnostic(document.uri, unknownMethod);
	});

	test('a method added to the methods file is no longer reported', async () => {
		const { document } = await openTransformer('{ "a": "#farewell($.name)" }');

		await waitForDiagnostic(document.uri, unknownMethod);

		const methodsFile = workspaceFile('.jolt', 'methods.json');
		const methods = JSON.parse(new TextDecoder().decode(await vscode.workspace.fs.readFile(methodsFile)));

		methods.methods.push({ name: 'farewell', parameters: [{ name: 'name' }] });

		try {
			await vscode.workspace.fs.writeFile(methodsFile, new TextEncoder().encode(JSON.stringify(methods, undefined, 4)));

			await waitFor(() => joltDiagnostics(document.uri).length === 0, 'the method to be known after the methods file changed');
			assert.ok((await getCompletionLabels(document, document.positionAt(document.getText().indexOf('fare')))).includes('farewell'));
		} finally {
			methods.methods.pop();
			await vscode.workspace.fs.writeFile(methodsFile, new TextEncoder().encode(JSON.stringify(methods, undefined, 4)));
		}
	});
});

suite('Quick fixes', () => {
	teardown(closeAllEditors);

	test('an unknown method can be changed to a similar one', async () => {
		const { document } = await openTransformer('{ "a": "#valuOf($.a)" }');
		const diagnostic = await waitForDiagnostic(document.uri, unknownMethod);
		const fix = (await getQuickFixes(document, diagnostic)).find(x => x.title === 'Change to \'#valueOf\'');

		assert.ok(fix?.edit, 'expected a fix changing the name to #valueOf');
		assert.equal(fix.isPreferred, true);
		assert.ok(await vscode.workspace.applyEdit(fix.edit));
		assert.equal(document.getText(), '{ "a": "#valueOf($.a)" }');
	});

	test('an unknown method can be added to the methods file', async () => {
		const { document } = await openTransformer('{ "a": "#valuOf($.a)" }');
		const diagnostic = await waitForDiagnostic(document.uri, unknownMethod);
		const titles = (await getQuickFixes(document, diagnostic)).map(x => x.title);

		assert.ok(titles.includes('Add \'#valuOf\' to .jolt/methods.json'), `offered: ${titles.join(', ')}`);
	});

	test('an undeclared variable can be declared', async () => {
		const { document } = await openTransformer('{ "a": "@missing" }');
		const diagnostic = await waitForDiagnostic(document.uri, undeclaredVariable);
		const fix = (await getQuickFixes(document, diagnostic)).find(x => x.title === 'Declare \'@missing\'');

		assert.ok(fix?.edit, 'expected a fix declaring @missing');
		assert.ok(await vscode.workspace.applyEdit(fix.edit));
		assert.match(document.getText(), /"@missing"\s*:/);
	});
});
