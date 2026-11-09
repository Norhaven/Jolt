/**
 * The file name ending of transformers, whose name before it (the prefix) names the folder of their preview inputs.
 */
export const transformerSuffix = '.jolt.json';

/**
 * The file name ending of preview input files, e.g. .jolt/orders/multi-item-order.input.json for orders.jolt.json.
 */
export const inputSuffix = '.input.json';

/**
 * The folder, next to a transformer, that holds a folder of preview inputs for each transformer (along with the
 * methods.json file that describes custom methods).
 */
export const inputsRootFolder = '.jolt';

/** The outcome of running a transformer with the language server's "jolt/transform" request. */
export type TransformOutcome = 'success' | 'skipped' | 'inputRequired' | 'invalidInput' | 'error' | 'timedOut';

export interface TransformError {
	code?: string;
	message: string;
	innerErrors?: TransformError[];
}

export interface TransformResult {
	outcome: TransformOutcome;
	output?: string;
	error?: TransformError;
	transformerErrorCount?: number;
	warnings?: string[];
	elapsedMilliseconds?: number;
}

/**
 * Gets a transformer's prefix (e.g. "orders" for orders.jolt.json), or undefined when it isn't named as a transformer.
 */
export function getTransformerPrefix(fileName: string): string | undefined {
	if (!fileName.toLowerCase().endsWith(transformerSuffix) || fileName.length === transformerSuffix.length) {
		return undefined;
	}

	return fileName.substring(0, fileName.length - transformerSuffix.length);
}

/**
 * Gets an input file's name without its ending (e.g. "multi-item-order"), or undefined when it isn't an input file.
 */
export function getInputName(fileName: string): string | undefined {
	if (!fileName.toLowerCase().endsWith(inputSuffix) || fileName.length === inputSuffix.length) {
		return undefined;
	}

	return fileName.substring(0, fileName.length - inputSuffix.length);
}

/**
 * Gets the name to create an input file with from what was typed, which may include the file name's ending.
 */
export function normalizeInputName(typed: string): string {
	const name = typed.trim();

	return getInputName(name) ?? name;
}

