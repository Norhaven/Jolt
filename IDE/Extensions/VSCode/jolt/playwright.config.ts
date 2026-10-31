import { defineConfig } from '@playwright/test';

// UI tests that drive a real VS Code window with the extension loaded. See e2e/vscode.ts for how it is started.
export default defineConfig({
	testDir: './e2e',
	// Each worker starts its own VS Code, which is heavy, so they run one at a time.
	workers: 1,
	fullyParallel: false,
	timeout: 60000,
	expect: { timeout: 15000 },
	retries: process.env.CI ? 1 : 0,
	// Screenshots and traces of failures are attached by the VS Code fixture.
	reporter: [['list'], ['html', { open: 'never' }]]
});
