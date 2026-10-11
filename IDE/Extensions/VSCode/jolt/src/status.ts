import * as vscode from 'vscode';
import { CustomMethodsService } from './customMethodsService';
import { LanguageServerController, ServerStatus } from './languageServer';

export const restartCommand = 'jolt.restartLanguageServer';
export const showOutputCommand = 'jolt.showLanguageServerOutput';

/**
 * Shows, in the language status area of a transformer's editor, whether transformers are being validated and which
 * .jolt/methods.json files apply to the transformer.
 */
export class JoltStatus implements vscode.Disposable {
	private readonly validation = vscode.languages.createLanguageStatusItem('jolt.validation', { language: 'jolt' });
	private readonly methodsFiles = vscode.languages.createLanguageStatusItem('jolt.methodsFiles', { language: 'jolt' });
	private readonly disposables: vscode.Disposable[] = [this.validation, this.methodsFiles];
	private methodsFilesUpdate = 0;

	constructor(server: LanguageServerController, private readonly methods: CustomMethodsService) {
		this.validation.name = 'Jolt Validation';
		this.methodsFiles.name = 'Jolt Custom Methods';

		this.showValidation(server.status);
		void this.updateMethodsFiles();

		this.disposables.push(
			server.onDidChangeStatus(x => this.showValidation(x)),
			// Also raised when the workspace becomes trusted.
			methods.onDidChange(() => void this.updateMethodsFiles()),
			vscode.window.onDidChangeActiveTextEditor(() => void this.updateMethodsFiles()));
	}

	dispose(): void {
		this.disposables.forEach(x => x.dispose());
	}

	private showValidation(status: ServerStatus): void {
		const item = this.validation;

		item.busy = status.state === 'starting';
		item.severity = status.state === 'failed' ? vscode.LanguageStatusSeverity.Error : vscode.LanguageStatusSeverity.Information;

		switch (status.state) {
			case 'starting':
				item.text = 'Starting Jolt validation';
				item.detail = undefined;
				item.command = { title: 'Show Output', command: showOutputCommand };
				break;
			case 'running':
				item.text = 'Jolt validation';
				item.detail = 'Validating with the Jolt language server';
				item.command = { title: 'Restart', command: restartCommand };
				break;
			case 'disabled':
				item.text = 'Jolt validation off';
				item.detail = undefined;
				item.command = { title: 'Settings', command: 'workbench.action.openSettings', arguments: ['jolt.validation.enabled'] };
				break;
			case 'failed':
				item.text = 'Jolt validation failed';
				item.detail = status.message;
				item.command = { title: 'Show Output', command: showOutputCommand };
				break;
		}
	}

	private async updateMethodsFiles(): Promise<void> {
		const document = vscode.window.activeTextEditor?.document;

		if (document?.languageId !== 'jolt') {
			return;
		}

		// Finding the files may take a while, during which another editor may become active, so only the latest applies.
		const update = ++this.methodsFilesUpdate;
		const files = await this.methods.getMethodsFilesFor(document);

		if (update !== this.methodsFilesUpdate) {
			return;
		}

		const item = this.methodsFiles;

		if (!files) {
			item.text = 'Custom methods off';
			item.detail = 'Only used in a trusted workspace';
			item.command = { title: 'Manage Trust', command: 'workbench.trust.manage' };
		} else if (files.length === 0) {
			item.text = 'No custom methods';
			item.detail = 'No .jolt/methods.json file applies';
			item.command = undefined;
		} else {
			item.text = files.length === 1 ? '1 methods file' : `${files.length} methods files`;
			item.detail = files.map(x => vscode.workspace.asRelativePath(x)).join(', ');
			item.command = { title: 'Open', tooltip: 'Open the closest methods file', command: 'vscode.open', arguments: [files[0]] };
		}
	}
}
