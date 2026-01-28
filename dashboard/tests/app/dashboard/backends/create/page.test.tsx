import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import CreateBackendPage from "#/app/dashboard/backends/create/page";

// Mock the CreateComponentPage component
jest.mock("#/page/create-backend", () => ({
  CreateComponentPage: function MockCreateComponentPage() {
    return <div data-testid="create-backend-page">Create Backend Content</div>;
  },
}));

describe("CreateBackendPage", () => {
  test("should render create backend page", () => {
    render(<CreateBackendPage />);

    expect(screen.getByTestId("create-backend-page")).toBeInTheDocument();
    expect(screen.getByText("Create Backend Content")).toBeInTheDocument();
  });

  test("should render only create backend component", () => {
    render(<CreateBackendPage />);

    const createBackend = screen.getByTestId("create-backend-page");
    expect(createBackend).toBeInTheDocument();

    // Should not have any other major components
    expect(screen.queryByTestId("landing-screen")).not.toBeInTheDocument();
    expect(
      screen.queryByTestId("backends-listing-page")
    ).not.toBeInTheDocument();
  });
});
