import * as vscode from 'vscode';
import { CustomMethodsResolver, MethodsFile, methodsFileName } from './language/customMethods';
import { LibraryMethod, libraryMethods } from './language/library';

/**
 * Provides the methods available to each transformer: the standard library plus any custom methods from
 * .jolt/methods.json files in the transformer's folder and its parent folders, up to the workspace folder root.
 */
export class CustomMethodsService implements vscode.Disposable {
	private readonly diagnostics = vscode.languages.createDiagnosticCollection('jolt-methods');
	private readonly resolver: CustomMethodsResolver;
	private readonly disposables: vscode.Disposable[] = [this.diagnostics];

	constructor() {
		const decoder = new TextDecoder();

		this.resolver = new CustomMethodsResolver(
			{
				readFile: async path => {
					try {
						return decoder.decode(await vscode.workspace.fs.readFile(vscode.Uri.parse(path)));
					} catch {
						return undefined;
					}
				}
			},
			new Set(libraryMethods.map(x => x.name)),
			path => vscode.workspace.asRelativePath(vscode.Uri.parse(path)),
			(path, file) => this.publishProblems(vscode.Uri.parse(path), file));

		// Edits only take effect once a methods file is saved, as the watcher reports changes on disk.
		const watcher = vscode.workspace.createFileSystemWatcher(`**/${methodsFileName}`);

		this.disposables.push(
			watcher,
			watcher.onDidCreate(uri => this.resolver.refresh(uri.toString())),
			watcher.onDidChange(uri => this.resolver.refresh(uri.toString())),
			watcher.onDidDelete(uri => {
				this.resolver.invalidate(uri.toString());
				this.diagnostics.delete(uri);
			}),
			vscode.workspace.onDidOpenTextDocument(document => this.preload(document)),
			vscode.workspace.onDidGrantWorkspaceTrust(() => vscode.workspace.textDocuments.forEach(x => this.preload(x))));

		vscode.workspace.textDocuments.forEach(x => this.preload(x));
	}

	/**
	 * Gets the methods available to the given transformer document, standard library methods first.
	 */
	async getMethodsFor(document: vscode.TextDocument): Promise<readonly LibraryMethod[]> {
		// A methods file in an untrusted workspace could come from anyone, so only the standard library is offered.
		if (!vscode.workspace.isTrusted) {
			return libraryMethods;
		}

		const customMethods = await this.resolver.getMethods(getCandidateFiles(document.uri).map(x => x.toString()));

		return customMethods.length > 0 ? [...libraryMethods, ...customMethods] : libraryMethods;
	}

	dispose(): void {
		this.disposables.forEach(x => x.dispose());
	}

	/**
	 * Reads the methods files for a transformer ahead of time, so that they are ready when completions are requested.
	 */
	private preload(document: vscode.TextDocument): void {
		if (document.languageId === 'jolt') {
			void this.getMethodsFor(document);
		}
	}

	private publishProblems(uri: vscode.Uri, file: MethodsFile | undefined): void {
		const problems = file?.problems ?? [];

		this.diagnostics.set(uri, problems.map(problem => {
			const diagnostic = new vscode.Diagnostic(
				new vscode.Range(problem.start.line, problem.start.character, problem.end.line, problem.end.character),
				problem.message,
				problem.severity === 'error' ? vscode.DiagnosticSeverity.Error : vscode.DiagnosticSeverity.Warning);

			diagnostic.source = 'Jolt';

			return diagnostic;
		}));
	}
}

/**
 * Gets the paths where methods files that apply to a document may exist, closest first: the document's folder and
 * each parent folder up to the root of its workspace folder. A document outside of the workspace only uses its own
 * folder, and an unsaved document uses none.
 */
function getCandidateFiles(documentUri: vscode.Uri): vscode.Uri[] {
	if (documentUri.scheme === 'untitled') {
		return [];
	}

	const workspaceRoot = vscode.workspace.getWorkspaceFolder(documentUri)?.uri;
	const candidates: vscode.Uri[] = [];

	for (let folder = vscode.Uri.joinPath(documentUri, '..'); ; ) {
		candidates.push(vscode.Uri.joinPath(folder, methodsFileName));

		const parent = vscode.Uri.joinPath(folder, '..');

		if (!workspaceRoot || folder.toString() === workspaceRoot.toString() || parent.toString() === folder.toString()) {
			return candidates;
		}

		folder = parent;
	}
}
