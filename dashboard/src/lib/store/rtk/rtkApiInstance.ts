import { createApi } from "@reduxjs/toolkit/query/react";

import { ollamaServerUrl } from "#/constants/apiConfig";
import { keepUnusedDataFor } from "#/constants/constant";
import axios from "axios";

export const axiosInstance = axios.create({
  baseURL: ollamaServerUrl,
});

export const changeAxiosBaseUrl = (url: string) => {
  axiosInstance.defaults.baseURL = url;
};

export const setAuthToken = (token: string) => {
  axiosInstance.defaults.headers.common["Authorization"] = `Bearer ${token}`;
};

export interface ApiBaseQueryArgs {
  url: string;
  method?: string;
  data?: any;
  headers?: any;
}

export const axiosBaseQuery = async ({
  url,
  method,
  data,
  headers,
}: ApiBaseQueryArgs) => {
  try {
    const response = await axiosInstance({
      url,
      method: method || "GET",
      data,
      headers,
    });
    return { data: response.data };
  } catch (error: any) {
    return { error: error?.response?.data || error };
  }
};

const apiSlice = createApi({
  reducerPath: "api",
  baseQuery: axiosBaseQuery,
  tagTypes: [],
  endpoints: () => ({}),
  keepUnusedDataFor: keepUnusedDataFor,
});

export default apiSlice;
