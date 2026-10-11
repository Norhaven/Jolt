import { Node, findNodeAtOffset, parseTree } from 'jsonc-parser';
import { forEachOutsideLiterals, getStringContent, getStringContentStart, getStringLocation, isMatchCase, isPropertyName } from './context';

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

/** A span of a transformer document, from the offset of its start to the offset just past its end. */
export interface Span {
	start: number;
	end: number;
}

export interface VariableBinding extends ScopeVariable {
	/** Where the variable's name is declared (without its "@"), or undefined for the implicit @params variable. */
	declaration?: Span;
	/** The raw text of the JSON string that declares the variable, or undefined for the implicit @params variable. */
	declaredIn?: string;
}

export interface VariableReference {
	/** The variable name, without the leading "@". */
	name: string;
	/** Where the name is (without its "@"). */
	span: Span;
	/** The variable that the reference refers to, or undefined when no variable of that name is in scope. */
	binding: VariableBinding | undefined;
}

type AddBinding = (binding: VariableBinding) => void;

const variableBeingWritten = /@(\w*)$/;
const variableReference = /@(\w+)/g;
const variableToken = /@(\w+)/y;
const lambdaParameters = /(@\w+(?:\s*;\s*@\w+)*)\s*:/y;
const variableName = /^[A-Za-z_]\w*$/;
const maxDeclaredInLength = 120;

const declarationKey = /^\s*@(\w+)\s*$/d;
const intoVariable = /\binto\s+@(\w+)/d;
const keyMethod = /#(\w+)/;
const foreachBinding = /#foreach\s*\(\s*@(\w+)(?:\s*;\s*@(\w+))?\s+in\b/d;
const aliasBinding = /#(using|match)\s*\(.*?\bas\s+@(\w+)/d;
const patternStart = /#is\s*\(/;

const params: VariableBinding = { name: 'params', description: 'transformer parameters (from #transform)' };

// Positions where a new variable is being named, so offering existing variables would not make sense.
const declarationKeyPosition = /^\s*@\w*$/;
const declarationPositions = [/\binto\s+@\w*$/, /\bas\s+@\w*$/, /#foreach\s*\(\s*@\w*(?:\s*;\s*@\w*)?$/];

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
		variables: isDeclaration
			? []
			: getBindingsInScope(text, node, textBeforeCursor).map(x => ({ name: x.name, description: x.description })),
		replaceLength: reference[0].length
	};
}

/**
 * Gets the variables in scope at the given offset of a transformer document (e.g. the "@" of a reference), innermost
 * first, or an empty list when the offset is not inside a JSON string.
 */
export function getVariablesInScopeAt(text: string, offset: number): ScopeVariable[] {
	const location = getStringLocation(text, offset);

	return location
		? getBindingsInScope(text, location.node, location.textBeforeCursor).map(x => ({ name: x.name, description: x.description }))
		: [];
}

/**
 * Gets the variable reference (e.g. "@x") at the given offset of a transformer document, along with the variable it
 * refers to, or undefined when the offset is not on a variable. A variable's declaration is itself a reference to it.
 */
export function getVariableReferenceAt(text: string, offset: number): VariableReference | undefined {
	const root = parseTree(text, [], { allowTrailingComma: true });
	const node = root && findNodeAtOffset(root, offset, true);

	if (node?.type !== 'string') {
		return undefined;
	}

	// The "@" is part of the reference, so that hovering over it works too.
	const token = findVariableTokens(text, node).find(x => offset >= x.start - 1 && offset <= x.end);

	if (!token) {
		return undefined;
	}

	return { name: token.name, span: { start: token.start, end: token.end }, binding: resolveBinding(text, node, token) };
}

/**
 * Gets every reference to the given variable within a transformer document, including its declaration, as the spans
 * of their names (without the "@"). References to other variables with the same name are not included.
 */
export function getVariableOccurrences(text: string, binding: VariableBinding): Span[] {
	const declaration = binding.declaration;
	const root = parseTree(text, [], { allowTrailingComma: true });

	const occurrences: Span[] = [];

	if (!declaration || !root) {
		return occurrences;
	}

	const visit = (node: Node) => {
		if (node.type === 'string') {
			for (const token of findVariableTokens(text, node)) {
				if (token.name === binding.name && resolveBinding(text, node, token)?.declaration?.start === declaration.start) {
					occurrences.push({ start: token.start, end: token.end });
				}
			}
		}

		node.children?.forEach(visit);
	};

	visit(root);

	return occurrences;
}

