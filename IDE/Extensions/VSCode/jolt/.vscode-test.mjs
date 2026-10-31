// Runs the tests in integration/ inside a VS Code extension host, against the built extension (dist/).
import { defineConfig } from '@vscode/test-cli';
import support from './test-support/vscodeTest.cjs';

support.assertServerBuilt();

export default defineConfig({
	label: 'integration',
	files: 'out-integration/**/*.test.js',
	version: support.vscodeVersion,
	workspaceFolder: support.createWorkspace('jolt-integration-'),
	// Clears the variables that getLaunchEnv leaves out (undefined removes a variable rather than setting it).
	env: Object.fromEntries(Object.keys(process.env)
		.filter(x => !(x in support.getLaunchEnv()))
		.map(x => [x, undefined])),
	// The extensions folder is left as the default (within .vscode-test), where the extension's dependencies are installed.
	launchArgs: ['--disable-gpu', `--user-data-dir=${support.createUserDataDir()}`],
	mocha: {
		ui: 'tdd',
		// Starting the language server (and finding .NET for it) can take a while the first time.
		timeout: 60000
	}
});
