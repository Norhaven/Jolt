import * as esbuild from 'esbuild';

const production = process.argv.includes('--production');
const watch = process.argv.includes('--watch');

const context = await esbuild.context({
	entryPoints: ['src/extension.ts'],
	bundle: true,
	format: 'cjs',
	platform: 'node',
	target: 'node20',
	external: ['vscode'],
	// jsonc-parser's default (UMD) entry point uses dynamic requires that esbuild cannot bundle, so prefer its ESM build.
	mainFields: ['module', 'main'],
	outfile: 'dist/extension.js',
	minify: production,
	sourcemap: !production,
	logLevel: 'info'
});

if (watch) {
	await context.watch();
} else {
	await context.rebuild();
	await context.dispose();
}
