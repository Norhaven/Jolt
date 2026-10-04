import { ExpressionContext } from './context';
import { LibraryMethod, LibraryParameter, MethodTarget } from './library';

export interface MethodCompletion {
	method: LibraryMethod;
	/** The text to insert, in VS Code snippet syntax. */
	snippet: string;
	/** The number of characters before the cursor that the snippet replaces (the partially typed method name). */
	replaceLength: number;
	/** Whether the method is being called with the pipe operator, which supplies its first parameter. */
	isPiped: boolean;
}

export interface MethodCompletionOptions {
	includeUnsafeMethods: boolean;
}

const methodAfterHash = /(->\s*)?#(\w*)$/;
const afterPipe = /->\s*$/;

const targetDisplayNames: Record<MethodTarget, string> = {
	propertyName: 'Property name',
	propertyValue: 'Property value',
	statementBlock: 'Statement',
	matchBlock: 'Match case'
};

/**
 * Gets the library method completions for the given expression context, or undefined when the cursor is not at a
 * position where a method name is being written (i.e. after "#" or after the "->" pipe operator).
 */
export function getMethodCompletions(
	context: ExpressionContext,
	methods: readonly LibraryMethod[],
	options: MethodCompletionOptions): MethodCompletion[] | undefined {

	const afterHash = methodAfterHash.exec(context.textBeforeCursor);
	const isAfterPipeOnly = !afterHash && afterPipe.test(context.textBeforeCursor);

	if (!afterHash && !isAfterPipeOnly) {
		return undefined;
	}

	const isPiped = isAfterPipeOnly || afterHash?.[1] !== undefined;

	return methods
		.filter(x => options.includeUnsafeMethods || !x.isUnsafe)
		.filter(x => isValidAt(x, context.target, isPiped))
		.map(x => ({
			method: x,
			snippet: buildSnippet(x, context.target, isPiped, isAfterPipeOnly),
			replaceLength: afterHash?.[2].length ?? 0,
			isPiped
		}));
}

/**
 * Formats a method's signature for display, e.g. "orderBy(value, lambda?)".
 */
export function formatSignature(method: LibraryMethod, isPiped = false): string {
	const parameters = method.parameters
		.slice(isPiped ? 1 : 0)
		.map(x => `${x.isVariadic ? '...' : ''}${x.name}${x.isOptional ? '?' : ''}`);

	return `${method.name}(${parameters.join(', ')})`;
}

/**
 * Formats a method's documentation as Markdown.
 */
export function formatDocumentation(method: LibraryMethod): string {
	const sections = ['```\n#' + formatSignature(method) + '\n```'];

	if (method.description) {
		sections.push(method.description);
	}

	if (method.example) {
		sections.push(`*Example:* \`${method.example}\``);
	}

	if (method.returnType) {
		sections.push(`*Returns:* \`${method.returnType}\``);
	}

	sections.push(`*Valid on:* ${method.validOn.map(x => targetDisplayNames[x]).join(', ')}`);

	if (method.isUnsafe) {
		sections.push('**Unsafe:** only available when the host application explicitly enables unsafe methods.');
	}

	if (method.source) {
		sections.push(`*Defined in:* \`${method.source}\``);
	}

	return sections.join('\n\n');
}

function isValidAt(method: LibraryMethod, target: MethodTarget, isPiped: boolean): boolean {
	if (isPiped && !canBePiped(method)) {
		return false;
	}

	// Match arms (#is, #given, #default) are also marked as valid on property names, but only within a match case list.
	if (target === 'propertyName' && method.validOn.includes('matchBlock')) {
		return false;
	}

	return method.validOn.includes(target);
}

function canBePiped(method: LibraryMethod): boolean {
	const first = method.parameters[0];

	return first !== undefined && (first.kind === 'value' || first.kind === 'string' || first.kind === 'object');
}

function buildSnippet(method: LibraryMethod, target: MethodTarget, isPiped: boolean, includeHash: boolean): string {
	let tabStop = 1;

	const placeholder = (text: string) => `\${${tabStop++}:${escapeSnippetText(text)}}`;

	const args = method.parameters
		.slice(isPiped ? 1 : 0)
		.filter(x => !x.isOptional)
		.map(x => formatArgument(x, placeholder))
		.join(', ');

	let snippet = `${includeHash ? '#' : ''}${method.name}(${args})`;

	if (method.isValueGenerator && target === 'propertyName') {
		snippet += ` into '${placeholder('name')}'`;
	}

	return snippet + '$0';
}

function formatArgument(parameter: LibraryParameter, placeholder: (text: string) => string): string {
	switch (parameter.kind) {
		case 'lambda':
			return `${placeholder('@x')}: ${placeholder('expression')}`;
		case 'enumeration':
			return `${placeholder('@x')} in ${placeholder('$.path')}`;
		case 'variableAlias':
			return `${placeholder('$.path')} as ${placeholder('@x')}`;
		case 'rangeVariable':
			return placeholder('@x');
		case 'variablePath':
			return placeholder('@x.path');
		case 'range':
			return `${placeholder('start')}..${placeholder('end')}`;
		default:
			return placeholder(parameter.name);
	}
}

function escapeSnippetText(text: string): string {
	return text.replace(/[\\$}]/g, '\\$&');
}
