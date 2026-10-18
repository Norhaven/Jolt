// Wires up the playground page (src/components/playground/Playground.astro).

import { createEditor, revealRange, setText, showDiagnostics } from './editors.js';
import { PlaygroundEngine, TimeoutError } from './engine.js';
import { decodeState, encodeState } from './share.js';
import docExamples from '../../generated/doc-examples.json';

const RUN_DELAY_MS = 250;
const SAVE_DELAY_MS = 600;

const json = (value) => JSON.stringify(value, null, 4);

/** The example shown when the page is opened without a shared state. */
const DEFAULT_STATE = {
	source: json({
		order: {
			customer: { name: 'Ada Lovelace' },
			currency: 'GBP',
			items: [
				{ sku: 'ENG-1', price: 120, quantity: 1 },
				{ sku: 'CARD-50', price: 2.5, quantity: 40 },
			],
		},
	}),
	transformer: json({
		'@items': '#valueOf($.order.items)',
		customer: '#valueOf($.order.customer.name)->#toUpperCase()',
		total: '@items->#sum(@x: @x.price * @x.quantity)',
		"#foreach(@x in @items) into 'lines'": [{ sku: '@x.sku', qty: '@x.quantity' }],
	}),
	options: { looseErrorHandling: false, allowUnsafe: false, trace: false },
};

const root = document.querySelector('[data-jolt-playground]');
if (root) start(root);

