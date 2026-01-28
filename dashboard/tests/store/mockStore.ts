import { configureStore } from "@reduxjs/toolkit";
import { RootState, AppStore, AppDispatch } from "#/lib/store/store";
import apiSlice from "#/lib/store/rtk/rtkApiInstance";

// ✅ Create a fresh mock state per test
export const createMockInitialState = (): RootState => ({
  [apiSlice.reducerPath]: undefined as any, // Let RTK initialize the API slice
  ollamaFlow: {
    adminAccessKey: "adminAccessKey",
  },
});

// ✅ Create a mock store with dynamic state
export const createMockStore = (initialState: RootState) => {
  return configureStore({
    reducer: () => initialState,
    preloadedState: initialState,
    middleware: (gDM: any) =>
      gDM({
        serializableCheck: false,
      }).concat([apiSlice.middleware]),
  });
};

// Test function to ensure store types are used
export const testStoreTypes = () => {
  const initialState = createMockInitialState();
  const store = createMockStore(initialState);

  // This ensures the types are used and covered
  const storeType: AppStore = store;
  const dispatchType: AppDispatch = store.dispatch;

  return { storeType, dispatchType, initialState };
};
