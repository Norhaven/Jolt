// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import fs from 'node:fs';

// Where the site is served from. The default assumes a GitHub Pages *project* site
// (https://norhaven.github.io/Jolt). To serve from the root of the Norhaven.github.io
// user site instead, set DOCS_BASE=/ in the deploy workflow.
const site = process.env.DOCS_SITE ?? 'https://norhaven.github.io';
const base = process.env.DOCS_BASE ?? '/';

const readJson = (path, fallback) =>
	fs.existsSync(path) ? JSON.parse(fs.readFileSync(path, 'utf8')) : fallback;

// Jolt syntax highlighting: `jolt` for transformer JSON, `jolt-expr` for bare expressions.
const joltGrammar = readJson('./src/grammars/jolt.tmLanguage.json');
const joltExprGrammar = readJson('./src/grammars/jolt-expr.tmLanguage.json');

// Written by scripts/generate-methods.mjs from data/methods.yaml (runs before dev/build).
const librarySidebar = readJson('./src/generated/library-sidebar.json', []);

// The generator copies ../JoltLogo.png here when it exists.
const hasLogo = fs.existsSync('./src/assets/jolt-logo.png');

export default defineConfig({
	site,
	base,
	integrations: [
		starlight({
			title: 'Jolt',
			description: 'An imperative JSON transformation language for .NET.',
			...(hasLogo ? { logo: { src: './src/assets/jolt-logo.png', alt: 'Jolt' } } : {}),
			favicon: '/favicon.svg',
			social: [
				{ icon: 'github', label: 'GitHub', href: 'https://github.com/Norhaven/Jolt' },
			],
			editLink: {
				baseUrl: 'https://github.com/Norhaven/Jolt/edit/main/docs/',
			},
			lastUpdated: true,
			customCss: ['./src/styles/jolt.css'],
			expressiveCode: {
				shiki: {
					langs: [joltExprGrammar, joltGrammar],
				},
			},
			sidebar: [
				{
					label: 'Getting Started',
					items: [{ autogenerate: { directory: 'getting-started' } }],
				},
				{ label: 'Playground', slug: 'playground' },
				{
					label: 'Language Guide',
					items: [{ autogenerate: { directory: 'guide' } }],
				},
				{
					label: 'Library Reference',
					items: [
						{ label: 'All methods', slug: 'reference/library' },
						...librarySidebar,
					],
				},
				{
					label: 'Host Integration (.NET)',
					items: [{ autogenerate: { directory: 'host' } }],
				},
				{
					label: 'Reference',
					items: [
						{ label: 'Operator precedence', slug: 'reference/operator-precedence' },
						{ label: 'Grammar (EBNF)', slug: 'reference/grammar' },
					],
				},
				{
					label: 'Contributing',
					items: [{ autogenerate: { directory: 'contributing' } }],
				},
			],
		}),
	],
});
