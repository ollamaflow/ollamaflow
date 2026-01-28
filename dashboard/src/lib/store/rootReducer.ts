import { UnknownAction, combineReducers, createAction } from "@reduxjs/toolkit";
import { rtkQueryErrorLogger } from "./rtkApiMiddlewear";
import apiSlice from "./rtk/rtkApiInstance";
import ollamaFlowReducer from "./ollamaflow/reducer";
import { localStorageKeys, paths } from "#/constants/constant";

const rootReducer = combineReducers({
  [apiSlice.reducerPath]: apiSlice.reducer,
  ollamaFlow: ollamaFlowReducer,
});

export const apiMiddleWares = [rtkQueryErrorLogger, apiSlice.middleware];

export const logout = createAction("logout");

export const handleLogout = (path?: string) => {
  console.log("handleLogout");
  localStorage.removeItem(localStorageKeys.adminAccessKey);
  localStorage.removeItem(localStorageKeys.serverUrl);
  window.location.href = path ? path : paths.Login;
};

const resettableRootReducer = (
  state: ReturnType<typeof rootReducer> | undefined,
  action: UnknownAction
) => {
  if (action.type === "logout") {
    handleLogout(action.payload as any);
  }
  return rootReducer(state, action);
};

export default resettableRootReducer;
