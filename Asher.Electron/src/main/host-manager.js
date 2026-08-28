import { spawn } from 'node:child_process';
import { EventEmitter } from 'node:events';
import { JsonlClient } from './jsonl-client.js';
import { resolveHostPath } from './resolve-host-path.js';

/** @typedef {'stopped' | 'starting' | 'ready' | 'error' | 'terminated'} HostStatus */

const READY_TIMEOUT_MS = 30_000;

export class HostManager extends EventEmitter {
  /** @type {HostStatus} */
  #status = 'stopped';
  /** @type {string | null} */
  #statusMessage = null;
  /** @type {import('node:child_process').ChildProcess | null} */
  #process = null;
  /** @type {JsonlClient | null} */
  #client = null;
  #hostPath = null;

  get status() {
    return this.#status;
  }

  get statusMessage() {
    return this.#statusMessage;
  }

  get client() {
    return this.#client;
  }

  get hostPath() {
    return this.#hostPath;
  }

  #setStatus(status, message = null) {
    this.#status = status;
    this.#statusMessage = message;
    this.emit('status-changed', { status, message });
  }

  async start() {
    if (this.#status === 'starting' || this.#status === 'ready') {
      return;
    }

    this.#setStatus('starting', 'Launching Asher.Host...');

    try {
      this.#hostPath = resolveHostPath();
    } catch (err) {
      this.#setStatus('error', err.message);
      throw err;
    }

    return new Promise((resolve, reject) => {
      const child = spawn(this.#hostPath, ['--jsonl'], {
        stdio: ['pipe', 'pipe', 'pipe'],
        windowsHide: true
      });

      this.#process = child;
      const client = new JsonlClient();
      this.#client = client;

      child.stderr.on('data', (chunk) => {
        const text = chunk.toString('utf8').trim();
        if (text) {
          console.error(`[asher-host] ${text}`);
        }
      });

      client.attach(child.stdin, child.stdout);

      const readyTimeout = setTimeout(() => {
        this.#setStatus('error', 'Host did not emit ready event within timeout');
        this.stop({ force: true });
        reject(new Error('Host ready timeout'));
      }, READY_TIMEOUT_MS);

      client.once('ready', () => {
        clearTimeout(readyTimeout);
        this.#setStatus('ready', 'Connected to Asher.Host');
        resolve();
      });

      client.once('closed', () => {
        if (this.#status !== 'stopped') {
          this.#setStatus('terminated', 'Host process exited unexpectedly');
        }
      });

      child.on('error', (err) => {
        clearTimeout(readyTimeout);
        this.#setStatus('error', `Failed to start host: ${err.message}`);
        reject(err);
      });

      child.on('exit', (code, signal) => {
        clearTimeout(readyTimeout);
        if (this.#status !== 'stopped') {
          const detail = signal ? `signal ${signal}` : `exit code ${code}`;
          this.#setStatus('terminated', `Host process ended (${detail})`);
        }
        this.#process = null;
        this.#client = null;
      });
    });
  }

  /**
   * @param {{ force?: boolean }} [options]
   */
  async stop(options = {}) {
    const { force = false } = options;

    if (!this.#process) {
      this.#setStatus('stopped');
      return;
    }

    if (!force && this.#client?.isReady) {
      try {
        await this.#client.shutdown();
      } catch {
        // Continue with process termination.
      }
    }

    if (this.#process.stdin && !this.#process.stdin.destroyed) {
      this.#process.stdin.end();
    }

    await new Promise((resolve) => {
      const proc = this.#process;
      if (!proc) {
        resolve();
        return;
      }

      const killTimer = setTimeout(() => {
        if (!proc.killed) {
          proc.kill();
        }
      }, 5000);

      proc.once('exit', () => {
        clearTimeout(killTimer);
        resolve();
      });
    });

    this.#process = null;
    this.#client = null;
    this.#setStatus('stopped');
  }
}
