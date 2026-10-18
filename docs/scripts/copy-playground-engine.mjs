#!/usr/bin/env node
// Copies the published playground engine into public/playground/engine/ and writes a manifest that tells
// engine-worker.js which file starts the .NET runtime.
//
// Usage: node scripts/copy-playground-engine.mjs <publish output wwwroot>
// `npm run engine` runs `dotnet publish` and then this script.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const wwwroot = path.resolve(process.argv[2] ?? path.join(root, 'playground/publish/wwwroot'));
const sourceFramework = path.join(wwwroot, '_framework');
const target = path.join(root, 'public/playground/engine');

if (!fs.existsSync(sourceFramework)) {
	console.error(`No _framework folder in ${wwwroot}. Run \`dotnet publish\` on docs/playground first.`);
	process.exit(1);
}

// GitHub Pages compresses responses itself and won't serve these, so they only take up space.
const skipped = new Set(['.br', '.gz']);

fs.rmSync(target, { recursive: true, force: true });
fs.mkdirSync(path.join(target, '_framework'), { recursive: true });

let bytes = 0;
let count = 0;
for (const entry of fs.readdirSync(sourceFramework, { withFileTypes: true })) {
	if (!entry.isFile() || skipped.has(path.extname(entry.name))) continue;
	const from = path.join(sourceFramework, entry.name);
	fs.copyFileSync(from, path.join(target, '_framework', entry.name));
	bytes += fs.statSync(from).size;
	count++;
}

// The runtime's entry module is dotnet.js, or dotnet.<fingerprint>.js when asset fingerprinting is on.
const files = fs.readdirSync(path.join(target, '_framework'));
const entry = files.find((name) => name === 'dotnet.js') ??
	files.find((name) => /^dotnet\.(?!native\b|runtime\b|boot\b)[^.]+\.js$/.test(name));

if (!entry) {
	console.error(`Couldn't find the runtime entry (dotnet.js) among: ${files.join(', ')}`);
	process.exit(1);
}

fs.writeFileSync(
	path.join(target, 'manifest.json'),
	JSON.stringify({ entry: `./_framework/${entry}`, assembly: 'Jolt.Playground.dll' }, null, 2),
);

console.log(`Copied ${count} engine files (${(bytes / 1024 / 1024).toFixed(1)} MB) to public/playground/engine/, entry ${entry}.`);