/**
 * Formats a variable's documentation as Markdown: what it is, and the property name or expression that declares it.
 */
export function formatVariableDocumentation(binding: VariableBinding): string {
	const sections = ['```\n@' + binding.name + '\n```', binding.description.charAt(0).toUpperCase() + binding.description.slice(1)];

	if (binding.declaredIn !== undefined) {
		const declaredIn = binding.declaredIn.length > maxDeclaredInLength
			? binding.declaredIn.substring(0, maxDeclaredInLength - 1) + '…'
			: binding.declaredIn;

		sections.push('*Declared in:*\n```\n' + declaredIn + '\n```');
	}

	return sections.join('\n\n');
}

/**
 * Whether the given name (without the "@") can be used for a variable.
 */
export function isValidVariableName(name: string): boolean {
	return variableName.test(name);
}

/**
 * Finds the variable that a reference in the given string refers to: the declaration that the reference itself is,
 * or otherwise the innermost variable of that name in scope just before it.
 */
function resolveBinding(text: string, node: Node, token: Span & { name: string }): VariableBinding | undefined {
	const declared = getDeclarationsIn(text, node).find(x => x.declaration?.start === token.start);

	if (declared) {
		return declared;
	}

	const textBeforeReference = text.slice(getStringContentStart(node), token.start - 1);

	return getBindingsInScope(text, node, textBeforeReference).find(x => x.name === token.name);
}

/**
 * Gets the variables declared within a string: the parameters of its lambdas, and when it is a property name, the
 * variables that the property declares or binds.
 */
function getDeclarationsIn(text: string, node: Node): VariableBinding[] {
	const content = getStringContent(text, node);
	const start = getStringContentStart(node);
	const declarations: VariableBinding[] = [];
	const add: AddBinding = x => declarations.push(x);

	scanExpression(content, start, content, { lambda: x => x.forEach(add) });

	if (node.parent && isPropertyName(node)) {
		addDeclarations(content, start, add);
		addKeyBindings(content, start, add);

		if (isMatchCase(node.parent)) {
			addPatternCaptures(content, start, content, add);
		}
	}

	return declarations;
}

function getBindingsInScope(text: string, node: Node, textBeforeCursor: string): VariableBinding[] {
	const bindings: VariableBinding[] = [];
	const add: AddBinding = x => bindings.push(x);
	const content = getStringContent(text, node);
	const start = getStringContentStart(node);

	getLambdaParameters(textBeforeCursor, start, content).forEach(add);

	// Captures within a match case's own #is() pattern are usable later in that same key (e.g. in a following #given).
	if (node.parent && isPropertyName(node) && isMatchCase(node.parent)) {
		addPatternCaptures(textBeforeCursor, start, content, add);
	}

	addEnclosingScopes(text, node, add);
	add(params);

	// Inner variables shadow outer variables of the same name.
	return bindings.filter((x, i) => bindings.findIndex(y => y.name === x.name) === i);
}

/**
 * Walks from the cursor's string up to the root of the document, adding variables from each enclosing scope.
 */
function addEnclosingScopes(text: string, node: Node, add: AddBinding): void {
	let child = node;

	for (let parent = node.parent; parent; child = parent, parent = parent.parent) {
		if (parent.type === 'property' && parent.children?.[1] === child) {
			// The cursor is within this property's value, so the bindings made by its key are in scope.
			const key = parent.children[0];
			const content = getStringContent(text, key);

			addKeyBindings(content, getStringContentStart(key), add);

			if (isMatchCase(parent)) {
				addPatternCaptures(content, getStringContentStart(key), content, add);
			}
		} else if (parent.type === 'object') {
			// Earlier properties of an enclosing object have already been evaluated, so their declarations are in
			// scope. Later properties are not. The nearest declaration is added first so that it shadows the others.
			const index = parent.children?.indexOf(child) ?? -1;

			for (let i = index - 1; i >= 0; i--) {
				const key = parent.children![i].children?.[0];

				if (key) {
					addDeclarations(getStringContent(text, key), getStringContentStart(key), add);
				}
			}
		}
	}
}

/**
 * Adds the variables bound by a property name for use within its value. The start is the offset of the key's text.
 */
