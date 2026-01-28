import "@testing-library/jest-dom";
import React from "react";
import { screen, fireEvent, waitFor } from "@testing-library/react";
import { createMockInitialState } from "../store/mockStore";

import { setupServer } from "msw/node";
import { commonHandlers } from "../handler";
import { renderWithRedux } from "../store/utils";
import FrontendsListingPage from "#/page/frontends/FrontendsListingPage";

const server = setupServer(...commonHandlers);

describe("FrontendsListingPage", () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
  });
  afterAll(() => server.close());
  test("should render frontends listing page", async () => {
    const { container } = renderWithRedux(
      <FrontendsListingPage />,
      createMockInitialState()
    );
    expect(screen.getByText("Frontends")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText("Frontend 1")).toBeInTheDocument();
    });
    expect(container).toMatchSnapshot();
  });

  test("should render sticky session columns", async () => {
    renderWithRedux(<FrontendsListingPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByText("Frontend 1")).toBeInTheDocument();
    });

    // Check for sticky session columns
    expect(screen.getByText("Sticky Sessions")).toBeInTheDocument();
    expect(
      screen.getByText("Sticky Session Expiration (ms)")
    ).toBeInTheDocument();

    // Check for sticky session data display
    expect(screen.getAllByText("Disabled")[0]).toBeInTheDocument(); // UseStickySessions: false
  });
});
