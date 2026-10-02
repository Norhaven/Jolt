import * as vscode from 'vscode';
import { getExpressionContext } from './language/context';
import { getMethodReferenceAt } from './language/hover';
import { libraryMethods } from './language/library';
import { formatDocumentation, formatSignature, getMethodCompletions } from './language/methodCompletions';

export function activate(context: vscode.ExtensionContext): void {
	context.subscriptions.push(
		vscode.languages.registerCompletionItemProvider({ language: 'jolt' }, new JoltCompletionProvider(), '#', '>'),
		vscode.languages.registerHoverProvider({ language: 'jolt' }, new JoltHoverProvider()));
}

export function deactivate(): void {
}

class JoltCompletionProvider implements vscode.CompletionItemProvider {
	provideCompletionItems(document: vscode.TextDocument, position: vscode.Position): vscode.CompletionItem[] | undefined {
		const expressionContext = getExpressionContext(document.getText(), document.offsetAt(position));

		if (!expressionContext) {
			return undefined;
		}

		const includeUnsafeMethods = vscode.workspace
			.getConfiguration('jolt', document)
			.get<boolean>('completion.includeUnsafeMethods', false);

		const completions = getMethodCompletions(expressionContext, libraryMethods, { includeUnsafeMethods });

		return completions?.map(completion => {
			const signature = formatSignature(completion.method, completion.isPiped);
			const item = new vscode.CompletionItem(
				{
					label: completion.method.name,
					detail: signature.substring(completion.method.name.length),
					description: completion.method.isUnsafe ? 'unsafe' : undefined
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
	provideHover(document: vscode.TextDocument, position: vscode.Position): vscode.Hover | undefined {
		const reference = getMethodReferenceAt(document.lineAt(position.line).text, position.character);

		// Method references only exist within Jolt expressions, which are always inside JSON strings.
		if (!reference || !getExpressionContext(document.getText(), document.offsetAt(position))) {
			return undefined;
		}

		const method = libraryMethods.find(x => x.name === reference.name);

		if (!method) {
			return undefined;
		}

		return new vscode.Hover(
			new vscode.MarkdownString(formatDocumentation(method)),
			new vscode.Range(position.line, reference.start, position.line, reference.end));
	}
}
