#!/usr/bin/env node
// Generates the library reference from data/methods.yaml.
//
// Outputs (all gitignored, regenerated before every dev/build):
//   src/content/docs/reference/library/index.md      the "All methods" overview
//   src/content/docs/reference/library/<name>.md     one page per method
//   src/generated/library-sidebar.json               sidebar groups, read by astro.config.mjs
//   src/generated/doc-examples.json                  examples with source/transformer/output, for tests
//                                                    and the playground's example list
//   src/generated/method-index.json                  names, signatures and summaries for playground completions
//
// Run directly with `npm run generate`.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { parse } from 'yaml';
import { encodeState } from '../src/components/playground/share.js';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const dataFile = path.join(root, 'data/methods.yaml');
const detailsDir = path.join(root, 'data/method-details');
const pagesDir = path.join(root, 'src/content/docs/reference/library');
const generatedDir = path.join(root, 'src/generated');

const REPO_EDIT_URL = 'https://github.com/Norhaven/Jolt/edit/main/docs/data/methods.yaml';

// Method pages live at /reference/library/<name>/, so this reaches the site root.
const PLAYGROUND_FROM_METHOD_PAGE = '../../../playground/';

// Offered first in playground completions.
const COMMON_METHODS = new Set(['valueOf', 'exists', 'if', 'foreach', 'select', 'where', 'sum', 'length', 'toString', 'joinWith']);

const VALID_IN = {
	'property-value': 'Property value',
	'property-name': 'Property name',
	statement: 'Statement',
	'match-case': 'Match case',
};

// ── Load and validate ────────────────────────────────────────────────

const data = parse(fs.readFileSync(dataFile, 'utf8'));
const categories = data.categories ?? {};
const methods = data.methods ?? [];
const errors = [];

const byName = new Map();
for (const m of methods) {
	const where = `method "${m.name ?? '(unnamed)'}"`;
	for (const field of ['name', 'category', 'summary', 'signature', 'validIn']) {
		if (m[field] === undefined) errors.push(`${where}: missing required field "${field}"`);
	}
	if (m.name && byName.has(m.name.toLowerCase())) errors.push(`${where}: duplicate name`);
	if (m.name) byName.set(m.name.toLowerCase(), m);
	if (m.category && !(m.category in categories)) errors.push(`${where}: unknown category "${m.category}"`);
	for (const v of m.validIn ?? []) {
		if (!(v in VALID_IN)) errors.push(`${where}: unknown validIn "${v}" (expected one of ${Object.keys(VALID_IN).join(', ')})`);
	}
	(m.examples ?? []).forEach((ex, i) => {
		const full = ['source', 'transformer', 'output'].filter((k) => ex[k] !== undefined);
		if (!ex.title) errors.push(`${where}: example ${i + 1} has no title`);
		if (ex.expression === undefined && full.length !== 3) {
			errors.push(`${where}: example ${i + 1} needs either "expression" or all of source/transformer/output`);
		}
	});
}
for (const m of methods) {
	for (const r of m.related ?? []) {
		if (!byName.has(String(r).toLowerCase())) errors.push(`method "${m.name}": related method "${r}" does not exist`);
	}
}

if (errors.length) {
	console.error(`data/methods.yaml has ${errors.length} problem(s):\n  - ${errors.join('\n  - ')}`);
	process.exit(1);
}

// ── Helpers ──────────────────────────────────────────────────────────

