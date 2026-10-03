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
	let isInLiteral = false;

	for (let i = 0; i < expression.length; i++) {
		const current = expression[i];

		if (isInLiteral) {
			if (current === '\\') {
				i++;
			} else if (current === '\'') {
				isInLiteral = false;
			}
		} else if (current === '\'') {
			isInLiteral = true;
		} else if (current === '(') {
			depth++;
		} else if (current === ')' && depth > 0) {
			depth--;
		}
	}

	return depth > 0;
}
