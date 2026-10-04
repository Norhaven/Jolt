import * as vscode from 'vscode';
import { CustomMethodsService } from './customMethodsService';
import { getExpressionContext } from './language/context';
import { getMethodReferenceAt } from './language/hover';
import { formatDocumentation, formatSignature, getMethodCompletions } from './language/methodCompletions';
import { getVariableCompletions } from './language/variables';
import { LanguageServerController } from './languageServer';

let languageServer: LanguageServerController | undefined;

export function activate(context: vscode.ExtensionContext): void {
	const methods = new CustomMethodsService();

	languageServer = new LanguageServerController(context, methods);

	context.subscriptions.push(
		methods,
		languageServer,
		vscode.languages.registerCompletionItemProvider({ language: 'jolt' }, new JoltCompletionProvider(methods), '#', '>', '@'),
		vscode.languages.registerHoverProvider({ language: 'jolt' }, new JoltHoverProvider(methods)));

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
