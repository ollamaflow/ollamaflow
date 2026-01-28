import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import CreateFrontendPage from "#/app/dashboard/frontends/create/page";

// Mock the CreateComponentPage component
jest.mock("#/page/create-frontend", () => ({
  CreateComponentPage: function MockCreateComponentPage() {
    return (
      <div data-testid="create-frontend-page">Create Frontend Content</div>
    );
  },
}));

describe("CreateFrontendPage", () => {
  test("should render create frontend page", () => {
    render(<CreateFrontendPage />);

    expect(screen.getByTestId("create-frontend-page")).toBeInTheDocument();
    expect(screen.getByText("Create Frontend Content")).toBeInTheDocument();
  });

  test("should render only create frontend component", () => {
    render(<CreateFrontendPage />);

    const createFrontend = screen.getByTestId("create-frontend-page");
    expect(createFrontend).toBeInTheDocument();

    // Should not have any other major components
    expect(screen.queryByTestId("landing-screen")).not.toBeInTheDocument();
    expect(
      screen.queryByTestId("frontends-listing-page")
    ).not.toBeInTheDocument();
  });
});