function addKeyBindings(key: string, start: number, add: AddBinding): void {
	const foreach = foreachBinding.exec(key);

	if (foreach) {
		add(bind(foreach, 1, 'loop item (#foreach)', start, key));

		if (foreach[2]) {
			add(bind(foreach, 2, 'loop index (#foreach)', start, key));
		}
	}

	const alias = aliasBinding.exec(key);

	if (alias) {
		add(bind(alias, 2, `#${alias[1]} variable`, start, key));
	}
}

/**
 * Adds the variables that a property name declares for use by the properties after it.
 */
function addDeclarations(key: string, start: number, add: AddBinding): void {
	const declaration = declarationKey.exec(key);

	if (declaration) {
		add(bind(declaration, 1, 'variable', start, key));
	}

	const into = intoVariable.exec(key);

	if (into) {
		add(bind(into, 1, `result of #${keyMethod.exec(key)?.[1] ?? 'method'}`, start, key));
	}
}

/**
 * Adds the variables captured by a match case's #is() pattern within the given text, which starts at the given
 * offset of the source string. A capture is the first use of a name in the pattern, and later uses refer to it.
 */
function addPatternCaptures(text: string, start: number, source: string, add: AddBinding): void {
	const patternIndex = text.search(patternStart);
	const captured = new Set<string>();

	if (patternIndex < 0) {
		return;
	}

	for (const match of text.slice(patternIndex).matchAll(variableReference)) {
		if (!captured.has(match[1])) {
			captured.add(match[1]);
			add(declaredAt(match[1], 'pattern capture (#is)', start + patternIndex + match.index + 1, source));
		}
	}
}

/**
 * Gets the parameters of the lambdas whose bodies contain the end of the expression, innermost first. A lambda body
 * runs from its "@x:" (or "@x;@y:") parameter list until the next comma or closing bracket at the same nesting level.
 */
function getLambdaParameters(expression: string, start: number, source: string): VariableBinding[] {
	const frames: VariableBinding[][] = [[]];

	scanExpression(expression, start, source, {
		open: () => frames.push([]),
		close: () => {
			if (frames.length > 1) {
				frames.pop();
			}
		},
		comma: () => frames[frames.length - 1] = [],
		lambda: parameters => frames[frames.length - 1].push(...parameters)
	});

	return frames.flat().reverse();
}

/**
 * Scans an expression that starts at the given offset of a source string, outside of string literals, reporting
 * brackets, commas, and lambda parameter lists.
 */
function scanExpression(
	expression: string,
	start: number,
	source: string,
	on: { open?: () => void; close?: () => void; comma?: () => void; lambda: (parameters: VariableBinding[]) => void }): void {

	forEachOutsideLiterals(expression, i => {
		switch (expression[i]) {
			case '(':
			case '[':
			case '{':
				on.open?.();
				break;
			case ')':
			case ']':
			case '}':
				on.close?.();
				break;
			case ',':
				on.comma?.();
				break;
			case '@': {
				lambdaParameters.lastIndex = i;
				const parameters = lambdaParameters.exec(expression);

				if (parameters) {
					on.lambda([...parameters[1].matchAll(variableReference)]
						.map(x => declaredAt(x[1], 'lambda parameter', start + i + x.index + 1, source)));

					return lambdaParameters.lastIndex - 1;
				}
				break;
			}
		}
	});
}

/**
 * Finds the variable references in a string, outside of string literals, as the spans of their names.
 */
function findVariableTokens(text: string, node: Node): (Span & { name: string })[] {
	const content = getStringContent(text, node);
	const start = getStringContentStart(node);
	const tokens: (Span & { name: string })[] = [];

	forEachOutsideLiterals(content, i => {
		variableToken.lastIndex = i;
		const token = content[i] === '@' ? variableToken.exec(content) : null;

		if (token) {
			tokens.push({ name: token[1], start: start + i + 1, end: start + i + token[0].length });

			return i + token[0].length - 1;
		}
	});

	return tokens;
}

function bind(match: RegExpExecArray, group: number, description: string, start: number, source: string): VariableBinding {
	return declaredAt(match[group], description, start + match.indices![group]![0], source);
}

function declaredAt(name: string, description: string, offset: number, source: string): VariableBinding {
	return { name, description, declaration: { start: offset, end: offset + name.length }, declaredIn: source };
}
