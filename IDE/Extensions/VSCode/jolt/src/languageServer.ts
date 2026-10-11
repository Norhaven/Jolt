import * as fs from 'node:fs';
import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions, State, TransportKind } from 'vscode-languageclient/node';
import { CustomMethodsService } from './customMethodsService';

/** Whether the language server is validating transformers, and if not, why. */
export type ServerStatus =
	| { state: 'starting' }
	| { state: 'running' }
	| { state: 'disabled' }
	| { state: 'failed'; message: string };

/** The oldest .NET runtime that the language server runs on (it rolls forward to any later one). */
const dotnetRuntimeVersion = '8.0';

/** The language server's notification describing what the host application provides to a transformer. */
const documentContextNotification = 'jolt/didChangeDocumentContext';

const enabledSetting = 'jolt.validation.enabled';

/** The .NET Install Tool's AcquireErrorConfiguration.DisableErrorPopups. */
const disableErrorPopups = 1;

/**
 * Runs the Jolt language server, which validates transformers with the Jolt library, while validation is enabled.
 * The server needs a .NET runtime, which the .NET Install Tool extension finds or installs.
 */
export class LanguageServerController implements vscode.Disposable {
	private client: LanguageClient | undefined;
	private transition: Promise<void> = Promise.resolve();
	private currentStatus: ServerStatus = isEnabled() ? { state: 'starting' } : { state: 'disabled' };
	private readonly statusChanged = new vscode.EventEmitter<ServerStatus>();
	private readonly clientDisposables: vscode.Disposable[] = [];

	/**
	 * The server's log, which is kept across restarts so that the reason for a failure can still be seen afterwards.
	 */
	readonly output = vscode.window.createOutputChannel('Jolt Language Server', { log: true });

	readonly onDidChangeStatus = this.statusChanged.event;

	private readonly disposables: vscode.Disposable[] = [this.output, this.statusChanged];

	constructor(private readonly context: vscode.ExtensionContext, private readonly methods: CustomMethodsService) {
		this.disposables.push(vscode.workspace.onDidChangeConfiguration(e => {
			if (e.affectsConfiguration(enabledSetting)) {
				void this.update();
			}
		}));
	}

	get status(): ServerStatus {
		return this.currentStatus;
	}

	/**
	 * Starts or stops the server to match the setting. Changes are made one at a time, in the order requested.
	 */
	update(): Promise<void> {
		return this.enqueue(() => isEnabled() ? this.start() : this.stopAndDisable());
	}

	/**
	 * Stops the server, if it is running, and starts it again when validation is enabled.
	 */
	restart(): Promise<void> {
		return this.enqueue(async () => {
			await this.stop();
			await (isEnabled() ? this.start() : this.stopAndDisable());
		});
	}

	async stop(): Promise<void> {
		const client = this.client;

		this.client = undefined;
		this.clientDisposables.splice(0).forEach(x => x.dispose());

		if (client) {
			try {
				await client.stop();
			} catch {
				// The server may already have exited.
			}
		}
	}

	dispose(): void {
		// The log is disposed afterwards, as the client may still write to it while stopping.
		void this.stop().finally(() => this.disposables.forEach(x => x.dispose()));
	}

	private enqueue(change: () => Promise<void>): Promise<void> {
		this.transition = this.transition.then(change);

		return this.transition;
	}

	private setStatus(status: ServerStatus): void {
		this.currentStatus = status;
		this.statusChanged.fire(status);
	}

	private async stopAndDisable(): Promise<void> {
		await this.stop();
		this.setStatus({ state: 'disabled' });
	}

