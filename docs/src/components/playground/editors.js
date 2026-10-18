// CodeMirror editors for the playground. Colors come from CSS variables defined in Playground.astro, so the editors
// follow the site's light and dark themes without being recreated.

import { basicSetup } from 'codemirror';
import { EditorState } from '@codemirror/state';
import { EditorView } from '@codemirror/view';
import { json } from '@codemirror/lang-json';
import { HighlightStyle, syntaxHighlighting } from '@codemirror/language';
import { lintGutter, setDiagnostics } from '@codemirror/lint';
import { tags as t } from '@lezer/highlight';
import { joltCompletions, joltHighlighting } from './jolt-language.js';

const theme = EditorView.theme({
	'&': {
		height: '100%',
		fontSize: '0.8125rem',
		color: 'var(--jp-text)',
		backgroundColor: 'var(--jp-editor-bg)',
	},
	'.cm-scroller': { fontFamily: 'var(--sl-font-mono)', lineHeight: '1.6' },
	'.cm-content': { caretColor: 'var(--jp-cursor)' },
	'.cm-cursor, .cm-dropCursor': { borderLeftColor: 'var(--jp-cursor)' },
	'&.cm-focused': { outline: 'none' },
	'&.cm-focused > .cm-scroller > .cm-selectionLayer .cm-selectionBackground, .cm-selectionBackground, .cm-content ::selection': {
		backgroundColor: 'var(--jp-selection)',
	},
	'.cm-gutters': { backgroundColor: 'var(--jp-editor-bg)', color: 'var(--jp-muted)', border: 'none' },
	'.cm-activeLine': { backgroundColor: 'var(--jp-active-line)' },
	'.cm-activeLineGutter': { backgroundColor: 'var(--jp-active-line)', color: 'var(--jp-text)' },
	'.cm-matchingBracket, &.cm-focused .cm-matchingBracket': { backgroundColor: 'var(--jp-bracket)', outline: 'none' },
	'.cm-foldPlaceholder': { backgroundColor: 'transparent', border: '1px solid var(--jp-border)', color: 'var(--jp-muted)' },
	'.cm-tooltip': {
		backgroundColor: 'var(--jp-panel-bg)',
		color: 'var(--jp-text)',
		border: '1px solid var(--jp-border)',
		borderRadius: '0.375rem',
	},
	'.cm-tooltip-autocomplete > ul > li[aria-selected]': { backgroundColor: 'var(--jp-selection)', color: 'var(--jp-text)' },
	'.cm-completionDetail': { color: 'var(--jp-muted)', fontStyle: 'normal', marginLeft: '0.75rem' },
	'.cm-diagnostic': { borderLeftWidth: '3px' },
	'.cm-diagnostic-error': { borderLeftColor: 'var(--jp-error)' },
	'.cm-diagnostic-warning': { borderLeftColor: 'var(--jp-warning)' },
	'.cm-lintRange-error': { backgroundImage: 'none', textDecoration: 'underline wavy var(--jp-error)', textUnderlineOffset: '3px' },
	'.cm-lintRange-warning': { backgroundImage: 'none', textDecoration: 'underline wavy var(--jp-warning)', textUnderlineOffset: '3px' },
});

// Strings are colored by joltHighlighting, which tells expressions apart from plain strings.
const jsonColors = HighlightStyle.define([
	{ tag: t.number, color: 'var(--jp-number)' },
	{ tag: [t.bool, t.null], color: 'var(--jp-constant)' },
	{ tag: [t.brace, t.squareBracket, t.separator, t.punctuation], color: 'var(--jp-punctuation)' },
]);

const jsonLanguage = json();

/**
 * @param {HTMLElement} parent
 * @param {{ doc: string, readOnly?: boolean, label: string, onChange?: (text: string) => void }} options
 * @returns {EditorView}
 */
export function createEditor(parent, { doc, readOnly = false, label, onChange }) {
	const extensions = [
		basicSetup,
		jsonLanguage,
		jsonLanguage.language.data.of({ autocomplete: joltCompletions }),
		joltHighlighting,
		syntaxHighlighting(jsonColors),
		lintGutter(),
		theme,
		EditorState.tabSize.of(4),
		EditorView.contentAttributes.of({ 'aria-label': label }),
	];

	if (readOnly) {
		extensions.push(EditorState.readOnly.of(true));
	}

	if (onChange) {
		extensions.push(EditorView.updateListener.of((update) => update.docChanged && onChange(update.state.doc.toString())));
	}

	return new EditorView({ parent, state: EditorState.create({ doc, extensions }) });
}

/** Replaces an editor's whole document. */
export function setText(view, text) {
	if (view.state.doc.toString() === text) return;
	view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: text } });
}

/**
 * Shows engine diagnostics (character offsets) in an editor.
 * @param {EditorView} view
 * @param {import('./engine.js').EngineDiagnostic[]} diagnostics
 */
export function showDiagnostics(view, diagnostics) {
	const length = view.state.doc.length;
	const clamp = (offset) => Math.max(0, Math.min(offset, length));

	view.dispatch(
		setDiagnostics(
			view.state,
			diagnostics.map((d) => ({
				from: clamp(d.from),
				to: clamp(Math.max(d.to, d.from)),
				severity: d.severity,
				source: d.code ? `Jolt ${d.code}` : 'Jolt',
				message: d.message,
			})),
		),
	);
}

/** Moves the cursor to a range and scrolls it into view. */
export function revealRange(view, from, to) {
	const length = view.state.doc.length;
	view.dispatch({
		selection: { anchor: Math.min(from, length), head: Math.min(to, length) },
		effects: EditorView.scrollIntoView(Math.min(from, length), { y: 'center' }),
	});
	view.focus();
}
