import { SdkConfiguration } from './SdkConfiguration';
import { SdkConfig } from '../types';
import SdkBase from './SdkBase';
import OpenAISdk from './OpenAISdk';
import OllamaSdk from './OllamaSdk';
import FrontendSdk from './FrontendSdk';
import BackendSdk from './BackendSdk';
/**
 * Ollamaflow SDK class.
 * Extends the SdkBase class.
 * @module  OllamaflowSdk
 * @extends SdkBase
 */
export default class OllamaflowSdk extends SdkBase {
  public config: SdkConfiguration;
  public OpenAI: OpenAISdk;
  public Ollama: OllamaSdk;
  public Frontend: FrontendSdk;
  public Backend: BackendSdk;
  /**
   * Instantiate the SDK.
   * @param {SdkConfig} sdkConfig - The SDK configuration.
   */

  constructor(sdkConfig: SdkConfig) {
    const config = new SdkConfiguration(sdkConfig);
    super(config);
    this.config = config;
    this.OpenAI = new OpenAISdk(config);
    this.Ollama = new OllamaSdk(config);
    this.Frontend = new FrontendSdk(config);
    this.Backend = new BackendSdk(config);
  }

  /**
   * Validates API connectivity using a HEAD request.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<boolean>} Resolves to true if the connection is successful.
   * @throws {Error} Rejects with the error in case of failure.
   */
  /**
   * Validates API connectivity using a HEAD request.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @return {Promise<boolean>} Resolves to true if the connection is successful.
   * @throws {Error} Rejects with the error in case of failure.
   */
  validateConnectivity(cancellationToken?: AbortController | undefined): Promise<boolean> {
    return this.head(this.config.endpoint, cancellationToken);
  }
}
