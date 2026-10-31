// Problems reported by the language server, as they appear in VS Code.
import { expect, test } from './vscode';

test.describe('Validation', () => {
	test('problems are underlined and listed in the Problems panel', async ({ vscode }) => {
		await vscode.openFile('Problems.jolt.json');

		await expect(vscode.editor.locator('.squiggly-error').first()).toBeVisible();

		await vscode.runCommand('View: Focus Problems (Errors, Warnings, Infos)');
		const problems = vscode.page.locator('.markers-panel .monaco-list-row');

		await expect(problems.filter({ hasText: 'JLT108' })).toBeVisible();
		await expect(problems.filter({ hasText: 'JLT562' })).toBeVisible();
	});

	test('a quick fix corrects a misspelled method', async ({ vscode }) => {
		await vscode.openFile('QuickFix.jolt.json', '{\n    "a": "#valu¦Of($.a)"\n}');
		await expect(vscode.editor.locator('.squiggly-error')).toBeVisible();

		await vscode.page.keyboard.press('Control+.');
		const action = vscode.page.locator('.action-widget .monaco-list-row', { hasText: 'Change to \'#valueOf\'' });

		await action.click();

		await expect(vscode.editor.locator('.view-lines')).toContainText('"#valueOf($.a)"');
		await expect(vscode.editor.locator('.squiggly-error')).toHaveCount(0);
	});
});

test.describe('Language status', () => {
	test('shows the validation state, the Jolt version and the methods file', async ({ vscode }) => {
		await vscode.openFile('Transformer.jolt.json');

		await vscode.page.locator('#status\\.languageStatus').click();
		const status = vscode.hover;

		await expect(status).toContainText('Jolt validation');
		await expect(status).toContainText(/Jolt \d+\.\d+/);
		await expect(status).toContainText('1 methods file');
	});
});

test.describe('Commands', () => {
	test('Show Language Server Output opens its log', async ({ vscode }) => {
		await vscode.openFile('Transformer.jolt.json');
		await vscode.runCommand('Jolt: Show Language Server Output');

		await expect(vscode.page.locator('.panel .monaco-select-box, .panel select').first()).toHaveValue(/Jolt Language Server/);
	});
});
