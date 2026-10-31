import { _electron, ElectronApplication, expect, Locator, Page, test as base } from '@playwright/test';
import { downloadAndUnzipVSCode, runVSCodeCommand } from '@vscode/test-electron';
import * as fs from 'node:fs';
import * as path from 'node:path';
import support from '../test-support/vscodeTest.cjs';

/** The .NET Install Tool, which the extension depends on to find .NET for the language server. */
const dotnetRuntimeExtension = 'ms-dotnettools.vscode-dotnet-runtime';

/** Shared with the extension host tests (which install the extension's dependencies there too). */
const extensionsDir = path.join(support.extensionRoot, '.vscode-test', 'extensions');

/**
 * A VS Code window with the extension loaded and the fixture workspace open.
 */
export class VSCodeWindow {
	constructor(readonly page: Page, readonly workspace: string) {
	}

	/** The editor of the active editor group. */
	get editor(): Locator {
		return this.page.locator('.editor-group-container.active .monaco-editor').first();
	}

	get suggestWidget(): Locator {
		return this.page.locator('.editor-widget.suggest-widget');
	}

	get parameterHints(): Locator {
		return this.page.locator('.editor-widget.parameter-hints-widget');
	}

	get hover(): Locator {
		return this.page.locator('.monaco-hover:not(.hidden)');
	}

	/**
	 * A rendered token on one of the editor's lines. Adjacent tokens with the same style are rendered together, so the
	 * text may need to include its punctuation (e.g. "#foreach" rather than "foreach").
	 */
	token(text: string): Locator {
		return this.editor.locator('.view-lines span > span', { hasText: new RegExp(`^${escapeRegExp(text)}$`) }).first();
	}

	/**
	 * Runs a command from the Command Palette, by its title as shown there (e.g. "Jolt: Restart Language Server").
	 */
	async runCommand(title: string): Promise<void> {
		await this.page.keyboard.press('F1');
		await this.quickInput.fill(`>${title}`);
		await this.quickInputRow(title).waitFor();
		await this.page.keyboard.press('Enter');
	}

	/**
	 * Opens a workspace file with Quick Open, first writing the text to it when given, with the cursor at the ¦ in the
	 * text if there is one.
	 */
	async openFile(name: string, textWithCursor?: string): Promise<void> {
		const file = path.join(this.workspace, name);

		if (textWithCursor !== undefined) {
			fs.writeFileSync(file, textWithCursor.replace('¦', ''));
		}

		await this.page.keyboard.press('Control+P');
		// Quick Open's file search may not have caught up with a file that was just written, but it always finds a
		// file by its full path.
		await this.quickInput.fill(file);
		await this.quickInputRow(name).waitFor();
		await this.page.keyboard.press('Enter');
		await expect(this.page.locator('.tab.active', { hasText: name })).toBeVisible();
		await expect(this.editor).toBeVisible();

		const offset = textWithCursor?.indexOf('¦') ?? -1;

		if (offset >= 0) {
			const before = textWithCursor!.slice(0, offset).split('\n');

			await this.goTo(before.length, before[before.length - 1].length + 1);
		}
	}

	/** Replaces the workspace's settings, which VS Code applies when it sees the file change. */
	async setWorkspaceSettings(settings: Record<string, unknown>): Promise<void> {
		const folder = path.join(this.workspace, '.vscode');

		fs.mkdirSync(folder, { recursive: true });
		fs.writeFileSync(path.join(folder, 'settings.json'), JSON.stringify(settings, undefined, 4));
	}

	/** Moves the cursor to a line and column, both starting at 1. */
	async goTo(line: number, column: number): Promise<void> {
		await this.page.keyboard.press('Control+G');
		await this.quickInput.fill(`:${line}:${column}`);
		await this.page.keyboard.press('Enter');
		await expect(this.page.locator('.quick-input-widget')).toBeHidden();
	}

	async closeAllEditors(): Promise<void> {
		await this.runCommand('View: Close All Editors');
		await expect(this.page.locator('.tab')).toHaveCount(0);
	}

	private get quickInput(): Locator {
		return this.page.locator('.quick-input-widget .quick-input-box input');
	}

	private quickInputRow(text: string): Locator {
		return this.page.locator('.quick-input-widget .monaco-list-row', { hasText: text }).first();
	}
}

function escapeRegExp(text: string): string {
	return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

async function installDependencies(): Promise<void> {
	const installed = fs.existsSync(extensionsDir) && fs.readdirSync(extensionsDir).some(x => x.startsWith(`${dotnetRuntimeExtension}-`));

	if (!installed) {
		await runVSCodeCommand([`--extensions-dir=${extensionsDir}`, '--install-extension', dotnetRuntimeExtension], {
			version: support.vscodeVersion,
			spawn: { env: support.getLaunchEnv() }
		});
	}
}

async function launch(): Promise<{ app: ElectronApplication; window: VSCodeWindow }> {
	support.assertServerBuilt();

	const executablePath = await downloadAndUnzipVSCode(support.vscodeVersion);

	await installDependencies();

	const workspace = support.createWorkspace('jolt-e2e-');
	const userDataDir = support.createUserDataDir({
		// The extension contributes token colors for the default themes, which the colorization tests check.
		'workbench.colorTheme': 'Dark 2026',
		// Tests write their own files, so saving avoids being asked about unsaved changes when editors are closed.
		'files.autoSave': 'afterDelay',
		'files.autoSaveDelay': 100,
		'window.restoreWindows': 'none',
		'editor.hover.delay': 100,
		'workbench.editor.enablePreview': false
	});

	const app = await _electron.launch({
		executablePath,
		env: support.getLaunchEnv(),
		args: [
			workspace,
			`--extensionDevelopmentPath=${support.extensionRoot}`,
			`--extensions-dir=${extensionsDir}`,
			`--user-data-dir=${userDataDir}`,
			'--disable-workspace-trust',
			'--disable-updates',
			'--skip-welcome',
			'--skip-release-notes',
			'--no-cached-data',
			'--disable-gpu-sandbox',
			'--no-sandbox'
		]
	});

	const page = await app.firstWindow();

	// The workbench is shown before its color theme has loaded, and until then changes to the theme are not applied.
	await expect(page.locator('.monaco-workbench')).toHaveClass(/\bvscode-theme-/, { timeout: 60000 });

	return { app, window: new VSCodeWindow(page, workspace) };
}

/**
 * One VS Code is started per worker and shared by its tests, as starting it takes several seconds. Each test starts
 * with no editors open.
 */
export const test = base.extend<{ vscode: VSCodeWindow }, { vscodeWorker: { app: ElectronApplication; window: VSCodeWindow } }>({
	vscodeWorker: [async ({}, use) => {
		const launched = await launch();

		await use(launched);
		await launched.app.close();
	}, { scope: 'worker', timeout: 300000 }],

	// Playwright's own trace and screenshot options only apply to the browsers it starts, so they are taken here, and
	// kept when a test fails.
	vscode: async ({ vscodeWorker: { app, window } }, use, testInfo) => {
		await app.context().tracing.start({ screenshots: true, snapshots: true });

		await use(window);

		if (testInfo.status !== testInfo.expectedStatus) {
			await testInfo.attach('screenshot', { body: await window.page.screenshot(), contentType: 'image/png' });
			await app.context().tracing.stop({ path: testInfo.outputPath('trace.zip') });
		} else {
			await app.context().tracing.stop();
		}

		await window.page.keyboard.press('Escape');
		await window.closeAllEditors();
	}
});

export { expect };