	private async start(): Promise<void> {
		if (this.client) {
			return;
		}

		this.setStatus({ state: 'starting' });

		try {
			const serverPath = this.context.asAbsolutePath('server/Jolt.LanguageServer.dll');

			if (!fs.existsSync(serverPath)) {
				throw new Error(`The language server was not found at ${serverPath}. Run "npm run build-server" to build it.`);
			}

			const dotnetPath = await findDotnet(this.context.extension.id);

			if (!dotnetPath) {
				throw new Error(`.NET ${dotnetRuntimeVersion} or later could not be found or installed.`);
			}

			const serverOptions: ServerOptions = { command: dotnetPath, args: [serverPath], transport: TransportKind.stdio };
			const clientOptions: LanguageClientOptions = {
				documentSelector: [{ language: 'jolt' }],
				// Settings changes are sent to the server under "jolt", in the same shape as the initialization options.
				synchronize: { configurationSection: 'jolt' },
				initializationOptions: { validation: { allowUnsafeMethods: getAllowUnsafeMethods() } },
				outputChannel: this.output
			};

			const client = new LanguageClient('jolt', 'Jolt Language Server', serverOptions, clientOptions);

			await client.start();
			this.client = client;

			this.clientDisposables.push(
				vscode.workspace.onDidOpenTextDocument(document => void this.sendContext(document)),
				this.methods.onDidChange(() => void this.sendAllContexts()),
				// The client restarts a server that exits unexpectedly, until it has done so too many times.
				client.onDidChangeState(e => {
					if (this.client !== client) {
						return;
					}

					switch (e.newState) {
						case State.Starting:
							this.setStatus({ state: 'starting' });
							break;
						case State.Running:
							this.setStatus({ state: 'running' });
							break;
						case State.Stopped:
							this.setStatus({ state: 'failed', message: 'The language server stopped unexpectedly.' });
							break;
					}
				}));

			this.setStatus({ state: 'running' });

			await this.sendAllContexts();
		} catch (error) {
			const message = error instanceof Error ? error.message : String(error);

			this.output.error(`The language server could not be started: ${message}`);
			this.setStatus({ state: 'failed', message });
			void this.offerActions(`Jolt transformers will not be validated: ${message}`);
		}
	}

	private async offerActions(message: string): Promise<void> {
		const showOutput = 'Show Output';
		const retry = 'Retry';
		const disable = 'Disable Validation';

		switch (await vscode.window.showWarningMessage(message, showOutput, retry, disable)) {
			case showOutput:
				this.output.show();
				break;
			case retry:
				await this.restart();
				break;
			case disable:
				await disableValidation();
				break;
		}
	}

	private async sendAllContexts(): Promise<void> {
		await Promise.all(vscode.workspace.textDocuments.map(x => this.sendContext(x)));
	}

	/**
	 * Tells the server which custom methods and transformers the host application provides to a transformer. When
	 * they are not known (in an untrusted workspace), they are left out so that the server does not report them.
	 */
	private async sendContext(document: vscode.TextDocument): Promise<void> {
		const client = this.client;

		if (!client || document.languageId !== 'jolt') {
			return;
		}

		const context = await this.methods.getContextFor(document);

		await client.sendNotification(documentContextNotification, {
			textDocument: { uri: client.code2ProtocolConverter.asUri(document.uri) },
			methods: context?.methods?.map(x => ({ name: x.name })),
			transformers: context?.transformers
		});
	}
}

function isEnabled(): boolean {
	return vscode.workspace.getConfiguration().get<boolean>(enabledSetting, true);
}

/**
 * Turns validation off where it is set, so that a workspace setting turning it on does not override the change.
 */
export async function disableValidation(): Promise<void> {
	const configuration = vscode.workspace.getConfiguration();
	const target = configuration.inspect<boolean>(enabledSetting)?.workspaceValue !== undefined
		? vscode.ConfigurationTarget.Workspace
		: vscode.ConfigurationTarget.Global;

	await configuration.update(enabledSetting, false, target);
}

function getAllowUnsafeMethods(): boolean {
	return vscode.workspace.getConfiguration().get<boolean>('jolt.validation.allowUnsafeMethods', false);
}

/**
 * Gets the path of a .NET executable that can run the language server: an existing installation when there is a
 * suitable one, and otherwise one that the .NET Install Tool installs for this extension.
 */
async function findDotnet(extensionId: string): Promise<string | undefined> {
	// The .NET Install Tool requires all of these to find a runtime, with the architecture in Node's format (e.g.
	// "x64" or "arm64"). The runtime must match this process's architecture, as it is started from here.
	const acquireContext = { version: dotnetRuntimeVersion, requestingExtensionId: extensionId, mode: 'runtime', architecture: process.arch };

	try {
		// Returns the same result as dotnet.acquire, although versions before 2.2.2 could return the path itself.
		const found = await vscode.commands.executeCommand<string | { dotnetPath: string } | undefined>(
			'dotnet.findPath',
			{
				// Failing to find a runtime isn't an error here, since one is acquired instead, so it shouldn't show one.
				acquireContext: { ...acquireContext, errorConfiguration: disableErrorPopups },
				versionSpecRequirement: 'greater_than_or_equal'
			});
		const foundPath = typeof found === 'string' ? found : found?.dotnetPath;

		if (foundPath) {
			return foundPath;
		}
	} catch {
		// Older versions of the .NET Install Tool do not have dotnet.findPath, so fall back to acquiring a runtime.
	}

	const acquired = await vscode.commands.executeCommand<{ dotnetPath: string } | undefined>('dotnet.acquire', acquireContext);

	return acquired?.dotnetPath;
}
