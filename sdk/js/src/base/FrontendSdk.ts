import { Frontend, FrontendCreateRequest, SdkConfig } from '../types';
import SdkBase from './SdkBase';
import { SdkConfiguration } from './SdkConfiguration';
import GenericExceptionHandlers from '../exception/GenericExceptionHandlers';

export default class FrontendSdk extends SdkBase {
  /**
   * Instantiate the SDK.
   * @param {SdkConfiguration} config - The configuration object.
   */
  constructor(config: SdkConfiguration) {
    super(config);
  }

  /**
   * Retrieve a list of all frontends.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Array<Frontend>>} Resolves with the list of frontends.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async readAll(cancellationToken?: AbortController | undefined): Promise<Array<Frontend>> {
    const url = `${this.config.endpoint}v1.0/frontends`;
    return this.get<Array<Frontend>>(url, cancellationToken);
  }

  /**
   * Retrieve a single frontend by identifier.
   * @param {string} id - The identifier of the frontend to retrieve.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Frontend>} Resolves with the frontend.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async read(id: string, cancellationToken?: AbortController | undefined): Promise<Frontend> {
    if (!id) {
      GenericExceptionHandlers.ArgumentNullException('id');
    }
    const url = `${this.config.endpoint}v1.0/frontends/${id}`;
    return this.get<Frontend>(url, cancellationToken);
  }

  /**
   * Check if a frontend exists by identifier.
   * @param {string} id - The identifier of the frontend to check.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<boolean>} Resolves to true if the frontend exists.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async exist(id: string, cancellationToken?: AbortController | undefined): Promise<boolean> {
    if (!id) {
      GenericExceptionHandlers.ArgumentNullException('id');
    }
    const url = `${this.config.endpoint}v1.0/frontends/${id}`;
    return this.head(url, cancellationToken);
  }

  /**
   * Delete a frontend by identifier.
   * @param {string} id - The identifier of the frontend to delete.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<boolean>} Resolves to true if the frontend is successfully deleted.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async delete(id: string, cancellationToken?: AbortController | undefined): Promise<boolean> {
    if (!id) {
      GenericExceptionHandlers.ArgumentNullException('id');
    }
    const url = `${this.config.endpoint}v1.0/frontends/${id}`;
    return this.del(url, undefined, cancellationToken);
  }

  /**
   * Create a new frontend.
   * @param {FrontendCreateRequest} frontend - The frontend object to create.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Frontend>} Resolves with the created frontend.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async create(frontend: FrontendCreateRequest, cancellationToken?: AbortController | undefined): Promise<Frontend> {
    if (!frontend) {
      GenericExceptionHandlers.ArgumentNullException('frontend');
    }
    const url = `${this.config.endpoint}v1.0/frontends`;
    return this.put<Frontend>(url, frontend, cancellationToken);
  }

  /**
   * Update an existing frontend by identifier.
   * @param {Frontend} frontend - The frontend object with updated values.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Frontend>} Resolves with the updated frontend.
   * @throws {Error} Rejects with the error in case of failure.
   */
  async update(frontend: Frontend, cancellationToken?: AbortController | undefined): Promise<Frontend> {
    if (!frontend.Identifier) {
      GenericExceptionHandlers.ArgumentNullException('frontend.Identifier');
    }
    const url = `${this.config.endpoint}v1.0/frontends/${frontend.Identifier}`;
    return this.put<Frontend>(url, frontend, cancellationToken);
  }
}
