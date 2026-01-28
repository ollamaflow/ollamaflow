import * as sdk from '../src';
export const mockEndpoint = 'http://localhost:8701/'; //endpoint
export const api = new sdk.OllamaflowSdk(
  { endpoint: mockEndpoint } //endpoint
);

export { sdk };
