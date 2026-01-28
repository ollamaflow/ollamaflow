import { SdkConfiguration } from './SdkConfiguration';
import SdkBase from './SdkBase';
import GenericExceptionHandlers from '../exception/GenericExceptionHandlers';
/**
 * OpenAI SDK class.
 * Extends the SdkBase class.
 * @module  OpenAISdk
 * @extends SdkBase
 */
export default class OpenAISdk extends SdkBase {
  /**
   * Instantiate the SDK.
   * @param {SdkConfiguration} config - The configuration object.
   */
  constructor(config: SdkConfiguration) {
    super(config);
  }

  /**
   * Generate a completion using the OpenAI completions API.
   * @param {object} request - The parameters for the generation. Should match OpenAI /v1/completions schema.
   * @param {function(string): void} [onToken] - Optional callback to handle tokens as they are emitted.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<object>} Resolves with the completion response.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async generateCompletion(
    request: any,
    onToken?: (token: string) => void,
    cancellationToken?: AbortController | undefined
  ): Promise<any> {
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('request');
    }
    const url = `${this.config.endpoint}v1/completions`;
    return this.post(url, request, cancellationToken, onToken);
  }

  /**
   * Generate a chat completion using the OpenAI chat completions API.
   * @param {object} request - The parameters for the generation. Should match OpenAI /v1/chat/completions schema.
   * @param {function(string): void} [onToken] - Optional callback to handle tokens as they are emitted.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<object>} Resolves with the chat completion response.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async generateChatCompletion(
    request: any,
    onToken?: (token: string) => void,
    cancellationToken?: AbortController | undefined
  ): Promise<any> {
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('request');
    }
    const url = `${this.config.endpoint}v1/chat/completions`;
    return this.post(url, request, cancellationToken, onToken);
  }

  /**
   * Generate embeddings using the OpenAI embeddings API.
   * @param {object} request - The parameters for the generation. Should match OpenAI /v1/embeddings schema.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<object>} Resolves with the embeddings response.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async generateEmbeddings(request: any, cancellationToken?: AbortController | undefined): Promise<any> {
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('request');
    }
    const url = `${this.config.endpoint}v1/embeddings`;
    return this.post(url, request, cancellationToken);
  }
}
