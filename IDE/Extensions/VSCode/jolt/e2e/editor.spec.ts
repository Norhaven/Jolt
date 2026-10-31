// What a user sees and does in the editor, which the extension host tests can't check.
import { expect, test } from './vscode';

/** The colors of the rules in package.json's configurationDefaults, by token. */
const darkColors = {
	'#foreach': 'rgb(197, 134, 192)',
	'in': 'rgb(78, 201, 176)',
	'@person': 'rgb(123, 165, 221)',
	'#toUpperCase': 'rgb(220, 220, 170)'
};

const lightColors = {
	'#foreach': 'rgb(175, 0, 219)',
	'in': 'rgb(38, 127, 153)',
	'@person': 'rgb(0, 16, 128)',
	'#toUpperCase': 'rgb(121, 94, 38)'
};

/**
 * The default themes that the rules apply to, by their IDs (which the rules use), with the class that VS Code gives
 * the workbench while each is in use. Older versions of VS Code have different IDs for some of them (e.g. "Default
 * Dark Modern"), which the rules also list.
 */
const themes = [
	{ id: 'Dark 2026', className: 'vscode-theme-defaults-themes-2026-dark-json', colors: darkColors },
	{ id: 'Dark Modern', className: 'vscode-theme-defaults-themes-dark_modern-json', colors: darkColors },
	{ id: 'Dark+', className: 'vscode-theme-defaults-themes-dark_plus-json', colors: darkColors },
	{ id: 'Visual Studio Dark', className: 'vscode-theme-defaults-themes-dark_vs-json', colors: darkColors },
	{ id: 'Light 2026', className: 'vscode-theme-defaults-themes-2026-light-json', colors: lightColors },
	{ id: 'Light Modern', className: 'vscode-theme-defaults-themes-light_modern-json', colors: lightColors },
	{ id: 'Light+', className: 'vscode-theme-defaults-themes-light_plus-json', colors: lightColors },
	{ id: 'Visual Studio Light', className: 'vscode-theme-defaults-themes-light_vs-json', colors: lightColors }
];

test.describe('Colorization', () => {
	for (const theme of themes) {
		test(`statements, methods and variables have the contributed colors in ${theme.id}`, async ({ vscode }) => {
			// The theme is set for the workspace, so that removing the setting restores the one the tests started with.
			await vscode.setWorkspaceSettings({ 'workbench.colorTheme': theme.id });

			try {
				await expect(vscode.page.locator('.monaco-workbench')).toContainClass(theme.className);

				await vscode.openFile('Colors.jolt.json', [
					'{',
					'    "#foreach(@person in $.people) into \'people\'": [',
					'        { "name": "@person.name->#toUpperCase()" }',
					'    ]',
					'}'
				].join('\n'));

				// The "in" keyword (and, in dark themes, variables) differ from the theme's own colors, which shows
				// that the rules are applied rather than the theme happening to match them.
				for (const [token, color] of Object.entries(theme.colors)) {
					await expect(vscode.token(token), token).toHaveCSS('color', color);
				}
			} finally {
				await vscode.setWorkspaceSettings({});
			}
		});
	}

	test('unsafe methods are underlined', async ({ vscode }) => {
		await vscode.openFile('Unsafe.jolt.json', '{\n    "a": "#eval(\'1 + 1\')"\n}');

		await expect(vscode.token('#eval')).toHaveCSS('text-decoration-line', 'underline');
	});
});

test.describe('Completions', () => {
	test('typing # in a property value suggests methods', async ({ vscode }) => {
		await vscode.openFile('Typing.jolt.json', '{\n    "a": "¦"\n}');
		await vscode.page.keyboard.type('#toUp');

		await expect(vscode.suggestWidget.locator('.monaco-list-row', { hasText: 'toUpperCase' })).toBeVisible();
	});

	test('accepting a method inserts its call', async ({ vscode }) => {
		await vscode.openFile('Accept.jolt.json', '{\n    "a": "¦"\n}');
		await vscode.page.keyboard.type('#toUpperCa');
		await expect(vscode.suggestWidget.locator('.monaco-list-row.focused', { hasText: 'toUpperCase' })).toBeVisible();
		await vscode.page.keyboard.press('Enter');

		await expect(vscode.editor.locator('.view-lines')).toContainText('#toUpperCase(');
	});

	test('Ctrl+Space shows suggestions and parameter hints together', async ({ vscode }) => {
		await vscode.openFile('CtrlSpace.jolt.json', '{\n    "a": "#substring($.a, ¦)"\n}');
		await vscode.page.keyboard.press('Control+Space');

		await expect(vscode.suggestWidget).toBeVisible();
		await expect(vscode.parameterHints).toBeVisible();
		await expect(vscode.parameterHints).toContainText('#substring(');
	});
});

test.describe('Hover', () => {
	test('hovering over a method shows its documentation', async ({ vscode }) => {
		await vscode.openFile('Hover.jolt.json', '{\n    "a": "#greet($.name)"\n}');
		await vscode.token('#greet').hover();

		await expect(vscode.hover).toContainText('Greets someone by name.');
	});
});
