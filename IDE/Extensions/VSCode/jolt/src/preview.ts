import { parse } from 'jsonc-parser';
import * as vscode from 'vscode';
import { CustomMethodsService } from './customMethodsService';
import {
	createInputTemplate,
	getCalledMethodNames,
	getInputName,
	getTransformerPrefix,
	inputSuffix,
	inputsRootFolder,
	normalizeInputName,
	PreviewState,
	renderPreview,
	TransformResult,
	validateInputName
} from './language/preview';
import { enableValidation, isValidationEnabled, LanguageServerController } from './languageServer';

export const previewScheme = 'jolt-preview';
export const openPreviewCommand = 'jolt.openPreview';
export const choosePreviewInputCommand = 'jolt.choosePreviewInput';
export const openPreviewInputCommand = 'jolt.openPreviewInput';

/** The workspace state holding the input file last chosen for each transformer, by the transformer's URI. */
const chosenInputsKey = 'jolt.preview.chosenInputs';

/** How long to wait after a change before running the transformer again, so that it isn't run on every keystroke. */
const runDelay = 300;

interface PreviewSession {
	readonly transformerUri: vscode.Uri;
	readonly previewUri: vscode.Uri;
	inputUri?: vscode.Uri;
	content: string;
	/** The output of the last successful run with the current input file. */
	lastOutput?: string;
	timer?: ReturnType<typeof setTimeout>;
	run?: vscode.CancellationTokenSource;
}

/**
 * Shows the output of a transformer run against a preview input file in a read-only document beside it, and runs it
 * again whenever either of them changes. A transformer's input files are kept in a .jolt folder next to it, in a
 * folder named after it, e.g. .jolt/orders/multi-item-order.input.json for orders.jolt.json.
 */
export class PreviewController implements vscode.TextDocumentContentProvider, vscode.Disposable {
	private readonly sessions = new Map<string, PreviewSession>();
	private readonly changed = new vscode.EventEmitter<vscode.Uri>();
	/** The revision of each input file that the language server was last sent, by the input file's URI. */
	private readonly sentRevisions = new Map<string, string>();
	/** Tells documents apart when one is closed and opened again, which starts its version over. */
	private readonly documentIds = new WeakMap<vscode.TextDocument, number>();
	private nextDocumentId = 1;
	private readonly disposables: vscode.Disposable[] = [this.changed];

	readonly onDidChange = this.changed.event;

	constructor(
		private readonly context: vscode.ExtensionContext,
		private readonly server: LanguageServerController,
		private readonly methods: CustomMethodsService) {

		const watcher = vscode.workspace.createFileSystemWatcher(`**/${inputsRootFolder}/*/*${inputSuffix}`);

		this.disposables.push(
			vscode.workspace.registerTextDocumentContentProvider(previewScheme, this),
			vscode.commands.registerCommand(openPreviewCommand, (uri?: vscode.Uri) => this.openPreview(uri)),
			vscode.commands.registerCommand(choosePreviewInputCommand, (uri?: vscode.Uri) => this.chooseInput(uri)),
			vscode.commands.registerCommand(openPreviewInputCommand, (uri?: vscode.Uri) => this.openInput(uri)),
			vscode.workspace.onDidChangeTextDocument(e => this.scheduleFor(e.document.uri)),
			vscode.workspace.onDidCloseTextDocument(document => this.closeSession(document.uri)),
			vscode.workspace.onDidRenameFiles(e => void this.moveInputs(e.files)),
			watcher,
			// Edits in an editor are seen as they're made, and changes to an input file that isn't open when it's saved.
			watcher.onDidChange(uri => this.scheduleFor(uri)),
			watcher.onDidCreate(uri => this.scheduleFor(uri)),
			watcher.onDidDelete(uri => this.inputDeleted(uri)),
			this.server.onDidChangeStatus(() => this.scheduleAll(0)),
			this.methods.onDidChange(() => this.scheduleAll(0)));
	}

	provideTextDocumentContent(uri: vscode.Uri): string {
		return this.getSession(uri)?.content ?? '';
	}

	dispose(): void {
		for (const session of this.sessions.values()) {
			this.stopSession(session);
		}

		this.sessions.clear();
		this.disposables.forEach(x => x.dispose());
	}

