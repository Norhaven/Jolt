import { Node, parseTree } from 'jsonc-parser';

/** What a symbol represents, which determines its icon. */
export type JoltSymbolKind = 'method' | 'variable' | 'object' | 'array' | 'string' | 'number' | 'boolean' | 'null';

export interface JoltSymbol {
	/** The property name, or the index (e.g. "[0]") of an array item. */
	name: string;
	/** A short summary of the value, such as a string value's text. */
	detail: string;
	kind: JoltSymbolKind;
	/** The offset and length of the whole property (or array item). */
	offset: number;
	length: number;
	/** The offset and length of the part to select when the symbol is chosen (the property name, or the item). */
	selectionOffset: number;
	selectionLength: number;
	children: JoltSymbol[];
}

const variableDeclaration = /^\s*@\w+\s*$/;
const methodCall = /^\s*#/;
const maxDetailLength = 60;

/**
 * Gets the outline of a transformer document: its properties and array items, nested as they are in the document.
 * Properties whose names call a method (e.g. "#foreach(...)") or declare a variable (e.g. "@x") are marked as such.
 */
export function getDocumentSymbols(text: string): JoltSymbol[] {
	const root = parseTree(text, [], { allowTrailingComma: true });

	return root ? getChildSymbols(root) : [];
}

function getChildSymbols(node: Node): JoltSymbol[] {
	if (node.type === 'object') {
		return (node.children ?? []).flatMap(property => {
			const [key, value] = property.children ?? [];

			if (!key) {
				return [];
			}

			const name = String(key.value);

			return [{
				name,
				detail: value ? getDetail(value) : '',
				kind: getPropertyKind(name, value),
				offset: property.offset,
				length: property.length,
				selectionOffset: key.offset,
				selectionLength: key.length,
				children: value ? getChildSymbols(value) : []
			}];
		});
	}

	if (node.type === 'array') {
		return (node.children ?? []).map((item, index) => ({
			name: `[${index}]`,
			detail: getDetail(item),
			kind: getValueKind(item),
			offset: item.offset,
			length: item.length,
			selectionOffset: item.offset,
			selectionLength: item.length,
			children: getChildSymbols(item)
		}));
	}

	return [];
}

function getPropertyKind(name: string, value: Node | undefined): JoltSymbolKind {
	if (variableDeclaration.test(name)) {
		return 'variable';
	}

	if (methodCall.test(name)) {
		return 'method';
	}

	return value ? getValueKind(value) : 'null';
}

function getValueKind(node: Node): JoltSymbolKind {
	return node.type === 'property' ? 'null' : node.type;
}

function getDetail(node: Node): string {
	switch (node.type) {
		case 'string':
			return truncate(String(node.value));
		case 'number':
		case 'boolean':
		case 'null':
			return String(node.value);
		default:
			return '';
	}
}

function truncate(text: string): string {
	return text.length > maxDetailLength ? text.substring(0, maxDetailLength - 1) + '…' : text;
}