const invalidFileNameCharacters = /[<>:"/\\|?*\u0000-\u001f]/;
const reservedFileNames = /^(con|prn|aux|nul|com[0-9]|lpt[0-9])$/i;

/**
 * Checks a typed input name, returning why it can't be used, or undefined when it can.
 */
export function validateInputName(typed: string, existingNames: readonly string[]): string | undefined {
	const name = normalizeInputName(typed);

	if (!name) {
		return 'Enter a name for the input file, e.g. multi-item-order.';
	}

	if (invalidFileNameCharacters.test(name)) {
		return 'The name can\'t contain any of these characters: < > : " / \\ | ? *';
	}

	if (name.endsWith('.') || reservedFileNames.test(name)) {
		return `"${name}" can't be used as a file name.`;
	}

	// File names are compared without case, since they can't differ only by case on Windows or macOS.
	if (existingNames.some(x => x.toLowerCase() === name.toLowerCase())) {
		return `An input named "${name}" already exists.`;
	}

	return undefined;
}

/**
 * Gets the names of the methods that a transformer calls, in the order that they're first called.
 */
export function getCalledMethodNames(transformerText: string): string[] {
	const names = new Set<string>();

	for (const match of transformerText.matchAll(/#([A-Za-z_][A-Za-z0-9_]*)\s*\(/g)) {
		names.add(match[1]);
	}

	return [...names];
}

/**
 * Creates the text of a new input file, with a mock for each of the given custom methods to fill in, along with the
 * offset of the empty "input" object's contents, where the source document goes.
 */
export function createInputTemplate(mockedMethodNames: readonly string[], eol = '\n'): { text: string; inputOffset: number } {
	const indent = '    ';
	const before = `{${eol}${indent}"input": {`;
	const mocks = mockedMethodNames.length === 0
		? '{}'
		: `{${eol}${mockedMethodNames.map(x => `${indent}${indent}${JSON.stringify(x)}: { "returns": null }`).join(`,${eol}`)}${eol}${indent}}`;

	return {
		text: `${before}},${eol}${indent}"mocks": ${mocks}${eol}}${eol}`,
		inputOffset: before.length
	};
}

/** A code lens shown at the top of a preview document, which runs one of the preview's input commands. */
export interface PreviewLens {
	readonly title: string;
	readonly tooltip: string;
	readonly action: 'choose' | 'open' | 'create';
}

/**
 * Gets the code lenses for a preview that's running against the given input file, out of the given number of input
 * files that the transformer has: switching to another of them (or opening the only one), and creating a new one.
 */
export function getPreviewLenses(inputName: string | undefined, inputCount: number): PreviewLens[] {
	if (inputName === undefined) {
		return [];
	}

	const input: PreviewLens = inputCount > 1
		? { title: `$(list-selection) Input: ${inputName} $(chevron-down)`, tooltip: `Choose another of this transformer's ${inputCount} input files`, action: 'choose' }
		: { title: `$(go-to-file) Input: ${inputName}`, tooltip: 'Open the input file', action: 'open' };

	return [input, { title: '$(add) New Input File', tooltip: 'Create another input file for this transformer and preview it', action: 'create' }];
}

/** What the preview document shows, besides a transform result. */
export type PreviewState =
	| { kind: 'noInput' }
	| { kind: 'validationDisabled' }
	| { kind: 'serverStarting' }
	| { kind: 'serverFailed'; message: string }
	| { kind: 'running' }
	| { kind: 'result'; result: TransformResult };

export interface PreviewContent {
	/** The input file shown in the header, e.g. ".jolt/orders/multi-item-order.input.json". */
	inputPath?: string;
	state: PreviewState;
	/** The output of the last successful run, shown when the latest run has no output of its own. */
	lastOutput?: string;
	/** Whether the preview shows code lenses, which get a blank line below them to stand out. */
	hasLenses?: boolean;
}

/**
 * The line that a preview's code lenses are shown above. It's left blank, so that the lenses have space below them.
 */
export const previewLensLine = 0;

/**
 * Renders the preview document: comments describing the run (or why it didn't happen), followed by the output.
 */
export function renderPreview(content: PreviewContent, eol = '\n'): string {
	const comments: string[] = [];
	const state = content.state;
	let output: string | undefined;

	if (content.inputPath) {
		comments.push(`Input: ${content.inputPath}`);
	}

	switch (state.kind) {
		case 'noInput':
			comments.push(
				'No input file is chosen for this transformer.',
				'Use "Jolt: Choose Preview Input" (the list button above) to choose one or create one.');
			break;
		case 'validationDisabled':
			comments.push(
				'The preview runs transformers with the Jolt language server, which only runs while validation is turned on.',
				'Turn on the "jolt.validation.enabled" setting to see the output.');
			break;
		case 'serverStarting':
			comments.push('Starting the Jolt language server...');
			break;
		case 'serverFailed':
			comments.push(`The Jolt language server isn't running: ${state.message}`);
			break;
		case 'running':
			comments.push('Running the transformer...');
			break;
		case 'result':
			output = describeResult(state.result, comments);
			break;
	}

	if (output === undefined && content.lastOutput !== undefined) {
		comments.push('Showing the output from the last successful run.');
		output = content.lastOutput;
	}

	const lensSpace = content.hasLenses ? new Array<string>(previewLensLine + 1).fill('') : [];
	const header = comments.flatMap(x => x.split(/\r?\n/)).map(x => `// ${x}`.trimEnd());

	return [...lensSpace, ...header, ...(output === undefined ? [] : [output])].join(eol) + eol;
}

function describeResult(result: TransformResult, comments: string[]): string | undefined {
	switch (result.outcome) {
		case 'success':
			comments.push(`Ran in ${formatElapsed(result.elapsedMilliseconds)}.`);
			break;
		case 'skipped': {
			const count = result.transformerErrorCount ?? 0;

			comments.push(`Not run, since the transformer has ${count === 1 ? '1 error' : `${count} errors`}. See the Problems panel.`);
			break;
		}
		case 'invalidInput':
		case 'timedOut':
		case 'inputRequired':
			comments.push(result.error?.message ?? 'The transformer couldn\'t be run.');
			break;
		case 'error':
			comments.push(`Error: ${formatError(result.error)}`);

			for (const inner of result.error?.innerErrors ?? []) {
				comments.push(`  Caused by: ${formatError(inner)}`);
			}
			break;
	}

	for (const warning of result.warnings ?? []) {
		comments.push(`Warning: ${warning}`);
	}

	return result.outcome === 'success' ? result.output ?? 'null' : undefined;
}

function formatError(error: TransformError | undefined): string {
	if (!error) {
		return 'The transformer couldn\'t be run.';
	}

	return error.code ? `${error.message} (${error.code})` : error.message;
}

function formatElapsed(milliseconds: number | undefined): string {
	if (milliseconds === undefined) {
		return 'an unknown time';
	}

	return milliseconds < 1 ? 'under 1 ms' : `${Math.round(milliseconds)} ms`;
}
