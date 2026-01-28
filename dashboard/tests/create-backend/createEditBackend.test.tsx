import "@testing-library/jest-dom";
import React from "react";
import { screen, waitFor } from "@testing-library/react";

import { setupServer } from "msw/node";
import { commonHandlers } from "../handler";
import CreateEditBackend from "#/page/create-backend/CreateEditBackend";
import { renderWithRedux } from "../store/utils";
import { createMockInitialState } from "../store/mockStore";

const server = setupServer(...commonHandlers);

describe("CreateEditBackend", () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
  });
  afterAll(() => server.close());

  test("should render create backend page", async () => {
    const { container } = renderWithRedux(
      <CreateEditBackend
        mode="create"
        initialValues={{}}
        onSubmit={jest.fn()}
        loading={false}
      />,
      createMockInitialState()
    );

    await waitFor(() => {
      expect(screen.getByText("Create Backend")).toBeInTheDocument();
    });

    expect(container).toMatchSnapshot();
  });

  test("should render edit backend page", async () => {
    const { container } = renderWithRedux(
      <CreateEditBackend
        mode="edit"
        initialValues={{}}
        onSubmit={jest.fn()}
        loading={false}
      />,
      createMockInitialState()
    );

    await waitFor(() => {
      expect(screen.getByText("Update Backend")).toBeInTheDocument();
    });

    expect(container).toMatchSnapshot();
  });

  test("should render create backend page with initial values", async () => {
    const { container } = renderWithRedux(
      <CreateEditBackend
        mode="create"
        initialValues={{
          Name: "",
          HealthCheckMethod: "GET",
          Identifier: "",
          HealthCheckUrl: "http://localhost:43411/health",
          HealthyThreshold: 1,
          Hostname: "localhost",
          Port: 43411,
          LogRequestFull: true,
          MaxParallelRequests: 100,
          RateLimitRequestsThreshold: 100,
          Ssl: true,
          UnhealthyThreshold: 2,
          LogRequestBody: true,
          LogResponseBody: true,
        }}
        onSubmit={jest.fn()}
        loading={false}
      />,
      createMockInitialState()
    );

    await waitFor(() => {
      expect(screen.getByText("Create Backend")).toBeInTheDocument();
    });

    expect(container).toMatchSnapshot();
  });

  test("should render edit backend page with initial values", async () => {
    const { container } = renderWithRedux(
      <CreateEditBackend
        mode="edit"
        initialValues={{
          Name: "Backend 1",
          HealthCheckMethod: "GET",
          Identifier: "backend-1",
          HealthCheckUrl: "http://localhost:43411/health",
          HealthyThreshold: 1,
          Hostname: "localhost",
          Port: 43411,
          LogRequestFull: true,
          MaxParallelRequests: 100,
          RateLimitRequestsThreshold: 100,
          Ssl: true,
          UnhealthyThreshold: 2,
          LogRequestBody: true,
          LogResponseBody: true,
        }}
        onSubmit={jest.fn()}
        loading={false}
      />,
      createMockInitialState()
    );

    await waitFor(() => {
      expect(screen.getByText("Update Backend")).toBeInTheDocument();
    });

    expect(container).toMatchSnapshot();
  });
});
