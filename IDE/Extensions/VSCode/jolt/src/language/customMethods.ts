import { Node, ParseError, findNodeAtLocation, parseTree } from 'jsonc-parser';
import { LibraryMethod, LibraryParameter } from './library';

/** The path of a custom methods file, relative to a folder that it applies to. */
export const methodsFileName = '.jolt/methods.json';

export interface TextPosition {
	line: number;
	character: number;
}

export interface MethodsFileProblem {
	message: string;
	severity: 'error' | 'warning';
	start: TextPosition;
	end: TextPosition;
}

export interface MethodsFile {
	/** Whether files in parent folders should not be merged into this one. */
	isRoot: boolean;
	/** The valid methods from the file. Invalid entries are skipped and reported as problems. */
	methods: LibraryMethod[];
	problems: MethodsFileProblem[];
}

/**
 * Reads files for the resolver. Paths are opaque keys (e.g. URI strings) chosen by the caller.
 */
export interface MethodsFileSystem {
	/** Reads a file's text, or returns undefined when it does not exist or cannot be read. */
	readFile(path: string): Promise<string | undefined>;
}

const methodName = /^[A-Za-z_]\w*$/;
const parameterKinds = ['value', 'lambda'];
const lambdaVariableName = /^@?([A-Za-z_]\w*)$/;
/** Jolt lambdas bind at most two variables (e.g. @acc;@current: ...). */
const maxLambdaVariables = 2;

/**
 * Parses a custom methods file. External methods are always valid only on property values and are never value
 * generators or unsafe, so those properties are fixed rather than read from the file.
 */
export function parseMethodsFile(text: string, source: string, builtInNames: ReadonlySet<string>): MethodsFile {
	const errors: ParseError[] = [];
	const root = parseTree(text, errors, { allowTrailingComma: true });
	const problems: MethodsFileProblem[] = [];
	const report = (message: string, node: Node | undefined, severity: 'error' | 'warning' = 'warning') => {
		const offset = node?.offset ?? 0;

		problems.push({ message, severity, start: positionAt(text, offset), end: positionAt(text, offset + (node?.length ?? 0)) });
	};

	// The JSON language support already reports the individual syntax errors when the file is open, so a single
	// problem is enough to explain why the file's methods are missing.
	if (errors.length > 0 || root?.type !== 'object') {
		report(
			errors.length > 0
				? 'This file contains invalid JSON, so its methods are not offered until it is fixed.'
				: 'This file must contain a JSON object with a "methods" array.',
			undefined,
			'error');

		return { isRoot: false, methods: [], problems };
	}

	const rootFlag = findNodeAtLocation(root, ['root']);

	if (rootFlag && rootFlag.type !== 'boolean') {
		report('"root" must be true or false.', rootFlag);
	}

	const methodsNode = findNodeAtLocation(root, ['methods']);
	const methods: LibraryMethod[] = [];

	if (methodsNode && methodsNode.type !== 'array') {
		report('"methods" must be an array.', methodsNode);
	}

	for (const entry of methodsNode?.type === 'array' ? methodsNode.children ?? [] : []) {
		const method = parseMethod(entry, source, report);

		if (!method) {
			continue;
		}

		const nameNode = findNodeAtLocation(entry, ['name']);

		if (builtInNames.has(method.name)) {
			report(`"${method.name}" is a standard library method, which Jolt always calls instead of a custom method with the same name. This entry is ignored.`, nameNode);
		} else if (methods.some(x => x.name === method.name)) {
			report(`"${method.name}" is already defined earlier in this file. This entry is ignored.`, nameNode);
		} else {
			methods.push(method);
		}
	}

	return { isRoot: rootFlag?.value === true, methods, problems };
}

function parseMethod(entry: Node, source: string, report: (message: string, node: Node | undefined) => void): LibraryMethod | undefined {
	if (entry.type !== 'object') {
		report('Each method must be an object. This entry is ignored.', entry);
		return undefined;
	}

	const name = findNodeAtLocation(entry, ['name']);

	if (name?.type !== 'string' || !methodName.test(name.value)) {
		report('Each method needs a "name": letters, digits and underscores, not starting with a digit, without the leading #. This entry is ignored.', name ?? entry);
		return undefined;
	}

	const description = getOptionalString(entry, 'description', report);
	const example = getOptionalString(entry, 'example', report);
	const returnType = getOptionalString(entry, 'returnType', report);
	const parametersNode = findNodeAtLocation(entry, ['parameters']);
	const parameters: LibraryParameter[] = [];

	if (parametersNode && parametersNode.type !== 'array') {
		report('"parameters" must be an array. This entry is ignored.', parametersNode);
		return undefined;
	}

	for (const parameter of parametersNode?.children ?? []) {
		const parameterName = parameter.type === 'object' ? findNodeAtLocation(parameter, ['name']) : undefined;
		const kind = parameter.type === 'object' ? findNodeAtLocation(parameter, ['kind']) : undefined;

		if (parameterName?.type !== 'string' || !parameterName.value) {
			report('Each parameter must be an object with a "name". This entry is ignored.', parameterName ?? parameter);
			return undefined;
		}

		if (kind && !parameterKinds.includes(kind.value)) {
			report(`"kind" must be one of: ${parameterKinds.join(', ')}. This entry is ignored.`, kind);
			return undefined;
		}

		const parameterKind = kind?.value ?? 'value';
		const lambdaVariables = getLambdaVariables(parameter, parameterKind, report);

		parameters.push({
			name: parameterName.value,
			kind: parameterKind,
			...(lambdaVariables ? { lambdaVariables } : {}),
			isOptional: false,
			isVariadic: false,
			isLazy: false
		});
	}

	return {
		name: name.value,
		description,
		example,
		validOn: ['propertyValue'],
		isValueGenerator: false,
		isUnsafe: false,
		returnType: returnType?.trim() || undefined,
		parameters,
		source
	};
}

