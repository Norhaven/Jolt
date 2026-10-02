export interface MethodReference {
	/** The method name, without the leading "#". */
	name: string;
	/** The character index of the leading "#" within the line. */
	start: number;
	/** The character index just past the end of the method name. */
	end: number;
}

const methodReference = /#([A-Za-z_]\w*)/g;

/**
 * Finds the method reference (e.g. "#valueOf") that spans the given character of a line, if any.
 */
export function getMethodReferenceAt(lineText: string, character: number): MethodReference | undefined {
	for (const match of lineText.matchAll(methodReference)) {
		const start = match.index;
		const end = start + match[0].length;

		if (character >= start && character <= end) {
			return { name: match[1], start, end };
		}
	}

	return undefined;
}