const slugOf = (name) => name.toLowerCase();
// Pretty JSON that keeps short arrays/objects on one line, the way a person would write them.
// The top-level object always gets one property per line.
const INLINE_WIDTH = 72;
function inline(value) {
	if (Array.isArray(value)) return value.length ? `[ ${value.map(inline).join(', ')} ]` : '[]';
	if (value && typeof value === 'object') {
		const entries = Object.entries(value);
		return entries.length ? `{ ${entries.map(([k, v]) => `${JSON.stringify(k)}: ${inline(v)}`).join(', ')} }` : '{}';
	}
	return JSON.stringify(value);
}
function json(value, depth = 0) {
	const pad = '    '.repeat(depth);
	const inner = '    '.repeat(depth + 1);
	if (value === null || typeof value !== 'object') return JSON.stringify(value);
	const oneLine = inline(value);
	if (depth > 0 && pad.length + oneLine.length <= INLINE_WIDTH) return oneLine;
	if (Array.isArray(value)) {
		if (!value.length) return '[]';
		return `[\n${value.map((v) => inner + json(v, depth + 1)).join(',\n')}\n${pad}]`;
	}
	const entries = Object.entries(value);
	if (!entries.length) return '{}';
	return `{\n${entries.map(([k, v]) => `${inner}${JSON.stringify(k)}: ${json(v, depth + 1)}`).join(',\n')}\n${pad}}`;
}
const cell = (text) => String(text ?? '').replace(/\|/g, '\\|').replace(/\r?\n/g, ' ');
const yamlString = (text) => JSON.stringify(String(text)); // JSON strings are valid YAML scalars
const langFor = (code) => (/^\s*["{]/.test(code) ? 'jolt' : 'jolt-expr');
const fence = (lang, code, title) => `\`\`\`${lang}${title ? ` title="${title}"` : ''}\n${code}\n\`\`\``;
const validInText = (m) => m.validIn.map((v) => VALID_IN[v]).join(', ');
const sortByName = (a, b) => a.name.localeCompare(b.name, 'en', { sensitivity: 'base' });

// ── Playground links ─────────────────────────────────────────────────

// Every complete example (source, transformer and output) gets a "Try it" link that opens it in the playground.
const tryItLinks = new Map();
for (const m of methods) {
	for (const ex of m.examples ?? []) {
		if (ex.source === undefined) continue;
		const hash = await encodeState({ source: json(ex.source), transformer: json(ex.transformer) });
		tryItLinks.set(ex, `${PLAYGROUND_FROM_METHOD_PAGE}#${hash}`);
	}
}

// ── Method pages ─────────────────────────────────────────────────────

function renderMethod(m) {
	const out = [];

	out.push('---');
	out.push(`title: ${yamlString(`#${m.name}`)}`);
	out.push(`description: ${yamlString(m.summary)}`);
	out.push(`editUrl: ${REPO_EDIT_URL}`);
	out.push('---');
	out.push('');
	out.push('<!-- Generated from data/methods.yaml by scripts/generate-methods.mjs. Do not edit. -->');
	out.push('');
	out.push(m.summary);
	out.push('');
	out.push(fence(langFor(m.signature), m.signature));
	out.push('');

	const meta = [
		`<span><strong>Category</strong> ${categories[m.category]}</span>`,
		`<span><strong>Valid in</strong> ${validInText(m)}</span>`,
	];
	if (m.unsafe) meta.push('<span><strong>Unsafe</strong> requires <code>WithUnsafeAllowed</code></span>');
	out.push(`<div class="jolt-meta">${meta.join('')}</div>`);
	out.push('');

	if (m.unsafe) {
		out.push(':::caution[Unsafe method]');
		out.push('This method is disabled unless you call `WithUnsafeAllowed()` on the `JoltOptions` passed to your transformer. Use it only with trusted input.');
		out.push(':::');
		out.push('');
	}

	if (m.parameters?.length) {
		out.push('## Parameters');
		out.push('');
		out.push('| Name | Type | Description |');
		out.push('| ---- | ---- | ----------- |');
		for (const p of m.parameters) {
			const name = `\`${cell(p.name)}\`${p.optional ? ' *(optional)*' : ''}`;
			out.push(`| ${name} | \`${cell(p.type)}\` | ${cell(p.description)} |`);
		}
		out.push('');
	}

	if (m.returns) {
		out.push('## Returns');
		out.push('');
		out.push(`\`${m.returns.type}\`${m.returns.description ? ` — ${m.returns.description}` : ''}`);
		out.push('');
	}

	if (m.notes?.length) {
		out.push('## Notes');
		out.push('');
		for (const n of m.notes) out.push(`- ${n}`);
		out.push('');
	}

	if (m.examples?.length) {
		out.push('## Examples');
		out.push('');
		for (const ex of m.examples) {
			out.push(`### ${ex.title}`);
			out.push('');
			if (ex.expression !== undefined) {
				out.push(fence(langFor(ex.expression), ex.expression));
			} else {
				out.push(fence('json', json(ex.source), 'source.json'));
				out.push('');
				out.push(fence('jolt', json(ex.transformer), 'transformer.json'));
				out.push('');
				out.push(fence('json', json(ex.output), 'output.json'));
				out.push('');
				out.push(`[Try it in the playground →](${tryItLinks.get(ex)})`);
			}
			out.push('');
		}
	}

	const detailsFile = path.join(detailsDir, `${m.name}.md`);
	if (fs.existsSync(detailsFile)) {
		out.push('## Details');
		out.push('');
		out.push(fs.readFileSync(detailsFile, 'utf8').trim());
		out.push('');
	}

	if (m.related?.length) {
		out.push('## Related');
		out.push('');
		const links = m.related.map((r) => {
			const target = byName.get(String(r).toLowerCase());
			return `[\`#${target.name}\`](../${slugOf(target.name)}/)`;
		});
		out.push(links.join(' · '));
		out.push('');
	}

	return out.join('\n');
}

// ── Index page ───────────────────────────────────────────────────────

function renderIndex() {
	const out = [];
	out.push('---');
	out.push('title: Library reference');
	out.push(`description: ${yamlString(`All ${methods.length} built-in Jolt methods, grouped by category.`)}`);
	out.push(`editUrl: ${REPO_EDIT_URL}`);
	out.push('---');
	out.push('');
	out.push('<!-- Generated from data/methods.yaml by scripts/generate-methods.mjs. Do not edit. -->');
	out.push('');
	out.push(`Jolt ships with ${methods.length} built-in methods. Any of them can be called directly or [piped](../../guide/methods-and-piping/#piping) with \`->\`, and most accept either a JSON Path or a value. Range variables can be passed to nearly all of them.`);
	out.push('');
	out.push('**Valid in** says where a method can appear: in a property value, in a property name, as a statement inside a [`#using` block](../../guide/using-blocks/), or as a case inside a [`#match`](../../guide/pattern-matching/).');
	out.push('');
	for (const [key, label] of Object.entries(categories)) {
		const inCategory = methods.filter((m) => m.category === key).sort(sortByName);
		if (!inCategory.length) continue;
		out.push(`## ${label}`);
		out.push('');
		out.push('| Method | Description | Valid in |');
		out.push('| ------ | ----------- | -------- |');
		for (const m of inCategory) {
			const unsafe = m.unsafe ? ' *(unsafe)*' : '';
			out.push(`| [\`#${m.name}\`](${slugOf(m.name)}/) | ${cell(m.summary)}${unsafe} | ${validInText(m)} |`);
		}
		out.push('');
	}
	out.push('Missing a method you need? [Raise an issue](https://github.com/Norhaven/Jolt/issues/new/choose).');
	out.push('');
	return out.join('\n');
}

// ── Write everything ─────────────────────────────────────────────────

fs.rmSync(pagesDir, { recursive: true, force: true });
fs.mkdirSync(pagesDir, { recursive: true });
fs.mkdirSync(generatedDir, { recursive: true });

fs.writeFileSync(path.join(pagesDir, 'index.md'), renderIndex());
for (const m of methods) {
	fs.writeFileSync(path.join(pagesDir, `${slugOf(m.name)}.md`), renderMethod(m));
}

const sidebar = Object.entries(categories)
	.map(([key, label]) => ({
		label,
		collapsed: true,
		items: methods
			.filter((m) => m.category === key)
			.sort(sortByName)
			.map((m) => ({ label: m.name, slug: `reference/library/${slugOf(m.name)}` })),
	}))
	.filter((group) => group.items.length);
fs.writeFileSync(path.join(generatedDir, 'library-sidebar.json'), JSON.stringify(sidebar, null, 2));

const docExamples = methods.flatMap((m) =>
	(m.examples ?? [])
		.filter((ex) => ex.source !== undefined)
		.map((ex) => ({
			method: m.name,
			title: ex.title,
			source: ex.source,
			transformer: ex.transformer,
			output: ex.output,
		})),
);
fs.writeFileSync(path.join(generatedDir, 'doc-examples.json'), JSON.stringify(docExamples, null, 2));

const methodIndex = [...methods].sort(sortByName).map((m) => ({
	name: m.name,
	signature: m.signature,
	summary: m.summary,
	common: COMMON_METHODS.has(m.name) || undefined,
}));
fs.writeFileSync(path.join(generatedDir, 'method-index.json'), JSON.stringify(methodIndex, null, 2));

// The header logo lives at the repo root; copy it in when present.
const logo = path.resolve(root, '../JoltLogo.png');
if (fs.existsSync(logo)) {
	fs.mkdirSync(path.join(root, 'src/assets'), { recursive: true });
	fs.copyFileSync(logo, path.join(root, 'src/assets/jolt-logo.png'));
}

console.log(
	`Generated ${methods.length} method pages in ${Object.keys(categories).length} categories, ${docExamples.length} executable examples.`,
);
