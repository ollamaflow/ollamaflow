import { BaseQueryFn, EndpointBuilder } from "@reduxjs/toolkit/query";
import apiSlice, { ApiBaseQueryArgs } from "../rtk/rtkApiInstance";
import { Frontend, Backend, BackendHealth } from "./types";

// Type for creating a frontend (excludes server-generated fields)
export type CreateFrontendPayload = Omit<
  Frontend,
  "Active" | "CreatedUtc" | "LastUpdateUtc" | "Identifier"
>;

// Type for creating a backend (excludes server-generated fields)
export type CreateBackendPayload = Omit<
  Backend,
  "Active" | "CreatedUtc" | "LastUpdateUtc"
>;

// Type for editing a frontend (includes identifier for targeting)
export type EditFrontendPayload = CreateFrontendPayload & {
  Identifier: string;
};

// Type for editing a backend (includes identifier for targeting)
export type EditBackendPayload = CreateBackendPayload & {
  Identifier: string;
};

export enum SliceTags {
  FRONTEND = "FRONTEND",
  BACKEND = "BACKEND",
}

const enhancedApiSlice = apiSlice.enhanceEndpoints({
  addTagTypes: [SliceTags.FRONTEND, SliceTags.BACKEND],
});

const apiPathSlice = enhancedApiSlice.injectEndpoints({
  endpoints: (
    builder: EndpointBuilder<
      BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>,
      SliceTags,
      "api"
    >
  ) => ({
    validateConnectivity: builder.mutation<boolean, void>({
      query: () => ({
        url: "/",
        method: "GET",
      }),
      transformResponse: (response: any) => {
        console.log(response);
        return response ? true : false;
      },
    }),
    getFrontendTest: builder.mutation<boolean, void>({
      query: () => ({
        url: "v1.0/frontends",
      }),
      transformResponse: (response: Frontend[]) => {
        console.log(response);
        return response.length > 0;
      },
    }),
    getFrontend: builder.query<Frontend[], void>({
      query: (arg: any) => ({
        url: "v1.0/frontends",
      }),
      providesTags: [{ type: SliceTags.FRONTEND, id: "LIST" }],
      transformResponse: (response: any) => response,
    }),
    getFrontendById: builder.query<Frontend, string>({
      query: (identifier: string) => ({
        url: `v1.0/frontends/${identifier}`,
      }),
      providesTags: [{ type: SliceTags.FRONTEND, id: "LIST" }],
      transformResponse: (response: any) => response,
    }),
    getBackend: builder.query<Backend[], void>({
      query: (arg: any) => ({
        url: "v1.0/backends",
      }),
      providesTags: [{ type: SliceTags.BACKEND, id: "LIST" }],
      transformResponse: (response: any) => response,
    }),
    getBackendHealth: builder.query<BackendHealth[], void>({
      query: (arg: any) => ({
        url: "v1.0/backends/health",
      }),
      providesTags: [{ type: SliceTags.BACKEND, id: "LIST" }],
      transformResponse: (response: any) => response,
    }),
    getBackendById: builder.query<Backend, string>({
      query: (identifier: string) => ({
        url: `v1.0/backends/${identifier}`,
      }),
      providesTags: [{ type: SliceTags.BACKEND, id: "LIST" }],
      transformResponse: (response: any) => response,
    }),
    createFrontend: builder.mutation<Frontend, CreateFrontendPayload>({
      query: (frontendData) => ({
        url: "v1.0/frontends",
        method: "PUT",
        data: frontendData,
      }),
      invalidatesTags: [
        { type: SliceTags.FRONTEND, id: "LIST" },
        SliceTags.FRONTEND,
      ],
      transformResponse: (response: any) => response,
    }),
    createBackend: builder.mutation<Backend, CreateBackendPayload>({
      query: (backendData) => ({
        url: "v1.0/backends",
        method: "PUT",
        data: backendData,
      }),
      invalidatesTags: [
        { type: SliceTags.BACKEND, id: "LIST" },
        SliceTags.BACKEND,
      ],
      transformResponse: (response: any) => response,
    }),
    editFrontend: builder.mutation<Frontend, EditFrontendPayload>({
      query: ({ Identifier, ...frontendData }) => ({
        url: `v1.0/frontends/${Identifier}`,
        method: "PUT",
        data: frontendData,
      }),
      invalidatesTags: (result, error, { Identifier }) => [
        { type: SliceTags.FRONTEND, id: "LIST" },
        { type: SliceTags.FRONTEND, id: Identifier },
        SliceTags.FRONTEND,
      ],
      transformResponse: (response: any) => response,
    }),
    deleteFrontend: builder.mutation<void, string>({
      query: (identifier) => ({
        url: `v1.0/frontends/${identifier}`,
        method: "DELETE",
      }),
      invalidatesTags: (result, error, identifier) => [
        { type: SliceTags.FRONTEND, id: "LIST" },
        { type: SliceTags.FRONTEND, id: identifier },
        SliceTags.FRONTEND,
      ],
    }),
    editBackend: builder.mutation<Backend, EditBackendPayload>({
      query: ({ Identifier, ...backendData }) => ({
        url: `v1.0/backends/${Identifier}`,
        method: "PUT",
        data: backendData,
      }),
      invalidatesTags: (result, error, { Identifier }) => [
        { type: SliceTags.BACKEND, id: "LIST" },
        { type: SliceTags.BACKEND, id: Identifier },
        SliceTags.BACKEND,
      ],
      transformResponse: (response: any) => response,
    }),
    deleteBackend: builder.mutation<void, string>({
      query: (identifier) => ({
        url: `v1.0/backends/${identifier}`,
        method: "DELETE",
      }),
      invalidatesTags: (result, error, identifier) => [
        { type: SliceTags.BACKEND, id: "LIST" },
        { type: SliceTags.BACKEND, id: identifier },
        SliceTags.BACKEND,
      ],
    }),
  }),
});

export const {
  useGetFrontendQuery,
  useValidateConnectivityMutation,
  useGetFrontendTestMutation,
  useGetBackendQuery,
  useCreateFrontendMutation,
  useCreateBackendMutation,
  useEditFrontendMutation,
  useDeleteFrontendMutation,
  useEditBackendMutation,
  useDeleteBackendMutation,
  useGetFrontendByIdQuery,
  useGetBackendByIdQuery,
  useGetBackendHealthQuery,
} = apiPathSlice;
