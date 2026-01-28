import { createSlice, PayloadAction } from "@reduxjs/toolkit";

interface OllamaFlowState {
  adminAccessKey: string | null;
}

const initialState: OllamaFlowState = {
  adminAccessKey: null,
};

const ollamaFlowSlice = createSlice({
  name: "ollamaFlow",
  initialState,
  reducers: {
    storeAdminAccessKey: (state, action: PayloadAction<string | null>) => {
      state.adminAccessKey = action.payload;
    },
  },
});

export const { storeAdminAccessKey } = ollamaFlowSlice.actions;

export default ollamaFlowSlice.reducer;
