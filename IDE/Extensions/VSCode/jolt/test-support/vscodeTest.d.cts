declare const support: {
	vscodeVersion: string;
	extensionRoot: string;
	fixtureWorkspace: string;
	createWorkspace(prefix?: string): string;
	createUserDataDir(settings?: Record<string, unknown>): string;
	getLaunchEnv(env?: NodeJS.ProcessEnv): Record<string, string>;
	assertServerBuilt(): void;
};

export = support;
