import { Node } from 'jsonc-parser';
import { getStringLocation, isMatchCase, isPropertyName } from './context';

export interface ScopeVariable {
	/** The variable name, without the leading "@". */
	name: string;
	/** A short description of where the variable comes from. */
	description: string;
}

export interface VariableCompletions {
	/** The variables in scope, innermost first, with shadowed outer variables removed. */
	variables: ScopeVariable[];
	/** The number of characters before the cursor that a completion replaces (the "@" and any partial name). */
	replaceLength: number;
}

const variableBeingWritten = /@(\w*)$/;
const variableReference = /@(\w+)/g;
const lambdaParameters = /(@\w+(?:\s*;\s*@\w+)*)\s*:/y;

const declarationKey = /^\s*@(\w+)\s*$/;
const intoVariable = /\binto\s+@(\w+)/;
const keyMethod = /#(\w+)/;
const foreachBinding = /#foreach\s*\(\s*@(\w+)(?:\s*;\s*@?(\w+))?\s+in\b/;
const aliasBinding = /#(using|match)\s*\(.*?\bas\s+@(\w+)/;
const patternStart = /#is\s*\(/;

// Positions where a new variable is being named, so offering existing variables would not make sense.
const declarationKeyPosition = /^\s*@\w*$/;
const declarationPositions = [/\binto\s+@\w*$/, /\bas\s+@\w*$/, /#foreach\s*\(\s*@\w*(?:\s*;\s*@?\w*)?$/];

/**
 * Gets the variables that can be referenced at the given offset of a transformer document, or undefined when the
 * cursor is not at a position where a variable reference is being written (i.e. directly after "@" and a partial name).
 */
export function getVariableCompletions(text: string, offset: number): VariableCompletions | undefined {
	const location = getStringLocation(text, offset);
	const reference = location && variableBeingWritten.exec(location.textBeforeCursor);

	if (!location || !reference) {
		return undefined;
	}

	const { node, textBeforeCursor } = location;
	const isDeclaration = (isPropertyName(node) && declarationKeyPosition.test(textBeforeCursor))
		|| declarationPositions.some(x => x.test(textBeforeCursor));

	return {
		variables: isDeclaration ? [] : getVariablesInScope(node, textBeforeCursor),
		replaceLength: reference[0].length
	};
}

function getVariablesInScope(node: Node, textBeforeCursor: string): ScopeVariable[] {
	const variables: ScopeVariable[] = [];
	const add = (name: string, description: string) => variables.push({ name, description });

	for (const name of getLambdaParameters(textBeforeCursor)) {
		add(name, 'lambda parameter');
	}

	// Captures within a match case's own #is() pattern are usable later in that same key (e.g. in a following #given).
	if (node.parent && isPropertyName(node) && isMatchCase(node.parent)) {
		addPatternCaptures(textBeforeCursor, add);
	}

	addEnclosingScopes(node, add);
	add('params', 'transformer parameters (from #transform)');

	// Inner variables shadow outer variables of the same name.
	return variables.filter((x, i) => variables.findIndex(y => y.name === x.name) === i);
}

/**
 * Walks from the cursor's string up to the root of the document, adding variables from each enclosing scope.
 */
function addEnclosingScopes(node: Node, add: (name: string, description: string) => void): void {
	let child = node;

	for (let parent = node.parent; parent; child = parent, parent = parent.parent) {
		if (parent.type === 'property' && parent.children?.[1] === child) {
			// The cursor is within this property's value, so the bindings made by its key are in scope.
			const key = String(parent.children[0].value);

			addKeyBindings(key, add);

			if (isMatchCase(parent)) {
				addPatternCaptures(key, add);
			}
		} else if (parent.type === 'object') {
			// Earlier properties of an enclosing object have already been evaluated, so their declarations are in
			// scope. Later properties are not. The nearest declaration is added first so that it shadows the others.
			const index = parent.children?.indexOf(child) ?? -1;

			for (let i = index - 1; i >= 0; i--) {
				addDeclarations(String(parent.children![i].children?.[0]?.value ?? ''), add);
			}
		}
	}
}

function addKeyBindings(key: string, add: (name: string, description: string) => void): void {
	const foreach = foreachBinding.exec(key);

	if (foreach) {
		add(foreach[1], 'loop item (#foreach)');

		if (foreach[2]) {
			add(foreach[2], 'loop index (#foreach)');
		}
	}

	const alias = aliasBinding.exec(key);

	if (alias) {
		add(alias[2], `#${alias[1]} variable`);
	}
}

function addDeclarations(key: string, add: (name: string, description: string) => void): void {
	const declaration = declarationKey.exec(key);

	if (declaration) {
		add(declaration[1], 'variable');
	}

	const into = intoVariable.exec(key);

	if (into) {
		add(into[1], `result of #${keyMethod.exec(key)?.[1] ?? 'method'}`);
	}
}

function addPatternCaptures(key: string, add: (name: string, description: string) => void): void {
	const start = key.search(patternStart);

	if (start < 0) {
		return;
	}

	for (const match of key.slice(start).matchAll(variableReference)) {
		add(match[1], 'pattern capture (#is)');
	}
}

/**
 * Gets the parameters of the lambdas whose bodies contain the end of the expression, innermost first. A lambda body
 * runs from its "@x:" (or "@x;@y:") parameter list until the next comma or closing bracket at the same nesting level.
 */
function getLambdaParameters(expression: string): string[] {
	const frames: string[][] = [[]];
	let isInLiteral = false;

	for (let i = 0; i < expression.length; i++) {
		const current = expression[i];

		if (isInLiteral) {
			if (current === '\\') {
				i++;
			} else if (current === '\'') {
				isInLiteral = false;
			}

			continue;
		}

		switch (current) {
			case '\'':
				isInLiteral = true;
				break;
			case '(':
			case '[':
			case '{':
				frames.push([]);
				break;
			case ')':
			case ']':
			case '}':
				if (frames.length > 1) {
					frames.pop();
				}
				break;
			case ',':
				frames[frames.length - 1] = [];
				break;
			case '@': {
				lambdaParameters.lastIndex = i;
				const parameters = lambdaParameters.exec(expression);

				if (parameters) {
					frames[frames.length - 1].push(...parameters[1].split(';').map(x => x.trim().substring(1)));
					i = lambdaParameters.lastIndex - 1;
				}
				break;
			}
		}
	}

	return frames.flat().reverse();
}
