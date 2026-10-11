import { getLocation } from 'jsonc-parser';
import { getStringLocation, isPropertyName, StringLocation } from './context';

export interface StatementSnippet {
	/** The label shown in completions, which is also what is matched against what has been typed. */
	label: string;
	/** A short description of what the statement does. */
	description: string;
	/** The property to insert, in VS Code snippet syntax, indented with tabs. */
	body: string;
}

export interface PropertyPosition {
	/**
	 * The number of characters before the cursor that a snippet replaces: a partially typed label (e.g. #fore), and
	 * the opening quote when it is being typed as a property name (e.g. "#fore).
	 */
	replaceLength: number;
	/** The number of characters after the cursor that a snippet replaces (e.g. the closing quote of a property name). */
	replaceAfterLength: number;
	/** The text before the label within the replaced text (e.g. the opening quote), which completions are filtered by. */
	labelPrefix: string;
	/** Where a comma is needed (just after the previous property's value) to separate it from the snippet, if anywhere. */
	leadingCommaOffset: number | undefined;
	/** Whether a comma is needed after the snippet, to separate it from the property after it. */
	needsTrailingComma: boolean;
}

/**
 * Snippets for the statements whose property values have a particular shape, which completing just the property name
 * (within its string) can't provide.
 */
export const statementSnippets: readonly StatementSnippet[] = [
	{
		label: '#foreach',
		description: 'Transform each item of an array',
		body: '"#foreach(${1:@x} in ${2:\\$.path}) into \'${3:name}\'": [\n\t{\n\t\t"${4:property}": "${5:@x}"\n\t}\n]'
	},
	{
		label: '#using',
		description: 'Change a value with a series of statements',
		body: '"#using(${1:\\$.path} as ${2:@x}) into \'${3:name}\'": [\n\t"${4:#setAt(@x.path, 1)}"\n]'
	},
	{
		label: '#match',
		description: 'Produce a value depending on what another value matches',
		body: '"#match(${1:\\$.path} as ${2:@x}) into \'${3:name}\'": [\n\t{ "#is(${4:object})": "${5:@x}" },\n\t{ "#default()": "${6:null}" }\n]'
	},
	{
		label: '@variable',
		description: 'Declare a variable for the properties after this one',
		body: '"@${1:name}": "#valueOf(${2:\\$.path})"'
	}
];

const partialLabel = /[#@]?\w*$/;
const wholePartialLabel = /^[#@]\w*$/;
const valueEnd = /["}\]\w]/;

/**
 * Determines whether a property can be inserted at the given offset of a transformer document: within an object where
 * a property name would go (including just after a property that has no comma yet). That is either outside of any
 * string, or within a property name that has only a partial label in it (e.g. "#fore", as typing a property name
 * usually starts with its quote) and no value yet, in which case the property name is replaced.
 */
export function getPropertyPosition(text: string, offset: number): PropertyPosition | undefined {
	const location = getStringLocation(text, offset);

	return location ? getPropertyNamePosition(text, offset, location) : getUnquotedPosition(text, offset);
}

function getUnquotedPosition(text: string, offset: number): PropertyPosition | undefined {
	const replaceLength = partialLabel.exec(text.slice(0, offset))![0].length;
	const start = offset - replaceLength;
	const previous = findSignificant(text, start - 1, -1);

	let leadingCommaOffset: number | undefined;

	if (!getLocation(text, start).isAtPropertyKey) {
		// After a value without a comma, which is where a new property usually starts being written.
		const withComma = text.slice(0, start) + ',' + text.slice(start);

		if (previous < 0 || !valueEnd.test(text[previous]) || !getLocation(withComma, start + 1).isAtPropertyKey) {
			return undefined;
		}

		leadingCommaOffset = previous + 1;
	}

	return {
		replaceLength,
		replaceAfterLength: 0,
		labelPrefix: '',
		leadingCommaOffset,
		needsTrailingComma: text[findSignificant(text, offset, 1)] === '"'
	};
}

function getPropertyNamePosition(text: string, offset: number, location: StringLocation): PropertyPosition | undefined {
	const { node, textBeforeCursor } = location;
	const property = node.parent;
	const contentEnd = node.offset + node.length - 1;

	// Only a property name with nothing but a partial label in it, and without a value, is replaced by a whole property.
	if (!property || !isPropertyName(node) || property.children!.length > 1 || !wholePartialLabel.test(textBeforeCursor)
		|| offset !== contentEnd || text[contentEnd] !== '"') {
		return undefined;
	}

	// A colon may have been typed after the name already, which is replaced along with it.
	const end = property.colonOffset !== undefined ? property.colonOffset + 1 : contentEnd + 1;
	const previous = findSignificant(text, node.offset - 1, -1);

	return {
		replaceLength: offset - node.offset,
		replaceAfterLength: end - offset,
		labelPrefix: '"',
		// The property before this one may not have a comma after it yet, unless this is the first property.
		leadingCommaOffset: previous >= 0 && valueEnd.test(text[previous]) ? previous + 1 : undefined,
		needsTrailingComma: text[findSignificant(text, end, 1)] === '"'
	};
}

/**
 * Finds the index of the nearest character that isn't whitespace, from the given index in the given direction, or -1.
 */
function findSignificant(text: string, index: number, direction: 1 | -1): number {
	for (let i = index; i >= 0 && i < text.length; i += direction) {
		if (!/\s/.test(text[i])) {
			return i;
		}
	}

	return -1;
}
