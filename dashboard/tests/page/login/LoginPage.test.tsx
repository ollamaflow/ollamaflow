import React from "react";
import { render, screen } from "@testing-library/react";
import { Provider } from "react-redux";

import LoginPage from "#/page/login/LoginPage";
import { createMockStore, createMockInitialState } from "../../store/mockStore";
import { ollamaServerUrl } from "#/constants/apiConfig";

// Mock the hoc
jest.mock("#/hoc/hoc", () => ({
  forGuest: (Component: React.ComponentType) => Component,
}));

const renderLoginPage = (initialState = createMockInitialState()) => {
  const store = createMockStore(initialState);
  return {
    ...render(
      <Provider store={store}>
        <LoginPage />
      </Provider>
    ),
    store,
  };
};

describe("LoginPage", () => {
  describe("Basic Render Tests", () => {
    it("should render login form with all required elements", () => {
      renderLoginPage();

      // Check if form elements are present
      expect(
        screen.getByLabelText("OllamaFlow Server URL")
      ).toBeInTheDocument();
      expect(screen.getByLabelText("Access Key")).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Login" })).toBeInTheDocument();
    });

    it("should render with initial URL value", () => {
      renderLoginPage();

      // Check if initial URL value is set
      const urlInput = screen.getByDisplayValue(ollamaServerUrl);
      expect(urlInput).toBeInTheDocument();
    });

    it("should have access key input disabled initially", () => {
      renderLoginPage();

      const accessKeyInput = screen.getByLabelText("Access Key");
      expect(accessKeyInput).toBeDisabled();
    });

    it("should have proper input types", () => {
      renderLoginPage();

      const urlInput = screen.getByLabelText("OllamaFlow Server URL");
      const accessKeyInput = screen.getByLabelText("Access Key");

      expect(urlInput).toHaveAttribute("type", "text");
      expect(accessKeyInput).toHaveAttribute("type", "password");
    });

    it("should have proper placeholders", () => {
      renderLoginPage();

      expect(
        screen.getByPlaceholderText("https://your-ollama-server.com")
      ).toBeInTheDocument();
      expect(
        screen.getByPlaceholderText("Enter your access key")
      ).toBeInTheDocument();
    });
  });
});
