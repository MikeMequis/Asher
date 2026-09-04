import { randomUUID } from 'node:crypto';
import { EventEmitter } from 'node:events';

/**
 * JSON Lines client for Asher.Host --jsonl protocol.
 * Parses stdout lines, correlates responses by requestId, emits progress events.
 */
export class JsonlClient extends EventEmitter {
  #writer = null;
  #pending = new Map();
  #buffer = '';
  #ready = false;
  #closed = false;

  attach(writer, reader) {
    this.#writer = writer;
    reader.on('data', (chunk) => this.#onData(chunk));
    reader.on('close', () => this.#onReaderClosed());
    reader.on('error', (err) => this.#failAll(err));
  }

  get isReady() {
    return this.#ready;
  }

  get isClosed() {
    return this.#closed;
  }

  #onData(chunk) {
    this.#buffer += chunk.toString('utf8');

    let newlineIndex;
    while ((newlineIndex = this.#buffer.indexOf('\n')) !== -1) {
      const line = this.#buffer.slice(0, newlineIndex).trim();
      this.#buffer = this.#buffer.slice(newlineIndex + 1);

      if (!line) {
        continue;
      }

      this.#handleLine(line);
    }
  }

  #handleLine(line) {
    let message;
    try {
      message = JSON.parse(line);
    } catch {
      this.emit('protocol-error', { message: 'Malformed JSON from host', line });
      return;
    }

    const type = message.type;

    if (type === 'event' && message.event === 'ready') {
      this.#ready = true;
      this.emit('ready', message);
      return;
    }

    if (type === 'progress') {
      this.emit('progress', {
        requestId: message.requestId,
        progress: message.progress
      });
      const pending = this.#pending.get(message.requestId);
      if (pending?.onProgress) {
        pending.onProgress(message.progress);
      }
      return;
    }

    if (type === 'response') {
      const pending = this.#pending.get(message.requestId);
      if (!pending) {
        this.emit('orphan-response', message);
        return;
      }

      this.#pending.delete(message.requestId);
      if (message.success) {
        pending.resolve(message.result);
      } else if (pending.allowFailure) {
        pending.resolve({
          success: false,
          result: message.result,
          error: message.error
        });
      } else {
        const error = new Error(message.error?.message ?? 'Request failed');
        error.code = message.error?.code;
        pending.reject(error);
      }
      return;
    }

    this.emit('protocol-error', { message: `Unexpected message type: ${type}`, line });
  }

  #onReaderClosed() {
    this.#closed = true;
    this.#failAll(new Error('Host stdout closed'));
    this.emit('closed');
  }

  #failAll(err) {
    for (const pending of this.#pending.values()) {
      pending.reject(err);
    }
    this.#pending.clear();
  }

  /**
   * @param {string} method
   * @param {object} [params]
   * @param {{ onProgress?: (progress: object) => void, onStarted?: (requestId: string) => void, allowFailure?: boolean }} [options]
   */
  request(method, params, options = {}) {
    if (this.#closed) {
      return Promise.reject(new Error('Host connection is closed'));
    }

    if (!this.#ready) {
      return Promise.reject(new Error('Host is not ready'));
    }

    if (!this.#writer) {
      return Promise.reject(new Error('Host stdin is not attached'));
    }

    const requestId = randomUUID();
    const payload = { requestId, method };
    if (params !== undefined) {
      payload.params = params;
    }

    return new Promise((resolve, reject) => {
      this.#pending.set(requestId, {
        resolve,
        reject,
        onProgress: options.onProgress,
        allowFailure: options.allowFailure ?? false
      });

      try {
        this.#writer.write(JSON.stringify(payload) + '\n');
        options.onStarted?.(requestId);
      } catch (err) {
        this.#pending.delete(requestId);
        reject(err);
      }
    });
  }

  async shutdown() {
    if (!this.#ready || this.#closed) {
      return;
    }

    try {
      await this.request('shutdown');
    } catch {
      // Host may already be shutting down.
    }
  }
}
