#!/usr/bin/env node
// Copies the published playground engine and its Web Worker into public/playground/engine/<version>/ and writes a
// manifest that tells engine-worker.js which file starts the .NET runtime.
//
// <version> is a hash of everything copied, so each build's engine lives at its own URLs and never changes once
// deployed. GitHub Pages lets browsers reuse any file for up to 10 minutes without asking, and the runtime and
// assemblies keep the same file names from build to build; without the version folder a browser could combine
// files from two deploys. Playground.astro finds the folder when the site builds and points the page at its
// worker, so a page always loads the engine it was built with.
//
// Usage: node scripts/copy-playground-engine.mjs <publish output wwwroot>
// `npm run engine` runs `dotnet publish` and then this script.

import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const wwwroot = path.resolve(process.argv[2] ?? path.join(root, 'playground/publish/wwwroot'));
const sourceFramework = path.join(wwwroot, '_framework');
const sourceWorker = path.join(root, 'playground/engine-worker.js');
const engineRoot = path.join(root, 'public/playground/engine');

if (!fs.existsSync(sourceFramework)) {
	console.error(`No _framework folder in ${wwwroot}. Run \`dotnet publish\` on docs/playground first.`);
	process.exit(1);
}

// GitHub Pages compresses responses itself and won't serve these, so they only take up space.
const skipped = new Set(['.br', '.gz']);

const frameworkFiles = fs.readdirSync(sourceFramework, { withFileTypes: true })
	.filter((entry) => entry.isFile() && !skipped.has(path.extname(entry.name)))
	.map((entry) => entry.name)
	.sort();

// The runtime's entry module is dotnet.js, or dotnet.<fingerprint>.js when asset fingerprinting is on.
const entry = frameworkFiles.find((name) => name === 'dotnet.js') ??
	frameworkFiles.find((name) => /^dotnet\.(?!native\b|runtime\b|boot\b)[^.]+\.js$/.test(name));

if (!entry) {
	console.error(`Couldn't find the runtime entry (dotnet.js) among: ${frameworkFiles.join(', ')}`);
	process.exit(1);
}

const manifest = JSON.stringify({ entry: `./_framework/${entry}`, assembly: 'Jolt.Playground.dll' }, null, 2);

// Hash names and contents, so renaming, adding, or removing a file also produces a new version.
const hash = crypto.createHash('sha256');
for (const name of frameworkFiles) {
	hash.update(`_framework/${name}\0`).update(fs.readFileSync(path.join(sourceFramework, name)));
}
hash.update('engine-worker.js\0').update(fs.readFileSync(sourceWorker));
hash.update('manifest.json\0').update(manifest);
const version = hash.digest('hex').slice(0, 12);

const target = path.join(engineRoot, version);

// Only the current version is kept. A page built against an earlier one gets a 404 when it starts the worker,
// which the playground reports as "reload the page".
fs.rmSync(engineRoot, { recursive: true, force: true });
fs.mkdirSync(path.join(target, '_framework'), { recursive: true });

let bytes = 0;
for (const name of frameworkFiles) {
	const from = path.join(sourceFramework, name);
	fs.copyFileSync(from, path.join(target, '_framework', name));
	bytes += fs.statSync(from).size;
}

fs.copyFileSync(sourceWorker, path.join(target, 'engine-worker.js'));
fs.writeFileSync(path.join(target, 'manifest.json'), manifest);

console.log(
	`Copied ${frameworkFiles.length} engine files (${(bytes / 1024 / 1024).toFixed(1)} MB) to ` +
	`public/playground/engine/${version}/, entry ${entry}.`,
);
