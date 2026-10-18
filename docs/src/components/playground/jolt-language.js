// Jolt support for CodeMirror: expression highlighting inside JSON strings, and method completions.
//
// Highlighting reuses the TextMate grammar the docs site uses for code blocks (src/grammars/jolt-expr.tmLanguage.json),
// so a change to the grammar shows up in both places.

import { syntaxTree } from '@codemirror/language';
import { RangeSetBuilder } from '@codemirror/state';
import { Decoration, ViewPlugin } from '@codemirror/view';
import grammar from '../../grammars/jolt-expr.tmLanguage.json';
import methods from '../../generated/method-index.json';
import { createTokenizer, EXPRESSION_START } from './jolt-tokenizer.js';

// Grammar rule → CSS class. Rules not listed here (e.g. punctuation) are left uncolored.
const CLASS_FOR_RULE = {
	'string-literal': 'cm-jolt-string',
	method: 'cm-jolt-method',
	variable: 'cm-jolt-variable',
	path: 'cm-jolt-path',
	keyword: 'cm-jolt-keyword',
	constant: 'cm-jolt-constant',
	discard: 'cm-jolt-discard',
	number: 'cm-jolt-number',
	operator: 'cm-jolt-operator',
};

const tokenize = createTokenizer(grammar);
const marks = Object.fromEntries(Object.entries(CLASS_FOR_RULE).map(([rule, cls]) => [rule, Decoration.mark({ class: cls })]));
const plainStringMark = Decoration.mark({ class: 'cm-jolt-plain-string' });
const keyMark = Decoration.mark({ class: 'cm-jolt-key' });

function buildDecorations(view) {
	const builder = new RangeSetBuilder();
	// A long string can span two visible ranges; ranges must be added in order, and only once.
	let lastEnd = -1;

	for (const { from, to } of view.visibleRanges) {
		syntaxTree(view.state).iterate({
			from,
			to,
			enter(node) {
				const isKey = node.name === 'PropertyName';
				if (!isKey && node.name !== 'String') return;
				if (node.from < lastEnd) return;
				lastEnd = node.to;

				const raw = view.state.doc.sliceString(node.from, node.to);
				const closed = raw.length >= 2 && raw.endsWith('"');
				const content = raw.slice(1, closed ? -1 : undefined);
				const contentStart = node.from + 1;

				if (EXPRESSION_START.test(content)) {
					for (const token of tokenize(content)) {
						const mark = marks[token.rule];
						if (mark) builder.add(contentStart + token.from, contentStart + token.to, mark);
					}
				} else {
					builder.add(node.from, node.to, isKey ? keyMark : plainStringMark);
				}
			},
		});
	}

	return builder.finish();
}

/** Highlights Jolt expressions inside the strings of a JSON document. */
export const joltHighlighting = ViewPlugin.fromClass(
	class {
		constructor(view) {
			this.decorations = buildDecorations(view);
		}

		update(update) {
			if (update.docChanged || update.viewportChanged || syntaxTree(update.startState) !== syntaxTree(update.state)) {
				this.decorations = buildDecorations(update.view);
			}
		}
	},
	{ decorations: (plugin) => plugin.decorations },
);

const methodOptions = methods.map((method) => ({
	label: `#${method.name}`,
	type: 'function',
	detail: method.signature.length <= 60 ? method.signature : undefined,
	info: method.summary,
	boost: method.common ? 1 : 0,
	apply(view, completion, from, to) {
		// Insert "#name()" with the cursor between the parentheses.
		const insert = `#${method.name}()`;
		view.dispatch({
			changes: { from, to, insert },
			selection: { anchor: from + insert.length - 1 },
		});
	},
}));

/**
 * Completes library method names after "#" inside a JSON string.
 * @param {import('@codemirror/autocomplete').CompletionContext} context
 */
export function joltCompletions(context) {
	const node = syntaxTree(context.state).resolveInner(context.pos, -1);
	if (node.name !== 'String' && node.name !== 'PropertyName') return null;

	const word = context.matchBefore(/#[A-Za-z_]*/);
	if (!word || (word.from === word.to && !context.explicit)) return null;

	return { from: word.from, options: methodOptions, validFor: /^#[A-Za-z_]*$/ };
}
