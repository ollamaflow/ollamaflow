import {
  isRejectedWithValue,
  Middleware,
  MiddlewareAPI,
} from "@reduxjs/toolkit";
import { isNumber } from "lodash";
import { message } from "antd";
import { handleLogout } from "./rootReducer";

export const errorHandler = (er: any) => {
  const error = er?.payload || {};
  const endpointName = er?.meta?.arg?.endpointName;
  const serverErrorMessage = error?.Message ||  error?.data?.message || error?.data?.detail;
  console.log(error, "chk endpointName");
  if (isNumber(error?.status) || isNumber(error?.StatusCode) || error?.code) {
    console.log(error);
    switch (error.status || error.StatusCode || error.code) {
      case 401:
      case 403:
        if (endpointName !== "getFrontendTest") {
          message.error("Invalid access key, login again. ");
          setTimeout(() => {
            handleLogout();
          }, 2000);
        }
        message.error(
          serverErrorMessage ? serverErrorMessage : "Something went wrong."
        );
        break;
      case "ERR_NETWORK":
        message.error("Network Error");
        break;
      default:
        message.error(
          serverErrorMessage ? serverErrorMessage : "Something went wrong."
        );
    }
  }
  if (error?.data == "Network Error") {
    message.error("Network Error");
  }
};

export const rtkQueryErrorLogger: Middleware =
  (_api: MiddlewareAPI) => (next: (action: any) => any) => (action: any) => {
    // RTK Query uses `createAsyncThunk` from redux-toolkit under the hood, so we're able to utilize these matchers!
    if (isRejectedWithValue(action)) {
      errorHandler(action);
    }

    return next(action);
  };
