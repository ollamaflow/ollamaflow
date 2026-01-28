import "@testing-library/jest-dom";

import { setupServer } from "msw/node";
import { commonHandlers } from "../handler";
import CreateComponentPage from "#/page/create-backend/CreateComponentPage";
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

  test("should render backend component page", async () => {
    const { container } = renderWithRedux(
      <CreateComponentPage />,
      createMockInitialState()
    );

    await waitFor(() => {
      expect(screen.getAllByText("Create Backend")[0]).toBeInTheDocument();
    });

    expect(container).toMatchSnapshot();
  });
});
