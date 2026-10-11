import * as vscode from 'vscode';
import { CustomMethodsService } from './customMethodsService';
import { getExpressionContext, getStringLocation } from './language/context';
import { getFormattingEdits, OffsetRange } from './language/formatting';
import { getMethodReferenceAt } from './language/hover';
import { formatDocumentation, formatParameters, formatSignature, getMethodCompletions } from './language/methodCompletions';
import { getCallContext } from './language/signatureHelp';
import { getDocumentSymbols, JoltSymbol, JoltSymbolKind } from './language/symbols';
import { getVariableCompletions } from './language/variables';
import { LanguageServerController } from './languageServer';
import { JoltStatus, restartCommand, showOutputCommand } from './status';

let languageServer: LanguageServerController | undefined;

export function activate(context: vscode.ExtensionContext): void {
	const methods = new CustomMethodsService();
	const server = new LanguageServerController(context, methods);
	const selector: vscode.DocumentSelector = { language: 'jolt' };
	const formatter = new JoltFormattingProvider();

	languageServer = server;

	context.subscriptions.push(
		methods,
		server,
		new JoltStatus(server, methods),
		vscode.commands.registerCommand(restartCommand, () => server.restart()),
		vscode.commands.registerCommand(showOutputCommand, () => server.output.show()),
		vscode.languages.registerCompletionItemProvider(selector, new JoltCompletionProvider(methods), '#', '>', '@'),
		vscode.languages.registerHoverProvider(selector, new JoltHoverProvider(methods)),
		vscode.languages.registerSignatureHelpProvider(
			selector,
			new JoltSignatureHelpProvider(methods),
			{ triggerCharacters: ['('], retriggerCharacters: [','] }),
		vscode.languages.registerDocumentFormattingEditProvider(selector, formatter),
		vscode.languages.registerDocumentRangeFormattingEditProvider(selector, formatter),
		vscode.languages.registerDocumentSymbolProvider(selector, new JoltDocumentSymbolProvider()));

	// Completions and hover don't depend on the language server, so they work while it starts (or if it can't).
	void languageServer.update();
}

export function deactivate(): Promise<void> | undefined {
	// Stopping the server asks it to shut down and exit, rather than leaving the process to be ended.
	return languageServer?.stop();
}

class JoltCompletionProvider implements vscode.CompletionItemProvider {
	constructor(private readonly methods: CustomMethodsService) {
	}

	async provideCompletionItems(document: vscode.TextDocument, position: vscode.Position): Promise<vscode.CompletionItem[] | undefined> {
		const variableCompletions = getVariableCompletions(document.getText(), document.offsetAt(position));

		if (variableCompletions) {
			const range = new vscode.Range(position.translate(0, -variableCompletions.replaceLength), position);

			return variableCompletions.variables.map((variable, index) => {
				const item = new vscode.CompletionItem(
					{ label: `@${variable.name}`, description: variable.description },
					vscode.CompletionItemKind.Variable);

				item.range = range;
				// Keeps the innermost variables at the top, rather than sorting alphabetically.
				item.sortText = String(index).padStart(4, '0');

				return item;
			});
		}

		const expressionContext = getExpressionContext(document.getText(), document.offsetAt(position));

		if (!expressionContext) {
			return undefined;
		}

		const includeUnsafeMethods = vscode.workspace
			.getConfiguration('jolt', document)
			.get<boolean>('completion.includeUnsafeMethods', false);

		const methods = await this.methods.getMethodsFor(document);
		const completions = getMethodCompletions(expressionContext, methods, { includeUnsafeMethods });

		return completions?.map(completion => {
			const signature = formatSignature(completion.method, completion.isPiped);
			const item = new vscode.CompletionItem(
				{
					label: completion.method.name,
					detail: signature.substring(completion.method.name.length),
					description: completion.method.source ?? (completion.method.isUnsafe ? 'unsafe' : undefined)
				},
				vscode.CompletionItemKind.Function);

			item.insertText = new vscode.SnippetString(completion.snippet);
			item.range = new vscode.Range(position.translate(0, -completion.replaceLength), position);
			item.documentation = new vscode.MarkdownString(formatDocumentation(completion.method));

			return item;
		});
	}
}

class JoltHoverProvider implements vscode.HoverProvider {
	constructor(private readonly methods: CustomMethodsService) {
	}