	private async openPreview(uri?: vscode.Uri): Promise<void> {
		const transformerUri = this.getTransformerUri(uri);

		if (!transformerUri) {
			void vscode.window.showErrorMessage('Open a Jolt transformer (a .jolt.json file) to preview it.');
			return;
		}

		const transformer = await vscode.workspace.openTextDocument(transformerUri);

		if (isTestDocument(transformer.getText())) {
			void vscode.window.showInformationMessage('Jolt test files contain a transformer in each test, so they can\'t be previewed.');
			return;
		}

		const previewUri = getPreviewUri(transformerUri);
		const session = this.getSession(previewUri)!;

		session.inputUri ??= await this.getChosenInput(transformerUri) ?? await this.pickFirstInput(session);

		if (session.inputUri) {
			this.rememberInput(session);
		}

		this.render(session, { kind: 'running' });

		const document = await vscode.workspace.openTextDocument(previewUri);

		await vscode.window.showTextDocument(document, { viewColumn: vscode.ViewColumn.Beside, preview: false, preserveFocus: true });

		this.schedule(session, 0);

		if (!isValidationEnabled()) {
			await this.offerToEnableValidation();
		}
	}

	private async chooseInput(uri?: vscode.Uri): Promise<void> {
		const transformerUri = this.getTransformerUri(uri);

		if (!transformerUri) {
			void vscode.window.showErrorMessage('Open a Jolt transformer (a .jolt.json file) or its preview to choose an input for it.');
			return;
		}

		const session = this.sessions.get(getPreviewUri(transformerUri).toString());
		const current = session?.inputUri ?? await this.getChosenInput(transformerUri);
		const inputs = await listInputs(transformerUri);
		const create: vscode.QuickPickItem = { label: '$(add) New Input File...', alwaysShow: true };
		const items: (vscode.QuickPickItem & { uri?: vscode.Uri })[] = [
			...inputs.map(x => ({
				label: getInputName(basename(x))!,
				description: x.toString() === current?.toString() ? 'current' : undefined,
				detail: vscode.workspace.asRelativePath(x),
				uri: x
			})),
			{ label: '', kind: vscode.QuickPickItemKind.Separator },
			create
		];

		const picked = await vscode.window.showQuickPick(items, {
			title: `Preview Input for ${basename(transformerUri)}`,
			placeHolder: inputs.length > 0 ? 'Choose the input file to run the transformer against' : 'This transformer has no input files yet'
		});

		const inputUri = picked === create ? await this.createInput(transformerUri) : picked?.uri;

		if (!inputUri) {
			return;
		}

		if (session) {
			this.useInput(session, inputUri);
		} else {
			await this.context.workspaceState.update(chosenInputsKey, { ...this.getChosenInputs(), [transformerUri.toString()]: inputUri.toString() });
			await this.openPreview(transformerUri);
		}
	}

	private async openInput(uri?: vscode.Uri): Promise<void> {
		const transformerUri = this.getTransformerUri(uri);
		const inputUri = transformerUri && (this.sessions.get(getPreviewUri(transformerUri).toString())?.inputUri ?? await this.getChosenInput(transformerUri));

		if (inputUri) {
			await vscode.window.showTextDocument(inputUri, { preview: false });
		} else {
			await this.chooseInput(uri);
		}
	}

