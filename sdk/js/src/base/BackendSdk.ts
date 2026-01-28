import { Backend, BackendCreateRequest, BackendHealth, SdkConfig } from '../types';
import SdkBase from './SdkBase';
import { SdkConfiguration } from './SdkConfiguration';
import GenericExceptionHandlers from '../exception/GenericExceptionHandlers';

export default class BackendSdk extends SdkBase {
  /**
   * Instantiate the SDK.
   * @param {SdkConfiguration} config - The configuration object.
   */
  constructor(config: SdkConfiguration) {
    super(config);
  }

  /**
   * Retrieve a list of all backends.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Array<Backend>>} Resolves with the list of backends.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async readAll(cancellationToken?: AbortController | undefined): Promise<Array<Backend>> {
    const url = `${this.config.endpoint}v1.0/backends`;
    return this.get<Array<Backend>>(url, cancellationToken);
  }

  /**
   * Retrieve a single backend by identifier.
   * @param {string} id - The identifier of the backend to retrieve.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Backend>} Resolves with the backend.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async read(id: string, cancellationToken?: AbortController | undefined): Promise<Backend> {
    if (!id) {
      GenericExceptionHandlers.ArgumentNullException('id');
    }
    const url = `${this.config.endpoint}v1.0/backends/${id}`;
    return this.get<Backend>(url, cancellationToken);
  }

  /**
   * Check if a backend exists by identifier.
   * @param {string} id - The identifier of the backend to check.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<boolean>} Resolves to true if the backend exists.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async exist(id: string, cancellationToken?: AbortController | undefined): Promise<boolean> {
    if (!id) {
      GenericExceptionHandlers.ArgumentNullException('id');
    }
    const url = `${this.config.endpoint}v1.0/backends/${id}`;
    return this.head(url, cancellationToken);
  }

  /**
   * Delete a backend by identifier.
   * @param {string} id - The identifier of the backend to delete.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<boolean>} Resolves to true if the backend is successfully deleted.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async delete(id: string, cancellationToken?: AbortController | undefined): Promise<boolean> {
    if (!id) {
      GenericExceptionHandlers.ArgumentNullException('id');
    }
    const url = `${this.config.endpoint}v1.0/backends/${id}`;
    return this.del(url, undefined, cancellationToken);
  }

  /**
   * Create a new backend.
   * @param {BackendCreateRequest} backend - The backend object to create.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Backend>} Resolves with the created backend.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async create(backend: BackendCreateRequest, cancellationToken?: AbortController | undefined): Promise<Backend> {
    if (!backend) {
      GenericExceptionHandlers.ArgumentNullException('backend');
    }
    const url = `${this.config.endpoint}v1.0/backends`;
    return this.put<Backend>(url, backend, cancellationToken);
  }

  /**
   * Update an existing backend by identifier.
   * @param {Backend} backend - The backend object with updated values.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Backend>} Resolves with the updated backend.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async update(backend: Backend, cancellationToken?: AbortController | undefined): Promise<Backend> {
    if (!backend.Identifier) {
      GenericExceptionHandlers.ArgumentNullException('backend.Identifier');
    }
    const url = `${this.config.endpoint}v1.0/backends/${backend.Identifier}`;
    return this.put<Backend>(url, backend, cancellationToken);
  }

  /**
   * Retrieve health information for all backends.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Array<BackendHealth>>} Resolves with an array of backends and their health information.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async readAllHealth(cancellationToken?: AbortController | undefined): Promise<Array<BackendHealth>> {
    const url = `${this.config.endpoint}v1.0/backends/health`;
    return this.get<Array<BackendHealth>>(url, cancellationToken);
  }

  /**
   * Retrieve health information for a specific backend by identifier.
   * @param {string} id - The identifier of the backend to retrieve health information for.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<BackendHealth>} Resolves with the backend health information.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async readHealth(id: string, cancellationToken?: AbortController | undefined): Promise<BackendHealth> {
    if (!id) {
      GenericExceptionHandlers.ArgumentNullException('id');
    }
    const url = `${this.config.endpoint}v1.0/backends/${id}`;
    return this.get<BackendHealth>(url, cancellationToken);
  }
}
