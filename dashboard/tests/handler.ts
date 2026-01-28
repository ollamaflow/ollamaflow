import { ollamaServerUrl } from "#/constants/apiConfig";
import {
  mockBackendData,
  mockFrontendData,
  mockBackendHealthData,
  mockBackendIdentifier,
  mockFrontendIdentifier,
} from "./mockData";
import { http, HttpResponse } from "msw";

export const commonHandlers = [
  // Authentication endpoints
  http.get(`${ollamaServerUrl}/`, () => {
    return HttpResponse.json({ status: "ok" });
  }),
  http.get(`${ollamaServerUrl}/v1.0/frontends`, () => {
    return HttpResponse.json([mockFrontendData]);
  }),

  //get backends
  http.get(`${ollamaServerUrl}/v1.0/backends`, () => {
    return HttpResponse.json([mockBackendData]);
  }),
  http.get(`${ollamaServerUrl}/v1.0/backends/${mockBackendIdentifier}`, () => {
    return HttpResponse.json(mockBackendData);
  }),
  http.get(`${ollamaServerUrl}/v1.0/backends/health`, () => {
    return HttpResponse.json([mockBackendHealthData]);
  }),
  http.put(`${ollamaServerUrl}/v1.0/backends`, () => {
    return HttpResponse.json(mockBackendData);
  }),
  http.put(`${ollamaServerUrl}/v1.0/backends/${mockBackendIdentifier}`, () => {
    return HttpResponse.json(mockBackendData);
  }),
  http.delete(
    `${ollamaServerUrl}/v1.0/backends/${mockBackendIdentifier}`,
    () => {
      return HttpResponse.json(mockBackendData);
    }
  ),

  //get frontends
  http.get(`${ollamaServerUrl}/v1.0/frontends`, () => {
    return HttpResponse.json([mockFrontendData]);
  }),
  http.get(
    `${ollamaServerUrl}/v1.0/frontends/${mockFrontendIdentifier}`,
    () => {
      return HttpResponse.json(mockFrontendData);
    }
  ),
  http.put(`${ollamaServerUrl}/v1.0/frontends`, () => {
    return HttpResponse.json(mockFrontendData);
  }),
  http.put(
    `${ollamaServerUrl}/v1.0/frontends/${mockFrontendIdentifier}`,
    () => {
      return HttpResponse.json(mockFrontendData);
    }
  ),
  http.delete(
    `${ollamaServerUrl}/v1.0/frontends/${mockFrontendIdentifier}`,
    () => {
      return HttpResponse.json(mockFrontendData);
    }
  ),
];
