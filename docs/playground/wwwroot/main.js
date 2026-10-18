// Self-test for `dotnet run`: loads the engine on the page's main thread and runs one transformation.
import { dotnet } from './_framework/dotnet.js';

const out = document.getElementById('out');

try {
	const runtime = await dotnet.create();
	const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
	const engine = exports.Jolt.Playground.PlaygroundEngine;

	const transformer = JSON.stringify({
		greeting: "#append('Hello, ', #valueOf($.name))",
		reversed: '#valueOf($.name)->#ReverseString()',
		typo: '#valuOf($.name)',
	});

	const description = JSON.parse(engine.Describe());
	const valid = JSON.parse(engine.Run(transformer.replace(/,"typo":"[^"]*"/, ''), '{"name":"Ada"}', '{"trace":true}'));
	const invalid = JSON.parse(engine.Run(transformer, '{"name":"Ada"}', '{}'));

	out.textContent = JSON.stringify({ description, valid, invalid }, null, 2);
} catch (error) {
	out.textContent = `Failed: ${error?.stack ?? error}`;
}
