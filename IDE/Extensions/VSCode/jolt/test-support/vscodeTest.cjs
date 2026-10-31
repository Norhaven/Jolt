// Shared by the extension host tests (.vscode-test.mjs) and the UI tests (e2e), so that both run against the same
// VS Code and the same fixture workspace.
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

/**
 * The VS Code version that tests run against. The UI tests rely on VS Code's DOM, which isn't a public API, so this is
 * pinned rather than following the latest release. Update it deliberately, and fix any selectors that break.
 */
const vscodeVersion = '1.142.0';

const extensionRoot = path.resolve(__dirname, '..');
const fixtureWorkspace = path.join(__dirname, 'workspace');
const serverPath = path.join(extensionRoot, 'server', 'Jolt.LanguageServer.dll');

/**
 * Copies the fixture workspace to a new temporary folder, so that tests can change its files freely.
 */
function createWorkspace(prefix = 'jolt-test-') {
	const folder = fs.mkdtempSync(path.join(os.tmpdir(), prefix));

	fs.cpSync(fixtureWorkspace, folder, { recursive: true });

	return folder;
}

/**
 * Creates a new user data folder, so that each run starts without state from the last one (such as settings changed
 * by a test, or editors left open), with settings that keep VS Code quiet and predictable.
 */
function createUserDataDir(settings = {}) {
	const folder = fs.mkdtempSync(path.join(os.tmpdir(), 'jolt-test-user-'));

	fs.mkdirSync(path.join(folder, 'User'));
	fs.writeFileSync(path.join(folder, 'User', 'settings.json'), JSON.stringify({
		'chat.disableAIFeatures': true,
		'extensions.autoUpdate': false,
		'telemetry.telemetryLevel': 'off',
		'update.mode': 'none',
		'workbench.startupEditor': 'none',
		'workbench.tips.enabled': false,
		...settings
	}, undefined, 4));

	return folder;
}

/**
 * The environment to start VS Code with. When tests are run from VS Code's integrated terminal (or by an extension),
 * the inherited ELECTRON_RUN_AS_NODE makes VS Code run as plain Node, and the VSCODE_* variables make it hand its
 * arguments to the VS Code that is already running instead of starting a new instance.
 */
function getLaunchEnv(env = process.env) {
	const result = {};

	for (const [key, value] of Object.entries(env)) {
		if (value !== undefined && key !== 'ELECTRON_RUN_AS_NODE' && !key.startsWith('VSCODE_')) {
			result[key] = value;
		}
	}

	return result;
}

/**
 * Validation needs the language server, which isn't part of the repository, so fail early with how to build it.
 */
function assertServerBuilt() {
	if (!fs.existsSync(serverPath)) {
		throw new Error(`The language server was not found at ${serverPath}. Run "npm run build-server" before running these tests.`);
	}
}

module.exports = { vscodeVersion, extensionRoot, fixtureWorkspace, createWorkspace, createUserDataDir, getLaunchEnv, assertServerBuilt };
