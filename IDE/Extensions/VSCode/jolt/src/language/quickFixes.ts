import { Edit, Node, ParseError, applyEdits, findNodeAtOffset, modify, parseTree } from 'jsonc-parser';
import { forEachOutsideLiterals, isMatchCase } from './context';

export interface MethodsFileParameter {
	name: string;
	kind: 'value' | 'lambda';
	lambdaVariables?: string[];
}

/** An entry in a .jolt/methods.json file's "methods" array. */
export interface MethodsFileEntry {
	name: string;
	description: string;
	parameters: MethodsFileParameter[];
}

export interface MethodsFileChange {
	/** The edits that add the entry, or a single insertion of the whole file when it doesn't exist yet. */
	edits: Edit[];
	/** The file's text after the edits. */
	newText: string;
	/** The offset of the new entry's name within the new text, for revealing it. */
	nameOffset: number;
}

export interface Insertion {
	offset: number;
	text: string;
}

const lambdaArgument = /^@([A-Za-z_]\w*)(?:\s*;\s*@([A-Za-z_]\w*))?\s*:/;
const defaultIndentation = 4;

/**
 * Gets the candidates that are close to the given name (e.g. misspellings, or differences in case), closest first.
 */
export function getSimilarNames(name: string, candidates: readonly string[], maxResults = 3): string[] {
	const lowerName = name.toLowerCase();
	const maxDistance = Math.max(1, Math.floor(name.length / 4));

	return [...new Set(candidates)]
		.filter(x => x !== name)
		.map(x => ({ name: x, distance: getEditDistance(lowerName, x.toLowerCase()) }))
		.filter(x => x.distance <= maxDistance)
		.sort((a, b) => a.distance - b.distance || a.name.localeCompare(b.name))
		.slice(0, maxResults)
		.map(x => x.name);
}

/**
 * Infers a method's parameters from the arguments of a call to it, given the text just after the method's name (e.g.
 * "($.a, @x: @x.b) + 1"). Lambda arguments (e.g. "@x: ...") become lambda parameters that bind the same variables.
 */
export function inferParameters(textAfterName: string): MethodsFileParameter[] {
	const open = textAfterName.search(/\S/);

	if (open < 0 || textAfterName[open] !== '(') {
		return [];
	}

	const argumentsText = textAfterName.slice(open + 1);
	const separators: number[] = [];
	let depth = 0;
	let end = argumentsText.length;

	forEachOutsideLiterals(argumentsText, i => {
		const current = argumentsText[i];

		if (current === '(' || current === '[' || current === '{') {
			depth++;
		} else if ((current === ')' || current === ']' || current === '}') && depth > 0) {
			depth--;
		} else if (current === ')') {
			end = i;
			return argumentsText.length;
		} else if (current === ',' && depth === 0) {
			separators.push(i);
		}
	});

	const bounds = [-1, ...separators, end];
	const args = bounds.slice(1).map((x, i) => argumentsText.slice(bounds[i] + 1, x).trim());

	if (args.length === 1 && args[0] === '') {
		return [];
	}

	return args.map((x, i) => {
		const lambda = lambdaArgument.exec(x);

		return lambda
			? { name: `arg${i + 1}`, kind: 'lambda', lambdaVariables: lambda.slice(1).filter(y => y !== undefined) }
			: { name: `arg${i + 1}`, kind: 'value' };
	});
}

/**
 * Adds an entry to the "methods" array of a methods file, keeping the file's indentation and line endings, or creates
 * the file's text when it doesn't exist. Returns undefined when the file isn't valid JSON, as it can't be safely changed.
 */
export function addToMethodsFile(text: string | undefined, entry: MethodsFileEntry): MethodsFileChange | undefined {
	if (text === undefined) {
		const newText = JSON.stringify({ methods: [entry] }, null, defaultIndentation) + '\n';

		return { edits: [{ offset: 0, length: 0, content: newText }], newText, nameOffset: findNameOffset(newText, entry.name) };
	}

	const errors: ParseError[] = [];
	const root = parseTree(text, errors, { allowTrailingComma: true });

	if (errors.length > 0 || root?.type !== 'object') {
		return undefined;
	}

	const edits = modify(text, ['methods', -1], entry, { formattingOptions: detectFormatting(text) });
	const newText = applyEdits(text, edits);

	return { edits, newText, nameOffset: findNameOffset(newText, entry.name) };
}

/**
 * Gets where to declare a variable (e.g. "@x": "") so that it is in scope at the given reference: just before the
 * property that contains the reference, which makes it in scope for the rest of that property and those after it.
 */
export function getDeclarationInsertion(text: string, referenceOffset: number, name: string): Insertion | undefined {
	const root = parseTree(text, [], { allowTrailingComma: true });
	let property: Node | undefined = root && findNodeAtOffset(root, referenceOffset, true);

	// A #match case must remain a single-property object, so the declaration goes before the #match property instead.
	while (property && (property.type !== 'property' || isMatchCase(property))) {
		property = property.parent;
	}

	if (!property) {
		return undefined;
	}

	const lineStart = text.lastIndexOf('\n', property.offset - 1) + 1;
	const indentation = text.slice(lineStart, property.offset);
	const declaration = `"@${name}": ""`;

	// A property on its own line gets a declaration on its own line, and one sharing a line (e.g. "{ "a": 1 }") shares it.
	return /^\s*$/.test(indentation)
		? { offset: property.offset, text: `${declaration},${text.includes('\r\n') ? '\r\n' : '\n'}${indentation}` }
		: { offset: property.offset, text: `${declaration}, ` };
}

function detectFormatting(text: string): { insertSpaces: boolean; tabSize: number; eol: string } {
	const indentation = /\n([ \t]+)\S/.exec(text)?.[1];

	return {
		insertSpaces: !indentation?.startsWith('\t'),
		tabSize: indentation && !indentation.startsWith('\t') ? indentation.length : defaultIndentation,
		eol: text.includes('\r\n') ? '\r\n' : '\n'
	};
}

function findNameOffset(text: string, name: string): number {
	const quoted = JSON.stringify(name);
	const index = text.lastIndexOf(quoted);

	return index < 0 ? 0 : index + 1;
}

/**
 * The edit distance between two strings: the fewest single character insertions, deletions, substitutions, or swaps
 * of adjacent characters (a common typo, e.g. "valeuOf") that turn one into the other.
 */
function getEditDistance(a: string, b: string): number {
	const distances = Array.from({ length: a.length + 1 }, (_, i) => Array.from({ length: b.length + 1 }, (_, j) => i === 0 ? j : j === 0 ? i : 0));

	for (let i = 1; i <= a.length; i++) {
		for (let j = 1; j <= b.length; j++) {
			distances[i][j] = Math.min(
				distances[i - 1][j] + 1,
				distances[i][j - 1] + 1,
				distances[i - 1][j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1));

			if (i > 1 && j > 1 && a[i - 1] === b[j - 2] && a[i - 2] === b[j - 1]) {
				distances[i][j] = Math.min(distances[i][j], distances[i - 2][j - 2] + 1);
			}
		}
	}

	return distances[a.length][b.length];
}
