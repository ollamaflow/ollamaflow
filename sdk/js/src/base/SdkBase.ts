import superagent from 'superagent';
import { SeverityEnum } from '../enums/SeverityEnum';
import GenericExceptionHandlers from '../exception/GenericExceptionHandlers';
import Serializer from '../utils/Serializer';
import { SdkConfiguration } from './SdkConfiguration';
import Logger from '../utils/Logger';
import Stream from '../utils/Stream';

/**
 * SDK Base class for making API calls with logging and timeout functionality.
 * @module SdkBase
 */
export default class SdkBase {
  private logger: Logger;
  protected config: SdkConfiguration;
  /**
   * Creates an instance of SdkBase.
   * @param {SdkConfiguration} config - The SDK configuration.
   * @throws {Error} Throws an error if the config is null.
   */
  constructor(config: SdkConfiguration) {
    if (!config) {
      GenericExceptionHandlers.ArgumentNullException('config');
    }
    this.config = config;
    this.logger = new Logger(this.config);
  }

  /**
   * Logs a message with a severity level.
   * @param {string} sev - The severity level (e.g., SeverityEnum.Debug, 'warn').
   * @param {string} msg - The message to log.
   */
  protected log(sev: SeverityEnum, msg: string) {
    if (!msg) return;
    this.logger.log(sev, msg);
  }
  /**
   * Sends a PUT request to create an object at a given URL.
   * @param {string} url - The URL where the object is created.
   * @param {Object} obj - The object to be created.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<Object>} Resolves with the created object.
   * @throws {Error} Rejects if the URL or object is invalid or if the request fails.
   */
  protected put<T>(url: string, obj: any, cancellationToken?: AbortController | undefined): Promise<T> {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));
      const request = superagent
        .put(url)
        .set(this.config.defaultHeaders)
        .set('Content-Type', 'application/json')
        .timeout({ response: this.config.timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (obj) {
        request.send(obj);
      }
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(Serializer.deserializeJson(res.text) as T);
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          if (err?.response?.text) {
            const errorResponse = Serializer.deserializeJson(err?.response?.text);
            reject(errorResponse);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Checks if an object exists at a given URL using a HEAD request.
   * @param {string} url - The URL to check.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<boolean>} Resolves to true if the object exists.
   * @throws {Error} Rejects if the URL is invalid or if the request fails.
   */
  protected head(url: string, cancellationToken?: AbortController | undefined): Promise<boolean> {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent.head(url).set(this.config.defaultHeaders).timeout({ response: this.config.timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(res.ok);
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.error) {
            reject(errorResponse?.error);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Retrieves an object from a given URL using a GET request.
   * @param {string} url - The URL of the object.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @param {Object} [headers] - Additional headers.
   * @return {Promise<Object>} Resolves with the retrieved object.
   * @throws {Error} Rejects if the URL is invalid or if the request fails.
   */
  protected get<T>(url: string, cancellationToken?: AbortController | undefined, headers?: object): Promise<T> {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent
        .get(url)
        .set({ ...this.config.defaultHeaders, ...(headers || {}) })
        .timeout({ response: this.config.timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(Serializer.deserializeJson(res.text) as T);
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.error) {
            reject(errorResponse.error);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Sends a DELETE request to remove an object at a given URL.
   * @param {string} url - The URL of the object to delete.
   * @param {any} [data] - The data to send in the DELETE request.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<true>} Resolves if the object is successfully deleted.
   * @throws {Error} Rejects if the URL is invalid or if the request fails.
   */
  protected del(url: string, data?: any, cancellationToken?: AbortController | undefined): Promise<boolean> {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent
        .delete(url)
        .set(this.config.defaultHeaders)
        .timeout({ response: this.config.timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      if (data) {
        request.send(data);
      }
      request
        .then((res) => {
          this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
          resolve(true);
        })
        .catch((err) => {
          this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
          const errorResponse = err?.response?.body || null;
          if (errorResponse && errorResponse?.error) {
            reject(errorResponse.error);
          } else {
            reject(err.message ? err.message : err);
          }
        });
    });
  }

  /**
   * Submits data using a POST request to a given URL.
   * @param {string} url - The URL to post data to.
   * @param {Object|string} data - The data to send in the POST request.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @param {OnToken} [onToken] - Callback to handle tokens as they are emitted.
   * @return {Promise<Object>} Resolves with the response data.
   * @throws {Error|ApiErrorResponse} Rejects if the URL or data is invalid or if the request fails.
   */
  protected post<T>(
    url: string,
    data: any,
    cancellationToken?: AbortController | undefined,
    onToken?: (token: string) => void
  ): Promise<T> {
    return new Promise((resolve, reject) => {
      if (!url) return reject(new Error('URL cannot be null or empty.'));

      const request = superagent
        .post(url)
        .set(this.config.defaultHeaders)
        .set('Content-Type', 'application/json')
        .send(data)
        .timeout({ response: this.config.timeoutMs });
      // If a cancelToken is provided, attach the abort method
      if (cancellationToken) {
        cancellationToken.abort = () => {
          request.abort();
          this.log(SeverityEnum.Debug, `Request aborted to ${url}.`);
        };
      }
      if (onToken) {
        request.pipe(Stream.createStreamParser(onToken));
      } else {
        request
          .then((res) => {
            this.log(SeverityEnum.Debug, `Success reported from ${url}: ${res.status}`);
            resolve(Serializer.deserializeJson(res.text) as T);
          })
          .catch((err) => {
            this.log(SeverityEnum.Warn, `Failed to retrieve object from ${url}: ${err.message}`);
            const errorResponse = err?.response?.body || null;
            if (errorResponse && errorResponse?.error) {
              reject(errorResponse?.error);
            } else {
              reject(err.message ? err.message : err);
            }
          });
      }
    });
  }
}