/**
 * Reads a parameter's optional "lambdaVariables". Problems only cause the variables to be ignored, which leaves the
 * lambda showing a single @x variable, rather than skipping the whole method.
 */
function getLambdaVariables(parameter: Node, kind: string, report: (message: string, node: Node | undefined) => void): string[] | undefined {
	const node = parameter.type === 'object' ? findNodeAtLocation(parameter, ['lambdaVariables']) : undefined;

	if (!node) {
		return undefined;
	}

	if (kind !== 'lambda') {
		report('"lambdaVariables" only applies to parameters whose "kind" is "lambda", so it is ignored.', node);
		return undefined;
	}

	const items = node.type === 'array' ? node.children ?? [] : [];
	const names = items.map(x => x.type === 'string' ? lambdaVariableName.exec(x.value)?.[1] : undefined);

	if (node.type !== 'array' || items.length === 0 || items.length > maxLambdaVariables || names.some(x => x === undefined)) {
		report(`"lambdaVariables" must be an array of 1 to ${maxLambdaVariables} variable names (e.g. ["acc", "current"]), so it is ignored.`, node);
		return undefined;
	}

	if (new Set(names).size !== names.length) {
		report('"lambdaVariables" must not repeat a variable name, so it is ignored.', node);
		return undefined;
	}

	return names as string[];
}

function getOptionalString(entry: Node, key: string, report: (message: string, node: Node | undefined) => void): string | undefined {
	const node = findNodeAtLocation(entry, [key]);

	if (node && node.type !== 'string') {
		report(`"${key}" must be a string, so it is ignored.`, node);
		return undefined;
	}

	return node?.value;
}

function positionAt(text: string, offset: number): TextPosition {
	const before = text.slice(0, offset);
	const lineStart = before.lastIndexOf('\n') + 1;

	return { line: before.split('\n').length - 1, character: offset - lineStart };
}

/**
 * Merges methods files that apply to a transformer, given closest first. A method in a closer file replaces a
 * method with the same name in a file further up the folder hierarchy.
 */
export function mergeMethodsFiles(files: readonly MethodsFile[]): LibraryMethod[] {
	const merged = new Map<string, LibraryMethod>();

	for (const file of files) {
		for (const method of file.methods) {
			if (!merged.has(method.name)) {
				merged.set(method.name, method);
			}
		}
	}

	return [...merged.values()];
}

/**
 * Resolves the custom methods for transformers by merging the methods files from a transformer's folder up the
 * folder hierarchy, stopping at a file marked as the root. Each file is read once and cached by path, including
 * files that do not exist, until it is invalidated.
 */
export class CustomMethodsResolver {
	private readonly files = new Map<string, Promise<MethodsFile | undefined>>();

	constructor(
		private readonly fileSystem: MethodsFileSystem,
		private readonly builtInNames: ReadonlySet<string>,
		private readonly describe: (path: string) => string,
		private readonly onFileRead?: (path: string, file: MethodsFile | undefined) => void) {
	}

	/**
	 * Gets the merged custom methods, given the paths where methods files may exist, closest folder first.
	 */
	async getMethods(candidatePaths: readonly string[]): Promise<LibraryMethod[]> {
		const applicable: MethodsFile[] = [];

		for (const path of candidatePaths) {
			const file = await this.getFile(path);

			if (!file) {
				continue;
			}

			applicable.push(file);

			if (file.isRoot) {
				break;
			}
		}

		return mergeMethodsFiles(applicable);
	}

	/**
	 * Discards the cached contents of a file (or the knowledge that it does not exist), so that it is read again.
	 */
	invalidate(path: string): void {
		this.files.delete(path);
	}

	/**
	 * Discards and re-reads a file, e.g. after it was created or changed.
	 */
	refresh(path: string): Promise<MethodsFile | undefined> {
		this.invalidate(path);
		return this.getFile(path);
	}

	clear(): void {
		this.files.clear();
	}

	private getFile(path: string): Promise<MethodsFile | undefined> {
		let file = this.files.get(path);

		if (!file) {
			file = this.load(path);
			this.files.set(path, file);
		}

		return file;
	}

	private async load(path: string): Promise<MethodsFile | undefined> {
		const text = await this.fileSystem.readFile(path);
		const file = text === undefined ? undefined : parseMethodsFile(text, this.describe(path), this.builtInNames);

		this.onFileRead?.(path, file);

		return file;
	}
}
