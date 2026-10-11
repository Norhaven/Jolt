import * as vscode from 'vscode';
import { CustomMethodsService } from './customMethodsService';
import { getExpressionContext } from './language/context';
import { methodsFileName } from './language/customMethods';
import { addToMethodsFile, getDeclarationInsertion, getSimilarNames, inferParameters, MethodsFileEntry } from './language/quickFixes';
import { getVariablesInScopeAt } from './language/variables';

/** The language server's code for a call to a method that isn't a library method or in an applicable methods file. */
const unknownMethodCode = 'JLT108';
/** The language server's code for a reference to a variable that isn't in scope. */
const undeclaredVariableCode = 'JLT562';

const methodName = /^(#?)([A-Za-z_]\w*)$/;
const variableName = /^(@?)([A-Za-z_]\w*)$/;

/**
 * Adds a method to a methods file. Reading and changing the file waits until the action is chosen.
 */
class AddMethodAction extends vscode.CodeAction {
	constructor(title: string, readonly methodsFile: vscode.Uri, readonly exists: boolean, readonly entry: MethodsFileEntry) {
		super(title, vscode.CodeActionKind.QuickFix);
	}
}

/**
 * Offers quick fixes for the language server's diagnostics: similar names for unknown methods and undeclared
 * variables, adding an unknown method to a methods file, and declaring an undeclared variable.
 */
export class JoltCodeActionProvider implements vscode.CodeActionProvider<vscode.CodeAction> {
	static readonly metadata: vscode.CodeActionProviderMetadata = { providedCodeActionKinds: [vscode.CodeActionKind.QuickFix] };

	constructor(private readonly methods: CustomMethodsService) {
	}

	async provideCodeActions(document: vscode.TextDocument, _range: vscode.Range, context: vscode.CodeActionContext): Promise<vscode.CodeAction[]> {
		const actions: vscode.CodeAction[] = [];

		for (const diagnostic of context.diagnostics) {
			switch (getCode(diagnostic)) {
				case unknownMethodCode:
					actions.push(...await this.fixUnknownMethod(document, diagnostic));
					break;
				case undeclaredVariableCode:
					actions.push(...fixUndeclaredVariable(document, diagnostic));
					break;
			}
		}

		return actions;
	}

	async resolveCodeAction(action: vscode.CodeAction): Promise<vscode.CodeAction> {
		if (!(action instanceof AddMethodAction)) {
			return action;
		}

		// Open documents include unsaved changes, so they are read from the editor rather than from disk.
		const text = action.exists ? (await vscode.workspace.openTextDocument(action.methodsFile)).getText() : undefined;
		const change = addToMethodsFile(text, action.entry);

		if (!change) {
			void vscode.window.showErrorMessage(`${vscode.workspace.asRelativePath(action.methodsFile)} contains invalid JSON, so the method can't be added to it.`);
			return action;
		}

		const edit = new vscode.WorkspaceEdit();

		if (action.exists) {
			const document = await vscode.workspace.openTextDocument(action.methodsFile);

			for (const x of change.edits) {
				edit.replace(action.methodsFile, new vscode.Range(document.positionAt(x.offset), document.positionAt(x.offset + x.length)), x.content);
			}
		} else {
			edit.createFile(action.methodsFile, { contents: new TextEncoder().encode(change.newText) });
		}

		action.edit = edit;

		// Shows the new entry, whose description and parameter names are placeholders to fill in.
		const position = positionAt(change.newText, change.nameOffset);
		action.command = {
			title: 'Open Methods File',
			command: 'vscode.open',
			arguments: [action.methodsFile, { selection: new vscode.Range(position, position.translate(0, action.entry.name.length)) }]
		};

		return action;
	}

	private async fixUnknownMethod(document: vscode.TextDocument, diagnostic: vscode.Diagnostic): Promise<vscode.CodeAction[]> {
		const reference = methodName.exec(document.getText(diagnostic.range));

		if (!reference) {
			return [];
		}

		const [, hash, name] = reference;
		const nameRange = new vscode.Range(diagnostic.range.start.translate(0, hash.length), diagnostic.range.end);
		const actions: vscode.CodeAction[] = [];

		// Only methods that are valid at the call's position are suggested, matching completions.
		const target = getExpressionContext(document.getText(), document.offsetAt(nameRange.start))?.target;
		const includeUnsafeMethods = vscode.workspace.getConfiguration('jolt', document).get<boolean>('completion.includeUnsafeMethods', false);
		const candidates = (await this.methods.getMethodsFor(document))
			.filter(x => (includeUnsafeMethods || !x.isUnsafe) && (!target || x.validOn.includes(target)))
			.map(x => x.name);

		getSimilarNames(name, candidates).forEach((suggestion, index) => {
			actions.push(createReplacement(`Change to '#${suggestion}'`, document, nameRange, suggestion, diagnostic, index === 0));
		});

		const addMethod = await this.createAddMethodAction(document, nameRange, name);

		if (addMethod) {
			addMethod.diagnostics = [diagnostic];
			actions.push(addMethod);
		}

		return actions;
	}

	/**
	 * Creates an action that adds the method to the closest applicable methods file, or creates one in the transformer's
	 * folder when none applies. Methods files are only used in a trusted workspace, so none is offered otherwise.
	 */
	private async createAddMethodAction(document: vscode.TextDocument, nameRange: vscode.Range, name: string): Promise<AddMethodAction | undefined> {
		const files = await this.methods.getMethodsFilesFor(document);

		if (!files || (files.length === 0 && document.uri.scheme === 'untitled')) {
			return undefined;
		}

		const closest = files[0];
		const methodsFile = closest ?? vscode.Uri.joinPath(document.uri, '..', methodsFileName);
		const title = closest
			? `Add '#${name}' to ${vscode.workspace.asRelativePath(closest)}`
			: `Create ${methodsFileName} with '#${name}'`;

		// Expressions are always on one line (within a JSON string), so the call's arguments follow on the same line.
		const textAfterName = document.lineAt(nameRange.end.line).text.substring(nameRange.end.character);

		return new AddMethodAction(title, methodsFile, closest !== undefined, {
			name,
			description: '',
			parameters: inferParameters(textAfterName)
		});
	}
}

function fixUndeclaredVariable(document: vscode.TextDocument, diagnostic: vscode.Diagnostic): vscode.CodeAction[] {
	const reference = variableName.exec(document.getText(diagnostic.range));

	if (!reference) {
		return [];
	}

	const [, at, name] = reference;
	const text = document.getText();
	const atOffset = document.offsetAt(diagnostic.range.start) - (at ? 0 : 1);
	const nameRange = new vscode.Range(document.positionAt(atOffset + 1), diagnostic.range.end);
	const actions: vscode.CodeAction[] = [];

	const inScope = getVariablesInScopeAt(text, atOffset).map(x => x.name);

	getSimilarNames(name, inScope).forEach((suggestion, index) => {
		actions.push(createReplacement(`Change to '@${suggestion}'`, document, nameRange, suggestion, diagnostic, index === 0));
	});

	const insertion = getDeclarationInsertion(text, atOffset, name);

	if (insertion) {
		const declare = new vscode.CodeAction(`Declare '@${name}'`, vscode.CodeActionKind.QuickFix);

		declare.edit = new vscode.WorkspaceEdit();
		declare.edit.insert(document.uri, document.positionAt(insertion.offset), insertion.text);
		declare.diagnostics = [diagnostic];
		actions.push(declare);
	}

	return actions;
}

function createReplacement(
	title: string,
	document: vscode.TextDocument,
	range: vscode.Range,
	replacement: string,
	diagnostic: vscode.Diagnostic,
	isPreferred: boolean): vscode.CodeAction {

	const action = new vscode.CodeAction(title, vscode.CodeActionKind.QuickFix);

	action.edit = new vscode.WorkspaceEdit();
	action.edit.replace(document.uri, range, replacement);
	action.diagnostics = [diagnostic];
	// The closest suggestion is applied by "Auto Fix", which is what a misspelling usually needs.
	action.isPreferred = isPreferred;

	return action;
}

function getCode(diagnostic: vscode.Diagnostic): string | undefined {
	if (diagnostic.source !== 'Jolt') {
		return undefined;
	}

	const code = typeof diagnostic.code === 'object' ? diagnostic.code.value : diagnostic.code;

	return code === undefined ? undefined : String(code);
}

function positionAt(text: string, offset: number): vscode.Position {
	const before = text.slice(0, offset);
	const lineStart = before.lastIndexOf('\n') + 1;

	return new vscode.Position(before.split('\n').length - 1, offset - lineStart);
}
