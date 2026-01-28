import { SeverityEnum } from '../enums/SeverityEnum';
import GenericExceptionHandlers from '../exception/GenericExceptionHandlers';
import { Credentials, SdkConfig } from '../types';
import Utils from '../utils/Utils';

export class SdkConfiguration {
  private _accountGuid: string | undefined;
  private _bearerToken: string | undefined;
  private _basicAuth: Credentials | undefined;
  private _endpoint: string;
  private _timeoutMs: number;
  public logLevel?: SeverityEnum;
  public defaultHeaders: any;
  /**
   * Instantiate the SDK.
   * @param {SdkConfig} sdkConfig - The configuration object.
   */
  constructor(sdkConfig: SdkConfig) {
    const { endpoint, bearerToken } = sdkConfig;
    if (!endpoint) {
      GenericExceptionHandlers.ArgumentNullException('Endpoint');
    }
    this.defaultHeaders = {};
    if (bearerToken) {
      this._bearerToken = bearerToken;

      this.defaultHeaders = {
        Authorization: Utils.createBearerAuthHeader(bearerToken),
      };
    }

    this._endpoint = endpoint.endsWith('/') ? endpoint : endpoint + '/';
    this._timeoutMs = 300000;
  }

  /**
   * Getter for the account GUID.
   * @return {string} The account GUID.
   */
  get accountGuid(): string | undefined {
    if (!this._accountGuid) {
      GenericExceptionHandlers.ArgumentNullException('Account GUID');
    }
    return this._accountGuid;
  }

  /**
   * Setter for the account GUID.
   * @param {string} accountGuid - The account GUID.
   * @throws {Error} Throws an error if the account GUID is null or empty.
   */
  set accountGuid(accountGuid: string) {
    if (!accountGuid) {
      GenericExceptionHandlers.ArgumentNullException('Account GUID');
    }
    this._accountGuid = accountGuid;
  }

  /**
   * Getter for the basic auth credentials.
   * @return {object} The basic auth credentials.
   */
  get basicAuth(): Credentials | undefined {
    return this._basicAuth;
  }

  /**
   * Setter for the basic auth credentials.
   * @param {object} basicAuth - The basic auth credentials.
   * @param {string} basicAuth.username - The username.
   * @param {string} basicAuth.password - The password.
   * @throws {Error} Throws an error if the basic auth credentials are null or empty.
   */
  set basicAuth(basicAuth: Credentials) {
    if (!basicAuth) {
      GenericExceptionHandlers.ArgumentNullException('BasicAuth');
    }
    this._basicAuth = basicAuth;
    this.defaultHeaders = {
      Authorization: Utils.createBasicAuthHeader(basicAuth.email, basicAuth.password),
    };
  }

  /**
   * Getter for the access key.
   * @return {string} The access key.
   */
  get bearerToken(): string | undefined {
    return this._bearerToken;
  }

  /**
   * Setter for the access key.
   * @param {string} token - The access key.
   * @throws {Error} Throws an error if the access key is null or empty.
   */
  set bearerToken(token: string) {
    if (!token) {
      GenericExceptionHandlers.ArgumentNullException('BearerToken');
    }
    this._bearerToken = token;
    this.defaultHeaders = {
      ...this.defaultHeaders,
      Authorization: Utils.createBearerAuthHeader(token),
    };
  }

  /**
   * Getter for the API endpoint.
   * @return {string} The endpoint URL.
   */
  get endpoint(): string {
    return this._endpoint;
  }

  /**
   * Setter for the API endpoint.
   * @param {string} value - The endpoint URL.
   * @throws {Error} Throws an error if the endpoint is null or empty.
   */
  set endpoint(value: string) {
    if (!value) {
      GenericExceptionHandlers.ArgumentNullException('Endpoint');
    }
    this._endpoint = value.endsWith('/') ? value : value + '/';
  }

  /**
   * Getter for the timeout in milliseconds.
   * @return {number} The timeout in milliseconds.
   */
  get timeoutMs(): number {
    return this._timeoutMs;
  }

  /**
   * Setter for the timeout in milliseconds.
   * @param {number} value - Timeout value in milliseconds.
   * @throws {Error} Throws an error if the timeout is less than 1.
   */
  set timeoutMs(value: number) {
    if (value < 1) {
      GenericExceptionHandlers.GenericException('TimeoutMs must be greater than 0.');
    }
    this._timeoutMs = value;
  }
}
