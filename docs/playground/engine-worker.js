// Runs the Jolt WebAssembly engine off the page's main thread.
//
// Not bundled: `npm run engine` copies it as-is into public/playground/engine/<version>/, next to the engine
// it loads, so a worker only ever loads the engine from its own build. The page talks to it through
// src/components/playground/engine.js:
//
//   page → worker   { id, type: 'run', transformer, source, options }
//   worker → page   { type: 'ready', description } | { type: 'failed', message }
//                   { id, type: 'result', result } | { id, type: 'error', message }

let engine = null;

async function start() {
	const manifestUrl = new URL('./manifest.json', import.meta.url);
	const response = await fetch(manifestUrl);

	if (!response.ok) {
		throw new Error(
			response.status === 404
				? 'The playground has been updated since this page was loaded. Reload the page to use the new version.'
				: `Could not load the playground engine (HTTP ${response.status}).`,
		);
	}

	const manifest = await response.json();
	const { dotnet } = await import(new URL(manifest.entry, manifestUrl).href);
	const runtime = await dotnet.create();
	const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName ?? manifest.assembly);

	engine = exports.Jolt.Playground.PlaygroundEngine;

	return JSON.parse(engine.Describe());
}

const ready = start().then(
	(description) => postMessage({ type: 'ready', description }),
	(error) => postMessage({ type: 'failed', message: error?.message ?? String(error) }),
);

self.addEventListener('message', async (event) => {
	await ready;

	const { id, type, transformer, source, options } = event.data;

	if (type !== 'run' || !engine) {
		return;
	}

	try {
		const result = JSON.parse(engine.Run(transformer, source, JSON.stringify(options ?? {})));
		postMessage({ id, type: 'result', result });
	} catch (error) {
		postMessage({ id, type: 'error', message: error?.message ?? String(error) });
	}
});
