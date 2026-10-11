import { Node, findNodeAtOffset, parseTree } from 'jsonc-parser';
import { MethodTarget } from './library';

export interface ExpressionContext {
	/** Where the expression sits in the transformer, which determines the library methods that are valid there. */
	target: MethodTarget;
	/** The raw text of the enclosing JSON string, from just after its opening quote up to the cursor. */
	textBeforeCursor: string;
}

export interface StringLocation {
	/** The JSON string node (a property name or a string value) that contains the cursor. */
	node: Node;
	/** The raw text of the string, from just after its opening quote up to the cursor. */
	textBeforeCursor: string;
}

const unescapedQuote = /(^|[^\\])(\\\\)*"/;

/**
 * Finds the JSON string that contains the given offset of a transformer document, or undefined when the offset is
 * not inside a JSON string (Jolt expressions only ever appear within property names and string values).
 */
export function getStringLocation(text: string, offset: number): StringLocation | undefined {
	const root = parseTree(text);

	if (!root) {
		return undefined;
	}

	const node = findNodeAtOffset(root, offset, true);

	if (!node || node.type !== 'string' || offset <= node.offset) {
		return undefined;
	}

	const textBeforeCursor = text.slice(node.offset + 1, offset);

	// Guards against the cursor sitting just after the closing quote, which is still within the node's bounds.
	if (unescapedQuote.test(textBeforeCursor)) {
		return undefined;
	}

	return { node, textBeforeCursor };
}

/**
 * Determines the Jolt expression context at the given offset of a transformer document, or undefined when the
 * offset is not inside a JSON string.
 */
export function getExpressionContext(text: string, offset: number): ExpressionContext | undefined {
	const location = getStringLocation(text, offset);

	if (!location) {
		return undefined;
	}

	return { target: getTarget(location.node, location.textBeforeCursor), textBeforeCursor: location.textBeforeCursor };
}

/** The offset of a JSON string's text, just after its opening quote. */
export function getStringContentStart(node: Node): number {
	return node.offset + 1;
}

/** The raw text of a JSON string, without its quotes (an unterminated string has no closing quote to remove). */
export function getStringContent(text: string, node: Node): string {
	const end = node.offset + node.length;
	const isTerminated = node.length >= 2 && text[end - 1] === '"';

	return text.slice(getStringContentStart(node), isTerminated ? end - 1 : end);
}

const jsonEscapes: Record<string, string> = { '"': '"', '\\': '\\', '/': '/', b: '\b', f: '\f', n: '\n', r: '\r', t: '\t' };

/**
 * Calls back with the index of each character of an expression that is outside of its string literals ('...').
 * The callback may return the index of the last character it has read (e.g. the end of a token) to continue after it.
 *
 * The expression is the raw text of a JSON string, so JSON escape sequences (e.g. \\ or \") are read as the single
 * character that Jolt sees, which matters within literals: 'it\\'s' in the JSON is the literal 'it\'s' to Jolt.
 * Characters written as escape sequences are not passed to the callback, as Jolt's syntax never needs them.
 */
export function forEachOutsideLiterals(expression: string, callback: (index: number) => number | void): void {
	let isInLiteral = false;
	let isLiteralEscape = false;

	for (let i = 0; i < expression.length;) {
		const length = expression[i] !== '\\' ? 1 : expression[i + 1] === 'u' ? 6 : 2;
		const current = length === 1 ? expression[i] : decodeJsonEscape(expression.substr(i, length));

		if (isInLiteral) {
			if (isLiteralEscape) {
				isLiteralEscape = false;
			} else if (current === '\\') {
				isLiteralEscape = true;
			} else if (current === '\'') {
				isInLiteral = false;
			}
		} else if (current === '\'') {
			isInLiteral = true;
		} else if (length === 1) {
			const last = callback(i);

			if (last !== undefined) {
				i = last + 1;
				continue;
			}
		}

		i += length;
	}
}

function decodeJsonEscape(sequence: string): string {
	if (sequence[1] === 'u') {
		const code = parseInt(sequence.substring(2), 16);

		return Number.isNaN(code) ? '�' : String.fromCharCode(code);
	}

	// An invalid escape is reported by validation, so it is read leniently as the character after the backslash.
	return jsonEscapes[sequence[1]] ?? sequence[1] ?? '';
}

/**
 * Whether the given string node is a property name rather than a value.
 */
export function isPropertyName(node: Node): boolean {
	return node.parent?.type === 'property' && node.parent.children?.[0] === node;
}

/**
 * Whether the given property is a case within a #match case list (i.e. a single-property object in its array).
 */
export function isMatchCase(property: Node): boolean {
	const caseList = property.parent?.parent;

	return caseList?.type === 'array' && isValueOfKey(caseList, /^#match\s*\(/);
}

function getTarget(node: Node, textBeforeCursor: string): MethodTarget {
	// Arguments to a method call are always value expressions, regardless of where the call itself sits.
	if (isWithinCallArguments(textBeforeCursor)) {
		return 'propertyValue';
	}

	const parent = node.parent;

	if (parent && isPropertyName(node)) {
		// The keys of the single-property objects in a #match case list are match arms (#is, #given, #default).
		return isMatchCase(parent) ? 'matchBlock' : 'propertyName';
	}

	if (parent?.type === 'array' && isValueOfKey(parent, /^#using\s*\(/)) {
		return 'statementBlock';
	}

	return 'propertyValue';
}

function isValueOfKey(node: Node, keyPattern: RegExp): boolean {
	const property = node.parent;

	if (property?.type !== 'property' || property.children?.[1] !== node) {
		return false;
	}

	return keyPattern.test(String(property.children[0].value).trim());
}

function isWithinCallArguments(expression: string): boolean {
	let depth = 0;

	forEachOutsideLiterals(expression, i => {
		if (expression[i] === '(') {
			depth++;
		} else if (expression[i] === ')' && depth > 0) {
			depth--;
		}
	});

	return depth > 0;
}
