import "@testing-library/jest-dom";

import { setupServer } from "msw/node";
import { commonHandlers } from "../handler";
import CreateComponentPage from "#/page/create-frontend/CreateComponentPage";
import { renderWithRedux } from "../store/utils";
import { createMockInitialState } from "../store/mockStore";
import { screen, waitFor } from "@testing-library/react";

const server = setupServer(...commonHandlers);

describe("CreateComponentPage", () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
  });
  afterAll(() => server.close());

  test("should render frontend component page", async () => {
    const { container } = renderWithRedux(
      <CreateComponentPage />,
      createMockInitialState()
    );

    await waitFor(() => {
      expect(screen.getAllByText("Create Frontend")[0]).toBeInTheDocument();
    });

    expect(container).toMatchSnapshot();
  });

  test("should render sticky session fields", async () => {
    renderWithRedux(
      <CreateComponentPage />,
      createMockInitialState()
    );

    await waitFor(() => {
      expect(screen.getAllByText("Create Frontend")[0]).toBeInTheDocument();
    });

    // Check for sticky session fields
    expect(screen.getByText("Use Sticky Sessions")).toBeInTheDocument();
    expect(screen.getByText("Sticky Session Expiration (ms)")).toBeInTheDocument();
  });
});