function start(root) {
	const $ = (selector) => root.querySelector(selector);

	const ui = {
		status: $('[data-status]'),
		elapsed: $('[data-elapsed]'),
		error: $('[data-error]'),
		issues: $('[data-issues]'),
		issueCount: $('[data-issue-count]'),
		trace: $('[data-trace]'),
		tracePanel: $('[data-trace-panel]'),
		externalMethods: $('[data-external-methods]'),
		examples: $('[data-examples]'),
		share: $('[data-action="share"]'),
		format: $('[data-action="format"]'),
		reset: $('[data-action="reset"]'),
		options: {
			looseErrorHandling: $('[data-option="looseErrorHandling"]'),
			allowUnsafe: $('[data-option="allowUnsafe"]'),
			trace: $('[data-option="trace"]'),
		},
	};

	const engine = new PlaygroundEngine(root.dataset.workerUrl);

	const editors = {
		source: createEditor($('[data-editor="source"]'), { doc: '', label: 'Source document', onChange: onEdit }),
		transformer: createEditor($('[data-editor="transformer"]'), { doc: '', label: 'Transformer', onChange: onEdit }),
		output: createEditor($('[data-editor="output"]'), { doc: '', label: 'Output', readOnly: true }),
	};

	let runTimer = 0;
	let saveTimer = 0;
	let latestRun = 0;
	// Set while the page itself changes the hash, so that change isn't loaded back in.
	let writingHash = false;
	// Set while a state is loaded into the editors, so loading one doesn't count as an edit.
	let applying = false;

	// ── State ────────────────────────────────────────────────────────

	function currentOptions() {
		return Object.fromEntries(Object.entries(ui.options).map(([name, input]) => [name, input.checked]));
	}

	function currentState() {
		return {
			source: editors.source.state.doc.toString(),
			transformer: editors.transformer.state.doc.toString(),
			options: currentOptions(),
		};
	}

	function applyState(state) {
		applying = true;
		setText(editors.source, state.source);
		setText(editors.transformer, state.transformer);
		applying = false;
		for (const [name, input] of Object.entries(ui.options)) input.checked = state.options?.[name] === true;
		runNow();
	}

	async function saveToHash() {
		const hash = await encodeState(currentState());
		writingHash = true;
		history.replaceState(null, '', `#${hash}`);
		writingHash = false;
	}

	function scheduleSave() {
		clearTimeout(saveTimer);
		saveTimer = setTimeout(saveToHash, SAVE_DELAY_MS);
	}

	// ── Running ──────────────────────────────────────────────────────

	function onEdit() {
		if (applying) return;
		scheduleRun();
		scheduleSave();
	}

	function scheduleRun() {
		clearTimeout(runTimer);
		runTimer = setTimeout(runNow, RUN_DELAY_MS);
	}

	async function runNow() {
		clearTimeout(runTimer);
		const runId = ++latestRun;
		const state = currentState();

		try {
			const result = await engine.run(state);
			if (runId === latestRun) showResult(result, state.options);
		} catch (error) {
			if (runId !== latestRun) return;
			showDiagnostics(editors.source, []);
			showDiagnostics(editors.transformer, []);
			renderIssues([]);
			showError(error instanceof TimeoutError ? { message: error.message } : { message: error.message ?? String(error) });
			ui.elapsed.textContent = '';
		}
	}

	function showResult(result, options) {
		showDiagnostics(editors.source, result.sourceDiagnostics);
		showDiagnostics(editors.transformer, result.transformerDiagnostics);
		renderIssues([
			...result.sourceDiagnostics.map((d) => ({ ...d, editor: 'source' })),
			...result.transformerDiagnostics.map((d) => ({ ...d, editor: 'transformer' })),
		]);

		if (result.skipped) {
			showError({ message: 'Fix the errors below to see the output.' }, 'notice');
			ui.elapsed.textContent = '';
		} else if (result.error) {
			showError(result.error);
			setText(editors.output, '');
		} else {
			showError(null);
			setText(editors.output, result.output ?? 'null');
		}

		if (typeof result.elapsedMilliseconds === 'number') {
			ui.elapsed.textContent = `${formatMs(result.elapsedMilliseconds)}`;
		}

		ui.tracePanel.hidden = !options.trace;
		ui.trace.textContent = result.trace || (options.trace ? 'No trace entries were recorded.' : '');
	}

	function showError(error, kind = 'error') {
		ui.error.hidden = !error;
		ui.error.dataset.kind = kind;
		ui.error.textContent = error ? (error.code ? `${error.code}: ${error.message}` : error.message) : '';
	}

	function renderIssues(issues) {
		ui.issueCount.textContent = issues.length ? String(issues.length) : '';
		ui.issues.replaceChildren(
			...(issues.length
				? issues.map((issue) => {
						const item = document.createElement('li');
						const button = document.createElement('button');
						button.type = 'button';
						button.dataset.severity = issue.severity;
						button.innerHTML = '<span class="jp-issue-where"></span><span class="jp-issue-message"></span>';
						const view = editors[issue.editor];
						const line = view.state.doc.lineAt(Math.min(issue.from, view.state.doc.length)).number;
						button.querySelector('.jp-issue-where').textContent =
							`${issue.editor === 'source' ? 'Source' : 'Transformer'} · line ${line}${issue.code ? ` · ${issue.code}` : ''}`;
						button.querySelector('.jp-issue-message').textContent = issue.message;
						button.addEventListener('click', () => revealRange(view, issue.from, issue.to));
						item.append(button);
						return item;
					})
				: [Object.assign(document.createElement('li'), { className: 'jp-no-issues', textContent: 'No issues.' })]),
		);
	}

	// ── Engine status ────────────────────────────────────────────────

	function renderStatus() {
		const labels = {
			idle: 'Engine idle',
			loading: 'Loading engine…',
			ready: `Jolt ${engine.description?.version ?? ''}`.trim(),
			failed: 'Engine unavailable',
		};
		ui.status.textContent = labels[engine.status];
		ui.status.dataset.state = engine.status;
		ui.status.title = engine.status === 'failed' ? (engine.failure ?? '') : '';

		if (engine.status === 'failed') {
			showError({ message: engine.failure ?? 'The playground engine could not be started.' });
		}

		if (engine.status === 'ready' && engine.description && !ui.externalMethods.childElementCount) {
			ui.externalMethods.replaceChildren(
				...engine.description.externalMethods.map((method) => {
					const item = document.createElement('li');
					const code = document.createElement('code');
					code.textContent = method.signature;
					item.append(code);
					return item;
				}),
			);
		}
	}

	engine.addEventListener('statuschange', renderStatus);
	renderStatus();

	// ── Toolbar ──────────────────────────────────────────────────────

	for (const input of Object.values(ui.options)) input.addEventListener('change', onEdit);

	ui.share.addEventListener('click', async () => {
		await saveToHash();
		try {
			await navigator.clipboard.writeText(location.href);
			flash(ui.share, 'Link copied');
		} catch {
			flash(ui.share, 'Copy the address bar');
		}
	});

	ui.format.addEventListener('click', () => {
		for (const name of ['source', 'transformer']) {
			try {
				setText(editors[name], json(JSON.parse(editors[name].state.doc.toString())));
			} catch {
				// Leave invalid JSON as it is; its errors are already shown.
			}
		}
	});

	ui.reset.addEventListener('click', () => {
		ui.examples.value = '';
		applyState(DEFAULT_STATE);
		scheduleSave();
	});

	populateExamples();
	ui.examples.addEventListener('change', () => {
		const example = docExamples[Number(ui.examples.value)];
		if (!example) return;
		applyState({ source: json(example.source), transformer: json(example.transformer), options: currentOptions() });
		scheduleSave();
	});

	function populateExamples() {
		const groups = new Map();
		docExamples.forEach((example, index) => {
			if (!groups.has(example.method)) groups.set(example.method, []);
			groups.get(example.method).push({ example, index });
		});

		const sorted = [...groups.entries()].sort(([a], [b]) => a.localeCompare(b, 'en', { sensitivity: 'base' }));
		for (const [method, entries] of sorted) {
			const group = document.createElement('optgroup');
			group.label = `#${method}`;
			for (const { example, index } of entries) {
				group.append(new Option(example.title, String(index)));
			}
			ui.examples.append(group);
		}
	}

	// ── Load ─────────────────────────────────────────────────────────

	window.addEventListener('hashchange', async () => {
		if (writingHash) return;
		const state = await decodeState(location.hash);
		if (state) applyState(state);
	});

	decodeState(location.hash).then((state) => applyState(state ?? DEFAULT_STATE));
}

function formatMs(ms) {
	if (ms < 1) return '<1 ms';
	if (ms < 1000) return `${Math.round(ms)} ms`;
	return `${(ms / 1000).toFixed(2)} s`;
}

function flash(button, text) {
	const original = button.dataset.label ?? button.textContent;
	button.dataset.label = original;
	button.textContent = text;
	setTimeout(() => (button.textContent = original), 1600);
}
