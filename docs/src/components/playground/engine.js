// Talks to the Jolt engine running in a Web Worker (public/playground/engine-worker.js).
//
// The worker is created on first use, so the .NET runtime is only downloaded on pages that run something.
// A run that takes too long terminates the worker, since a busy WebAssembly thread can't be interrupted
// any other way, and the next run starts a fresh one.

/**
 * @typedef {{ from: number, to: number, severity: 'error' | 'warning' | 'info', code?: string, message: string }} EngineDiagnostic
 * @typedef {{
 *   transformerDiagnostics: EngineDiagnostic[],
 *   sourceDiagnostics: EngineDiagnostic[],
 *   skipped?: boolean,
 *   output?: string,
 *   error?: { code?: string, message: string },
 *   elapsedMilliseconds?: number,
 *   trace?: string,
 * }} RunResult
 * @typedef {{ version: string, externalMethods: { name: string, signature: string }[] }} EngineDescription
 */

export class TimeoutError extends Error {}

export class PlaygroundEngine extends EventTarget {
	/** @type {'idle' | 'loading' | 'ready' | 'failed'} */
	status = 'idle';
	/** @type {EngineDescription | null} */
	description = null;
	/** @type {string | null} */
	failure = null;

	#workerUrl;
	#timeoutMs;
	#worker = null;
	#ready = null;
	#nextId = 1;
	#pending = new Map();

	/**
	 * @param {string | URL} workerUrl
	 * @param {{ timeoutMs?: number }} [options]
	 */
	constructor(workerUrl, { timeoutMs = 5000 } = {}) {
		super();
		this.#workerUrl = workerUrl;
		this.#timeoutMs = timeoutMs;
	}

	/**
	 * @param {{ transformer: string, source: string, options: object }} request
	 * @returns {Promise<RunResult>}
	 */
	async run(request) {
		await this.#start();

		const id = this.#nextId++;

		return new Promise((resolve, reject) => {
			const timer = setTimeout(() => {
				this.#restart(new TimeoutError(`The transformation took longer than ${this.#timeoutMs / 1000} seconds, so it was stopped.`));
			}, this.#timeoutMs);

			this.#pending.set(id, { resolve, reject, timer });
			this.#worker.postMessage({ id, type: 'run', ...request });
		});
	}

	/** Starts the worker if needed and waits until the engine reports that it's ready. */
	#start() {
		if (this.#ready) return this.#ready;

		this.#setStatus('loading');

		this.#ready = new Promise((resolve, reject) => {
			const worker = new Worker(this.#workerUrl, { type: 'module', name: 'jolt-engine' });
			this.#worker = worker;

			worker.onmessage = ({ data }) => {
				switch (data.type) {
					case 'ready':
						this.description = data.description;
						this.#setStatus('ready');
						resolve();
						break;
					case 'failed':
						this.#fail(data.message);
						reject(new Error(data.message));
						break;
					case 'result':
					case 'error':
						this.#settle(data);
						break;
				}
			};

			worker.onerror = (event) => {
				event.preventDefault?.();
				const message = event.message || 'The playground engine could not be started.';
				this.#fail(message);
				reject(new Error(message));
			};
		});

		return this.#ready;
	}

	#settle({ id, type, result, message }) {
		const pending = this.#pending.get(id);
		if (!pending) return;

		clearTimeout(pending.timer);
		this.#pending.delete(id);

		if (type === 'result') pending.resolve(result);
		else pending.reject(new Error(message));
	}

	/** Stops the worker, fails everything in flight, and lets the next run start a new one. */
	#restart(error) {
		this.#worker?.terminate();
		this.#worker = null;
		this.#ready = null;

		for (const { reject, timer } of this.#pending.values()) {
			clearTimeout(timer);
			reject(error);
		}
		this.#pending.clear();

		this.#setStatus('idle');
	}

	#fail(message) {
		this.failure = message;
		this.#worker?.terminate();
		this.#worker = null;
		// Keep #ready as the rejected promise, so later runs fail immediately instead of retrying a broken engine.
		this.#setStatus('failed');
	}

	#setStatus(status) {
		this.status = status;
		this.dispatchEvent(new Event('statuschange'));
	}
}