	/**
	 * Asks for the name of a new input file for a transformer and creates it, along with its folder, with a mock to
	 * fill in for each custom method that the transformer calls. Returns the new file, or undefined if cancelled.
	 */
	private async createInput(transformerUri: vscode.Uri): Promise<vscode.Uri | undefined> {
		const folder = getInputsFolder(transformerUri);
		const existingNames = (await listInputs(transformerUri)).map(x => getInputName(basename(x))!);
		const typed = await vscode.window.showInputBox({
			title: `New Preview Input for ${basename(transformerUri)}`,
			prompt: `Name the input file, which is created in ${inputsRootFolder}/${getTransformerPrefix(basename(transformerUri))}/ next to the transformer.`,
			placeHolder: 'e.g. multi-item-order',
			validateInput: value => validateInputName(value, existingNames)
		});

		if (typed === undefined) {
			return undefined;
		}

		const inputUri = vscode.Uri.joinPath(folder, normalizeInputName(typed) + inputSuffix);
		const transformer = await vscode.workspace.openTextDocument(transformerUri);
		const customMethods = new Set((await this.methods.getContextFor(transformer))?.methods?.map(x => x.name) ?? []);
		const template = createInputTemplate(getCalledMethodNames(transformer.getText()).filter(x => customMethods.has(x)));

		await vscode.workspace.fs.createDirectory(folder);
		await vscode.workspace.fs.writeFile(inputUri, new TextEncoder().encode(template.text));

		const document = await vscode.workspace.openTextDocument(inputUri);
		const position = document.positionAt(template.inputOffset);

		await vscode.window.showTextDocument(document, { preview: false, selection: new vscode.Range(position, position) });

		return inputUri;
	}

	/**
	 * Picks the input file to start a transformer's preview with: its only input file, one chosen from several, or a
	 * new one when it has none.
	 */
	private async pickFirstInput(session: PreviewSession): Promise<vscode.Uri | undefined> {
		const inputs = await listInputs(session.transformerUri);

		if (inputs.length === 1) {
			return inputs[0];
		}

		if (inputs.length > 1) {
			const picked = await vscode.window.showQuickPick(
				inputs.map(x => ({ label: getInputName(basename(x))!, detail: vscode.workspace.asRelativePath(x), uri: x })),
				{ title: `Preview Input for ${basename(session.transformerUri)}`, placeHolder: 'Choose the input file to run the transformer against' });

			return picked?.uri;
		}

		const create = 'Create Input File';
		const answer = await vscode.window.showInformationMessage(
			`${basename(session.transformerUri)} has no preview input files yet. Create one with the source document to run it against?`,
			create);

		return answer === create ? this.createInput(session.transformerUri) : undefined;
	}

	private useInput(session: PreviewSession, inputUri: vscode.Uri | undefined): void {
		session.inputUri = inputUri;
		session.lastOutput = undefined;

		this.rememberInput(session);
		this.render(session, inputUri ? { kind: 'running' } : { kind: 'noInput' });
		this.schedule(session, 0);
	}

	private async offerToEnableValidation(): Promise<void> {
		const turnOn = 'Turn On Validation';
		const answer = await vscode.window.showWarningMessage(
			'The Jolt preview won\'t work while validation is turned off, since it runs transformers with the language server that validation uses. Turn on validation?',
			turnOn);

		if (answer === turnOn) {
			await enableValidation();
		}
	}

	/**
	 * Gets the transformer that a command applies to: the given transformer or preview document, or else the active one.
	 */
	private getTransformerUri(uri: vscode.Uri | undefined): vscode.Uri | undefined {
		const candidate = uri ?? vscode.window.activeTextEditor?.document.uri;

		if (!candidate) {
			return undefined;
		}

		if (candidate.scheme === previewScheme) {
			return getTransformerUriOf(candidate);
		}

		return getTransformerPrefix(basename(candidate)) ? candidate : undefined;
	}

	/**
	 * Gets the session for a preview document, starting one if needed (e.g. when a preview is restored on startup).
	 */
	private getSession(previewUri: vscode.Uri): PreviewSession | undefined {
		const key = previewUri.toString();
		const existing = this.sessions.get(key);

		if (existing) {
			return existing;
		}

		const transformerUri = getTransformerUriOf(previewUri);

		if (!transformerUri) {
			return undefined;
		}

		const session: PreviewSession = { transformerUri, previewUri, content: renderPreview({ state: { kind: 'running' } }) };

		this.sessions.set(key, session);

		void this.getChosenInput(transformerUri).then(inputUri => {
			session.inputUri ??= inputUri;
			this.schedule(session, 0);
		});

		return session;
	}

	private closeSession(uri: vscode.Uri): void {
		if (uri.scheme !== previewScheme) {
			return;
		}

		const session = this.sessions.get(uri.toString());

		if (session) {
			this.stopSession(session);
			this.sessions.delete(uri.toString());
		}
	}

