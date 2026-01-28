import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import FrontendsPage from "#/app/dashboard/frontends/page";

// Mock the FrontendsListingPage component
jest.mock("#/page/frontends/FrontendsListingPage", () => {
  return function MockFrontendsListingPage() {
    return (
      <div data-testid="frontends-listing-page">Frontends Listing Content</div>
    );
  };
});

describe("FrontendsPage", () => {
  test("should render frontends listing page", () => {
    render(<FrontendsPage />);

    expect(screen.getByTestId("frontends-listing-page")).toBeInTheDocument();
    expect(screen.getByText("Frontends Listing Content")).toBeInTheDocument();
  });

  test("should render only frontends listing component", () => {
    render(<FrontendsPage />);

    const frontendsListing = screen.getByTestId("frontends-listing-page");
    expect(frontendsListing).toBeInTheDocument();

    // Should not have any other major components
    expect(screen.queryByTestId("landing-screen")).not.toBeInTheDocument();
    expect(screen.queryByTestId("backends-page")).not.toBeInTheDocument();
  });
});
