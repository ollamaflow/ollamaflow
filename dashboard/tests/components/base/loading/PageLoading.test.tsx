import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import PageLoading from "#/components/base/loading/PageLoading";

describe("PageLoading", () => {
  test("should render page loading with custom tip", () => {
    render(<PageLoading tip="Loading..." />);
    expect(screen.getByText("Loading...")).toBeInTheDocument();
  });
});
