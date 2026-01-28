import { Writable } from 'stream';

export type OnToken = (token: string) => void;

export default class Stream {
  /**
   * Create a writable stream to parse SSE data.
   * @private
   * @param {OnToken} onToken - Callback to handle tokens as they are emitted.
   * @returns {Writable} - A writable stream for parsing.
   */
  static createStreamParser(onToken: OnToken): Writable {
    return new Writable({
      write: (chunk: any, encoding: any, callback: any) => {
        const dataString = chunk.toString();
        const lines = dataString.split('\n');
        lines.forEach((line: any) => {
          const jsonString = line.trim();
          onToken(jsonString);
        });
        callback();
      },
      final(callback: any) {
        callback(); // Ensure stream is finalized properly
      },
    });
  }

  /**
   * Extract a token from JSON string.
   * @private
   * @param {string} json - The JSON string.
   * @returns {string|null} - The extracted token or null if not found.
   */
  static extractToken(json: any) {
    try {
      const obj = JSON.parse(json);
      return obj.token || null;
    } catch (_error) {
      return null;
    }
  }
}
