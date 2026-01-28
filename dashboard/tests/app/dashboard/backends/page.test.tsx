import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import BackendsPage from "#/app/dashboard/backends/page";

// Mock the BackendsListingPage component
jest.mock("#/page/backends/BackendsListingPage", () => {
  return function MockBackendsListingPage() {
    return (
      <div data-testid="backends-listing-page">Backends Listing Content</div>
    );
  };
});

describe("BackendsPage", () => {
  test("should render backends listing page", () => {
    render(<BackendsPage />);

    expect(screen.getByTestId("backends-listing-page")).toBeInTheDocument();
    expect(screen.getByText("Backends Listing Content")).toBeInTheDocument();
  });

  test("should render only backends listing component", () => {
    render(<BackendsPage />);

    const backendsListing = screen.getByTestId("backends-listing-page");
    expect(backendsListing).toBeInTheDocument();

    // Should not have any other major components
    expect(screen.queryByTestId("landing-screen")).not.toBeInTheDocument();
    expect(screen.queryByTestId("frontends-page")).not.toBeInTheDocument();
  });
});
