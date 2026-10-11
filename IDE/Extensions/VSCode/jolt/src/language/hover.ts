import { findNodeAtOffset, parseTree } from 'jsonc-parser';
import { forEachOutsideLiterals, getStringContent, getStringContentStart } from './context';

export interface MethodReference {
	/** The method name, without the leading "#". */
	name: string;
	/** The document offset of the leading "#". */
	start: number;
	/** The document offset just past the end of the method name. */
	end: number;
}

const methodReference = /#([A-Za-z_]\w*)/y;

/**
 * Finds the method reference (e.g. "#valueOf") that spans the given offset of a transformer document, if any. Method
 * references are only found within Jolt expressions (JSON strings), and not within their string literals.
 */
export function getMethodReferenceAt(text: string, offset: number): MethodReference | undefined {
	const root = parseTree(text, [], { allowTrailingComma: true });
	const node = root && findNodeAtOffset(root, offset, true);

	if (node?.type !== 'string') {
		return undefined;
	}

	const content = getStringContent(text, node);
	const contentStart = getStringContentStart(node);
	let found: MethodReference | undefined;

	forEachOutsideLiterals(content, i => {
		methodReference.lastIndex = i;
		const match = content[i] === '#' ? methodReference.exec(content) : null;

		if (!match) {
			return;
		}

		const start = contentStart + i;
		const end = start + match[0].length;

		if (offset >= start && offset <= end) {
			found = { name: match[1], start, end };
		}

		return i + match[0].length - 1;
	});

	return found;
}