	async provideHover(document: vscode.TextDocument, position: vscode.Position): Promise<vscode.Hover | undefined> {
		const reference = getMethodReferenceAt(document.lineAt(position.line).text, position.character);

		// Method references only exist within Jolt expressions, which are always inside JSON strings.
		if (!reference || !getExpressionContext(document.getText(), document.offsetAt(position))) {
			return undefined;
		}

		// Standard library methods come first, matching Jolt, which calls them in preference to custom methods.
		const methods = await this.methods.getMethodsFor(document);
		const method = methods.find(x => x.name === reference.name);

		if (!method) {
			return undefined;
		}

		return new vscode.Hover(
			new vscode.MarkdownString(formatDocumentation(method)),
			new vscode.Range(position.line, reference.start, position.line, reference.end));
	}
}

class JoltSignatureHelpProvider implements vscode.SignatureHelpProvider {
	constructor(private readonly methods: CustomMethodsService) {
	}

	async provideSignatureHelp(document: vscode.TextDocument, position: vscode.Position): Promise<vscode.SignatureHelp | undefined> {
		const location = getStringLocation(document.getText(), document.offsetAt(position));
		const call = location && getCallContext(location.textBeforeCursor);

		if (!call) {
			return undefined;
		}

		const methods = await this.methods.getMethodsFor(document);
		const method = methods.find(x => x.name === call.name);

		if (!method) {
			return undefined;
		}

		const signature = new vscode.SignatureInformation(
			'#' + formatSignature(method, call.isPiped),
			method.description ? new vscode.MarkdownString(method.description) : undefined);

		// Parameters are identified by their position within the label, which starts with "#name(" and separates them with ", ".
		let start = method.name.length + 2;

		signature.parameters = formatParameters(method, call.isPiped).map(label => {
			const parameter = new vscode.ParameterInformation([start, start + label.length]);

			start += label.length + 2;

			return parameter;
		});

		// A variadic parameter is always last, and takes every remaining argument.
		const parameters = method.parameters.slice(call.isPiped ? 1 : 0);
		const isLastVariadic = parameters[parameters.length - 1]?.isVariadic === true;

		signature.activeParameter = isLastVariadic ? Math.min(call.argumentIndex, parameters.length - 1) : call.argumentIndex;

		const help = new vscode.SignatureHelp();

		help.signatures = [signature];
		help.activeSignature = 0;

		return help;
	}
}

class JoltFormattingProvider implements vscode.DocumentFormattingEditProvider, vscode.DocumentRangeFormattingEditProvider {
	provideDocumentFormattingEdits(document: vscode.TextDocument, options: vscode.FormattingOptions): vscode.TextEdit[] {
		return this.format(document, undefined, options);
	}

	provideDocumentRangeFormattingEdits(document: vscode.TextDocument, range: vscode.Range, options: vscode.FormattingOptions): vscode.TextEdit[] {
		const offset = document.offsetAt(range.start);

		return this.format(document, { offset, length: document.offsetAt(range.end) - offset }, options);
	}

	private format(document: vscode.TextDocument, range: OffsetRange | undefined, options: vscode.FormattingOptions): vscode.TextEdit[] {
		const edits = getFormattingEdits(document.getText(), range, {
			tabSize: options.tabSize,
			insertSpaces: options.insertSpaces,
			eol: document.eol === vscode.EndOfLine.CRLF ? '\r\n' : '\n'
		});

		return edits.map(x => vscode.TextEdit.replace(
			new vscode.Range(document.positionAt(x.offset), document.positionAt(x.offset + x.length)),
			x.content));
	}
}

const symbolKinds: Record<JoltSymbolKind, vscode.SymbolKind> = {
	method: vscode.SymbolKind.Function,
	variable: vscode.SymbolKind.Variable,
	object: vscode.SymbolKind.Object,
	array: vscode.SymbolKind.Array,
	string: vscode.SymbolKind.String,
	number: vscode.SymbolKind.Number,
	boolean: vscode.SymbolKind.Boolean,
	null: vscode.SymbolKind.Null
};

class JoltDocumentSymbolProvider implements vscode.DocumentSymbolProvider {
	provideDocumentSymbols(document: vscode.TextDocument): vscode.DocumentSymbol[] {
		const toDocumentSymbol = (symbol: JoltSymbol): vscode.DocumentSymbol => {
			const range = (offset: number, length: number) =>
				new vscode.Range(document.positionAt(offset), document.positionAt(offset + length));

			const documentSymbol = new vscode.DocumentSymbol(
				// VS Code rejects symbols with blank names, which an empty property name would otherwise have.
				symbol.name.trim() ? symbol.name : '""',
				symbol.detail,
				symbolKinds[symbol.kind],
				range(symbol.offset, symbol.length),
				range(symbol.selectionOffset, symbol.selectionLength));

			documentSymbol.children = symbol.children.map(toDocumentSymbol);

			return documentSymbol;
		};

		return getDocumentSymbols(document.getText()).map(toDocumentSymbol);
	}
}