	private stopSession(session: PreviewSession): void {
		clearTimeout(session.timer);
		session.run?.cancel();
		session.run?.dispose();
	}

	private scheduleFor(uri: vscode.Uri): void {
		if (uri.scheme === previewScheme) {
			return;
		}

		const key = uri.toString();

		for (const session of this.sessions.values()) {
			if (session.transformerUri.toString() === key || session.inputUri?.toString() === key) {
				this.schedule(session);
			}
		}
	}

	private scheduleAll(delay: number): void {
		for (const session of this.sessions.values()) {
			this.schedule(session, delay);
		}
	}

	private schedule(session: PreviewSession, delay = runDelay): void {
		clearTimeout(session.timer);
		session.timer = setTimeout(() => void this.run(session), delay);
	}

	private async run(session: PreviewSession): Promise<void> {
		session.run?.cancel();
		session.run?.dispose();

		const cancellation = new vscode.CancellationTokenSource();
		const status = this.server.status;

		session.run = cancellation;

		if (!session.inputUri) {
			this.render(session, { kind: 'noInput' });
			return;
		}

		switch (status.state) {
			case 'disabled':
				this.render(session, { kind: 'validationDisabled' });
				return;
			case 'starting':
				this.render(session, { kind: 'serverStarting' });
				return;
			case 'failed':
				this.render(session, { kind: 'serverFailed', message: status.message });
				return;
		}

		try {
			const transformer = await vscode.workspace.openTextDocument(session.transformerUri);
			const input = await vscode.workspace.openTextDocument(session.inputUri);
			const inputKey = input.uri.toString();
			const revision = `${this.getDocumentId(input)}:${input.version}`;
			const send = (text?: string) => this.server.transform(transformer, { uri: input.uri, revision, text }, cancellation.token);

			// The input file may be large, so its text is only sent when the server doesn't already have this revision.
			let result = await send(this.sentRevisions.get(inputKey) === revision ? undefined : input.getText());

			if (result?.outcome === 'inputRequired') {
				result = await send(input.getText());
			}

			if (cancellation.token.isCancellationRequested) {
				return;
			}

			if (!result) {
				// The server stopped while running, and a change in its status runs the transformer again.
				return;
			}

			this.sentRevisions.set(inputKey, revision);

			if (result.outcome === 'success') {
				session.lastOutput = result.output ?? 'null';
			}

			this.render(session, { kind: 'result', result });
		} catch (error) {
			if (cancellation.token.isCancellationRequested) {
				return;
			}

			const message = error instanceof Error ? error.message : String(error);
			const result: TransformResult = { outcome: 'error', error: { message: `The transformer couldn't be run: ${message}` } };

			this.render(session, { kind: 'result', result });
		}
	}

	private render(session: PreviewSession, state: PreviewState): void {
		const content = renderPreview({
			inputPath: session.inputUri && vscode.workspace.asRelativePath(session.inputUri),
			state,
			lastOutput: session.lastOutput
		});

		if (content !== session.content) {
			session.content = content;
			this.changed.fire(session.previewUri);
		}
	}

	private getDocumentId(document: vscode.TextDocument): number {
		let id = this.documentIds.get(document);

		if (id === undefined) {
			id = this.nextDocumentId++;
			this.documentIds.set(document, id);
		}

		return id;
	}

	private inputDeleted(uri: vscode.Uri): void {
		this.sentRevisions.delete(uri.toString());

		for (const session of this.sessions.values()) {
			if (session.inputUri?.toString() === uri.toString()) {
				this.useInput(session, undefined);
			}
		}
	}

