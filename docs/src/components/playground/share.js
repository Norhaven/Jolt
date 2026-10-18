// Encodes playground state into the URL hash, and back.
//
// Used both in the browser (the playground page) and in Node at build time (scripts/generate-methods.mjs, for the
// "Try it" links on method pages), so it only relies on web APIs that both provide: CompressionStream, Blob,
// Response, btoa and atob.
//
// Format: #code=<base64url(deflate-raw(JSON))>, where the JSON is
//   { "source": string, "transformer": string, "options"?: { looseErrorHandling, allowUnsafe, trace } }

const PREFIX = 'code=';

/**
 * @typedef {{ looseErrorHandling?: boolean, allowUnsafe?: boolean, trace?: boolean }} PlaygroundOptions
 * @typedef {{ source: string, transformer: string, options?: PlaygroundOptions }} PlaygroundState
 */

/**
 * @param {PlaygroundState} state
 * @returns {Promise<string>} The hash, without the leading "#".
 */
export async function encodeState(state) {
	const payload = { source: state.source, transformer: state.transformer };
	const options = compactOptions(state.options);
	if (options) payload.options = options;

	const compressed = new Blob([JSON.stringify(payload)]).stream().pipeThrough(new CompressionStream('deflate-raw'));
	const bytes = new Uint8Array(await new Response(compressed).arrayBuffer());

	return PREFIX + toBase64Url(bytes);
}

/**
 * @param {string} hash A location hash, with or without the leading "#".
 * @returns {Promise<PlaygroundState | null>} The state, or null when the hash doesn't hold one.
 */
export async function decodeState(hash) {
	const value = hash.replace(/^#/, '');
	if (!value.startsWith(PREFIX)) return null;

	try {
		const bytes = fromBase64Url(value.slice(PREFIX.length));
		const decompressed = new Blob([bytes]).stream().pipeThrough(new DecompressionStream('deflate-raw'));
		const parsed = JSON.parse(await new Response(decompressed).text());

		if (typeof parsed?.source !== 'string' || typeof parsed?.transformer !== 'string') return null;

		return {
			source: parsed.source,
			transformer: parsed.transformer,
			options: {
				looseErrorHandling: parsed.options?.looseErrorHandling === true,
				allowUnsafe: parsed.options?.allowUnsafe === true,
				trace: parsed.options?.trace === true,
			},
		};
	} catch {
		return null;
	}
}

/** Keeps only the options that are switched on, so links stay short. */
function compactOptions(options) {
	if (!options) return null;
	const entries = Object.entries(options).filter(([, on]) => on === true);
	return entries.length ? Object.fromEntries(entries) : null;
}

function toBase64Url(bytes) {
	let binary = '';
	for (let i = 0; i < bytes.length; i += 0x8000) {
		binary += String.fromCharCode(...bytes.subarray(i, i + 0x8000));
	}
	return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fromBase64Url(text) {
	const base64 = text.replace(/-/g, '+').replace(/_/g, '/');
	const binary = atob(base64 + '='.repeat((4 - (base64.length % 4)) % 4));
	const bytes = new Uint8Array(binary.length);
	for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
	return bytes;
}
