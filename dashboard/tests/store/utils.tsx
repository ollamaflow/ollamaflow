import resettableRootReducer from "#/lib/store/rootReducer";
import apiSlice from "#/lib/store/rtk/rtkApiInstance";
import { RootState } from "#/lib/store/store";
import { StyleProvider } from "@ant-design/cssinjs";
import { configureStore } from "@reduxjs/toolkit";
import { render } from "@testing-library/react";
import { Provider } from "react-redux";

export const renderWithRedux = (
  ui: React.ReactNode,
  reduxState?: RootState
) => {
  const store = configureStore({
    reducer: resettableRootReducer,
    preloadedState: reduxState,
    middleware: (gDM: any) =>
      gDM({
        serializableCheck: false,
      }).concat([apiSlice.middleware]),
  });

  const utils = render(
    <Provider store={store}>
      <StyleProvider hashPriority="high">
        <>{ui}</>
      </StyleProvider>
    </Provider>
  );

  return {
    ...utils,
    store, // ⬅️ expose store for dispatching or inspection if needed
  };
};