	/**
	 * Offers to move the input files of renamed transformers, so that they stay with them.
	 */
	private async moveInputs(files: readonly { oldUri: vscode.Uri; newUri: vscode.Uri }[]): Promise<void> {
		for (const { oldUri, newUri } of files) {
			const oldPrefix = getTransformerPrefix(basename(oldUri));
			const newPrefix = getTransformerPrefix(basename(newUri));

			if (!oldPrefix || !newPrefix) {
				continue;
			}

			const oldFolder = getInputsFolder(oldUri);
			const newFolder = getInputsFolder(newUri);

			if (oldFolder.toString() === newFolder.toString() || !await exists(oldFolder) || await exists(newFolder)) {
				continue;
			}

			const move = 'Move Input Files';
			const answer = await vscode.window.showInformationMessage(
				`Move the preview input files of ${basename(oldUri)} to ${vscode.workspace.asRelativePath(newFolder)} as well, so that they stay with ${basename(newUri)}?`,
				move);

			if (answer !== move) {
				continue;
			}

			await vscode.workspace.fs.createDirectory(vscode.Uri.joinPath(newFolder, '..'));
			await vscode.workspace.fs.rename(oldFolder, newFolder, { overwrite: false });

			const chosen = this.getChosenInputs();
			const oldInput = chosen[oldUri.toString()];

			if (oldInput) {
				delete chosen[oldUri.toString()];
				chosen[newUri.toString()] = vscode.Uri.joinPath(newFolder, basename(vscode.Uri.parse(oldInput))).toString();
				await this.context.workspaceState.update(chosenInputsKey, chosen);
			}
		}
	}

	private getChosenInputs(): Record<string, string> {
		return { ...this.context.workspaceState.get<Record<string, string>>(chosenInputsKey, {}) };
	}

	/**
	 * Gets the input file last chosen for a transformer, as long as it still exists.
	 */
	private async getChosenInput(transformerUri: vscode.Uri): Promise<vscode.Uri | undefined> {
		const chosen = this.getChosenInputs()[transformerUri.toString()];
		const uri = chosen ? vscode.Uri.parse(chosen) : undefined;

		return uri && await exists(uri) ? uri : undefined;
	}

	private rememberInput(session: PreviewSession): void {
		const chosen = this.getChosenInputs();

		if (session.inputUri) {
			chosen[session.transformerUri.toString()] = session.inputUri.toString();
		} else {
			delete chosen[session.transformerUri.toString()];
		}

		void this.context.workspaceState.update(chosenInputsKey, chosen);
	}
}

/**
 * Gets the URI of a transformer's preview document, which has the transformer's URI as its query. The ".jsonc" file
 * name ending shows it as JSON with comments, which are used to describe each run.
 */
export function getPreviewUri(transformerUri: vscode.Uri): vscode.Uri {
	const prefix = getTransformerPrefix(basename(transformerUri)) ?? basename(transformerUri);

	return vscode.Uri.from({
		scheme: previewScheme,
		path: vscode.Uri.joinPath(transformerUri, '..', `${prefix}.preview.jsonc`).path,
		query: transformerUri.toString()
	});
}

function getTransformerUriOf(previewUri: vscode.Uri): vscode.Uri | undefined {
	try {
		return previewUri.query ? vscode.Uri.parse(previewUri.query, true) : undefined;
	} catch {
		return undefined;
	}
}

/**
 * Gets the folder of a transformer's input files, e.g. .jolt/orders/ next to orders.jolt.json.
 */
function getInputsFolder(transformerUri: vscode.Uri): vscode.Uri {
	const prefix = getTransformerPrefix(basename(transformerUri)) ?? basename(transformerUri);

	return vscode.Uri.joinPath(transformerUri, '..', inputsRootFolder, prefix);
}

async function listInputs(transformerUri: vscode.Uri): Promise<vscode.Uri[]> {
	const folder = getInputsFolder(transformerUri);

	try {
		const entries = await vscode.workspace.fs.readDirectory(folder);

		return entries
			.filter(([name, type]) => type === vscode.FileType.File && getInputName(name))
			.map(([name]) => name)
			.sort((a, b) => a.localeCompare(b))
			.map(x => vscode.Uri.joinPath(folder, x));
	} catch {
		// The transformer has no inputs folder yet.
		return [];
	}
}

async function exists(uri: vscode.Uri): Promise<boolean> {
	try {
		await vscode.workspace.fs.stat(uri);
		return true;
	} catch {
		return false;
	}
}

function basename(uri: vscode.Uri): string {
	return uri.path.substring(uri.path.lastIndexOf('/') + 1);
}

function isTestDocument(text: string): boolean {
	const root: unknown = parse(text);

	return typeof root === 'object' && root !== null && !Array.isArray(root) && Array.isArray((root as Record<string, unknown>).testGroups);
}
