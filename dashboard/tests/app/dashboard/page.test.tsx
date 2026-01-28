import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import DashboardPage from "#/app/dashboard/page";

// Mock the LandingScreen component
jest.mock("#/page/home/LandingScreen", () => {
  return function MockLandingScreen() {
    return <div data-testid="landing-screen">Landing Screen Content</div>;
  };
});

describe("DashboardPage", () => {
  test("should render landing screen", () => {
    render(<DashboardPage />);

    expect(screen.getByTestId("landing-screen")).toBeInTheDocument();
    expect(screen.getByText("Landing Screen Content")).toBeInTheDocument();
  });

  test("should render only landing screen component", () => {
    render(<DashboardPage />);

    const landingScreen = screen.getByTestId("landing-screen");
    expect(landingScreen).toBeInTheDocument();

    // Should not have any other major components
    expect(screen.queryByTestId("login-page")).not.toBeInTheDocument();
    expect(screen.queryByTestId("backends-page")).not.toBeInTheDocument();
  });
});
