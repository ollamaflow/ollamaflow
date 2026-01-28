import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import MainPage from "#/app/page";

// Mock the LoginPage component
jest.mock("#/page/login/LoginPage", () => {
  return function MockLoginPage() {
    return <div data-testid="login-page">Login Page Content</div>;
  };
});

describe("MainPage", () => {
  test("should render login page", () => {
    render(<MainPage />);

    expect(screen.getByTestId("login-page")).toBeInTheDocument();
    expect(screen.getByText("Login Page Content")).toBeInTheDocument();
  });

  test("should render only login page component", () => {
    render(<MainPage />);

    const loginPage = screen.getByTestId("login-page");
    expect(loginPage).toBeInTheDocument();

    // Should not have any other major components
    expect(screen.queryByTestId("dashboard")).not.toBeInTheDocument();
    expect(screen.queryByTestId("landing-screen")).not.toBeInTheDocument();
  });
});
