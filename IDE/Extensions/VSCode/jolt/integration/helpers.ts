import * as vscode from 'vscode';

export const extensionId = 'Norhaven.jolt';

/** Marks the cursor position within a test transformer's text. */
export const cursor = '¦';

export function workspaceRoot(): vscode.Uri {
	const folder = vscode.workspace.workspaceFolders?.[0];

	if (!folder) {
		throw new Error('The tests must be run with the fixture workspace open.');
	}

	return folder.uri;
}

export function workspaceFile(...segments: string[]): vscode.Uri {
	return vscode.Uri.joinPath(workspaceRoot(), ...segments);
}

export async function activateExtension(): Promise<void> {
	const extension = vscode.extensions.getExtension(extensionId);

	if (!extension) {
		throw new Error(`${extensionId} is not loaded.`);
	}

	await extension.activate();
}

let documentCount = 0;

/**
 * Writes a transformer to a new file in the workspace (so that its methods file applies) and opens it in an editor.
 * The text may mark the cursor with ¦, whose position is returned.
 */
export async function openTransformer(textWithCursor: string): Promise<{ document: vscode.TextDocument; position: vscode.Position }> {
	const offset = textWithCursor.indexOf(cursor);
	const text = textWithCursor.replace(cursor, '');
	const uri = workspaceFile(`Test${++documentCount}.jolt.json`);

	await vscode.workspace.fs.writeFile(uri, new TextEncoder().encode(text));

	const document = await vscode.workspace.openTextDocument(uri);

	await vscode.window.showTextDocument(document);
	await activateExtension();

	return { document, position: document.positionAt(Math.max(offset, 0)) };
}

export async function openWorkspaceFile(...segments: string[]): Promise<vscode.TextDocument> {
	const document = await vscode.workspace.openTextDocument(workspaceFile(...segments));

	await vscode.window.showTextDocument(document);
	await activateExtension();

	return document;
}

export async function closeAllEditors(): Promise<void> {
	await vscode.commands.executeCommand('workbench.action.closeAllEditors');
}

export function labelOf(item: vscode.CompletionItem): string {
	return typeof item.label === 'string' ? item.label : item.label.label;
}

export async function getCompletionLabels(document: vscode.TextDocument, position: vscode.Position): Promise<string[]> {
	const list = await vscode.commands.executeCommand<vscode.CompletionList>(
		'vscode.executeCompletionItemProvider', document.uri, position);

	return list.items.map(labelOf);
}

export function hoverText(hovers: vscode.Hover[]): string {
	return hovers
		.flatMap(x => x.contents)
		.map(x => typeof x === 'string' ? x : x.value)
		.join('\n');
}

/**
 * Waits until the given check returns a value other than undefined or false, failing with the message if it doesn't
 * within the timeout. Used for what happens in the background, such as validation by the language server.
 */
export async function waitFor<T>(
	check: () => T | undefined | false | Promise<T | undefined | false>,
	message: string,
	timeout = 30000): Promise<T> {

	const end = Date.now() + timeout;

	for (;;) {
		const result = await check();

		if (result !== undefined && result !== false) {
			return result;
		}

		if (Date.now() > end) {
			throw new Error(`Timed out after ${timeout}ms: ${message}`);
		}

		await new Promise(resolve => setTimeout(resolve, 100));
	}
}

/** The diagnostics that the language server reports for a document. */
export function joltDiagnostics(uri: vscode.Uri): vscode.Diagnostic[] {
	return vscode.languages.getDiagnostics(uri).filter(x => x.source === 'Jolt');
}

export function codeOf(diagnostic: vscode.Diagnostic): string {
	return String(typeof diagnostic.code === 'object' ? diagnostic.code.value : diagnostic.code);
}

/** Waits for the language server to report a diagnostic with the given code. */
export function waitForDiagnostic(uri: vscode.Uri, code: string): Promise<vscode.Diagnostic> {
	return waitFor(
		() => joltDiagnostics(uri).find(x => codeOf(x) === code),
		`a ${code} diagnostic for ${vscode.workspace.asRelativePath(uri)}`);
}

/**
 * Changes a setting for the duration of a test, restoring it afterwards.
 */
export async function withSetting<T>(section: string, value: unknown, action: () => Promise<T>): Promise<T> {
	const configuration = vscode.workspace.getConfiguration();
	const previous = configuration.inspect(section)?.globalValue;

	await configuration.update(section, value, vscode.ConfigurationTarget.Global);

	try {
		return await action();
	} finally {
		await configuration.update(section, previous, vscode.ConfigurationTarget.Global);
	}
}
