import * as fs from 'node:fs';
import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions, TransportKind } from 'vscode-languageclient/node';
import { CustomMethodsService } from './customMethodsService';

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
	private readonly disposables: vscode.Disposable[] = [];
	private readonly clientDisposables: vscode.Disposable[] = [];

	constructor(private readonly context: vscode.ExtensionContext, private readonly methods: CustomMethodsService) {
		this.disposables.push(vscode.workspace.onDidChangeConfiguration(e => {
			if (e.affectsConfiguration(enabledSetting)) {
				void this.update();
			}
		}));
	}

	/**
	 * Starts or stops the server to match the setting. Changes are made one at a time, in the order requested.
	 */
	update(): Promise<void> {
		this.transition = this.transition.then(() => isEnabled() ? this.start() : this.stop());

		return this.transition;
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
		this.disposables.forEach(x => x.dispose());
		void this.stop();
	}

	private async start(): Promise<void> {
		if (this.client) {
			return;
		}

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
				initializationOptions: { validation: { allowUnsafeMethods: getAllowUnsafeMethods() } }
			};

			const client = new LanguageClient('jolt', 'Jolt Language Server', serverOptions, clientOptions);

			await client.start();
			this.client = client;

			this.clientDisposables.push(
				vscode.workspace.onDidOpenTextDocument(document => void this.sendContext(document)),
				this.methods.onDidChange(() => void this.sendAllContexts()));

			await this.sendAllContexts();
		} catch (error) {
			const message = error instanceof Error ? error.message : String(error);

			void vscode.window.showWarningMessage(`Jolt transformers will not be validated: ${message}`);
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
