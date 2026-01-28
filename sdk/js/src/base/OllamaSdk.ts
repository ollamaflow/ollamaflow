import { SdkConfiguration } from './SdkConfiguration';
import SdkBase from './SdkBase';
import GenericExceptionHandlers from '../exception/GenericExceptionHandlers';
import { GenerateChatCompletionResponse, GenerateCompletionResponse, ModelListResponse } from '../types';
/**
 * Ollama SDK class.
 * Extends the SdkBase class.
 * @module  OllamaSdk
 * @extends SdkBase
 */
export default class OllamaSdk extends SdkBase {
  /**
   * Instantiate the SDK.
   * @param {SdkConfiguration} config - The configuration object.
   */
  constructor(config: SdkConfiguration) {
    super(config);
  }

  /**
   * Generate a completion using the Ollama /api/generate endpoint.
   * @param {object} request - The parameters for the generation. Should match Ollama /api/generate payload.
   * @param {function(string): void} [onToken] - Optional callback to handle tokens as they are emitted.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<GenerateCompletionResponse>} Resolves with the completion response.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async generateCompletion(
    request: any,
    onToken?: (token: string) => void,
    cancellationToken?: AbortController | undefined
  ): Promise<GenerateCompletionResponse> {
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('request');
    }
    const url = `${this.config.endpoint}api/generate`;
    return this.post<GenerateCompletionResponse>(url, request, cancellationToken, onToken);
  }
  /**
   * Generate a chat completion using the OpenAI chat completions API.
   * @param {object} request - The parameters for the generation. Should match OpenAI /v1/chat/completions schema.
   * @param {function(string): void} [onToken] - Optional callback to handle tokens as they are emitted.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<GenerateChatCompletionResponse>} Resolves with the chat completion response.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async generateChatCompletion(
    request: any,
    onToken?: (token: string) => void,
    cancellationToken?: AbortController | undefined
  ): Promise<GenerateChatCompletionResponse> {
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('request');
    }
    const url = `${this.config.endpoint}api/chat`;
    return this.post<GenerateChatCompletionResponse>(url, request, cancellationToken, onToken);
  }

  /**
   * Pull a model using the Ollama /api/pull endpoint.
   * @param {{name:string}} request - The parameters for pulling the model. Should match Ollama /api/pull payload.
   * @param {function(string): void} [onToken] - Optional callback to handle tokens as they are emitted.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<object>} Resolves with the pull model response.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async pullModel(
    request: { name: string },
    onToken?: (token: string) => void,
    cancellationToken?: AbortController | undefined
  ): Promise<any> {
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('request');
    }
    const url = `${this.config.endpoint}api/pull`;
    return this.post(url, request, cancellationToken, onToken);
  }
  /**
   * Retrieve model information using the Ollama /api/show endpoint.
   * @param {{name:string}} request - The parameters for model information. Should match Ollama /api/show payload.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<object>} Resolves with the model information response.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async modelInformation(request: { name: string }, cancellationToken?: AbortController | undefined): Promise<any> {
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('request');
    }
    const url = `${this.config.endpoint}api/show`;
    return this.post(url, request, cancellationToken);
  }
  /**
   * Delete a model using the Ollama /api/delete endpoint.
   * @param {{name:string}} request - The parameters for deleting the model. Should match Ollama /api/delete payload.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<boolean>} Resolves with the delete model response.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async deleteModel(request: { name: string }, cancellationToken?: AbortController | undefined): Promise<boolean> {
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('request');
    }
    const url = `${this.config.endpoint}api/delete`;
    return this.del(url, request, cancellationToken);
  }
  /**
   * List local models using the Ollama /api/tags endpoint.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<ModelListResponse>} Resolves with the list of local models.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async listLocalModel(cancellationToken?: AbortController | undefined): Promise<ModelListResponse> {
    const url = `${this.config.endpoint}api/tags`;
    return this.get<ModelListResponse>(url, cancellationToken);
  }

  /**
   * List running models using the Ollama /api/ps endpoint.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<ModelListResponse>} Resolves with the response containing the list of running models.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async listRunningModel(cancellationToken?: AbortController | undefined): Promise<ModelListResponse> {
    const url = `${this.config.endpoint}api/ps`;
    return this.get<ModelListResponse>(url, cancellationToken);
  }

  /**
   * Generate embeddings using the Ollama /api/embed endpoint.
   * @param {{model: string, input: string | string[]}} request - The embedding request. Should match Ollama /api/embed payload.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<any>} Resolves with the embeddings response.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async generateEmbeddings(
    request: { model: string; input: string | string[] },
    cancellationToken?: AbortController | undefined
  ): Promise<any> {
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('request');
    }
    const url = `${this.config.endpoint}api/embed`;
    return this.post(url, request, cancellationToken);
  }
}
