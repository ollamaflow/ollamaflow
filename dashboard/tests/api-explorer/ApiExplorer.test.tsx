import React from "react";
import { render, screen } from "@testing-library/react";
import { Provider } from "react-redux";

import ApiExplorerPage from "#/page/api-explorer/ApiExplorerPage";
import { createMockStore, createMockInitialState } from "../store/mockStore";

const renderApiExplorerPage = (initialState = createMockInitialState()) => {
  const store = createMockStore(initialState);
  return {
    ...render(
      <Provider store={store}>
        <ApiExplorerPage />
      </Provider>
    ),
    store,
  };
};

describe("ApiExplorerPage", () => {
  describe("Basic Render Tests", () => {
    it("should render page container with correct title", () => {
      const { container } = renderApiExplorerPage();
      expect(screen.getByText("API Explorer")).toBeInTheDocument();
      expect(container).toMatchSnapshot();
    });

    it("should render without crashing", () => {
      expect(() => renderApiExplorerPage()).not.toThrow();
    });

    it("should render the main container", () => {
      renderApiExplorerPage();

      // Check if the page renders without errors
      expect(screen.getByText("API Explorer")).toBeInTheDocument();
    });
  });
});
