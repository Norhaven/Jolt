import { Edit, format } from 'jsonc-parser';

export interface FormattingOptions {
	tabSize: number;
	insertSpaces: boolean;
	/** The document's line ending, which new lines use. */
	eol: string;
}

/** A span of a document, as offsets. */
export interface OffsetRange {
	offset: number;
	length: number;
}

/**
 * Gets the edits that format a transformer document as JSON, or just the given range of it. Jolt expressions are
 * always within JSON strings, which formatting leaves untouched.
 */
export function getFormattingEdits(text: string, range: OffsetRange | undefined, options: FormattingOptions): Edit[] {
	return format(text, range, { ...options, keepLines: false });
}
