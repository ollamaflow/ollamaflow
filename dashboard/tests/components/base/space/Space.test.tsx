import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import Space from "#/components/base/space/Space";

describe("Space", () => {
  test("should render space with children", () => {
    render(
      <Space>
        <div>Item 1</div>
        <div>Item 2</div>
      </Space>
    );

    expect(screen.getByText("Item 1")).toBeInTheDocument();
    expect(screen.getByText("Item 2")).toBeInTheDocument();
  });

  test("should render space with direction", () => {
    render(
      <Space direction="vertical">
        <div>Item 1</div>
        <div>Item 2</div>
      </Space>
    );

    expect(screen.getByText("Item 1")).toBeInTheDocument();
    expect(screen.getByText("Item 2")).toBeInTheDocument();
  });

  test("should render space with size", () => {
    render(
      <Space size="large">
        <div>Item 1</div>
        <div>Item 2</div>
      </Space>
    );

    expect(screen.getByText("Item 1")).toBeInTheDocument();
    expect(screen.getByText("Item 2")).toBeInTheDocument();
  });

  test("should render space with custom className", () => {
    const customClass = "custom-space-class";
    render(
      <Space className={customClass}>
        <div>Item 1</div>
        <div>Item 2</div>
      </Space>
    );

    const space = screen.getByText("Item 1").closest(".ant-space");
    expect(space).toHaveClass(customClass);
  });

  test("should render space with align", () => {
    render(
      <Space align="center">
        <div>Item 1</div>
        <div>Item 2</div>
      </Space>
    );

    expect(screen.getByText("Item 1")).toBeInTheDocument();
    expect(screen.getByText("Item 2")).toBeInTheDocument();
  });

  test("should render space with wrap", () => {
    render(
      <Space wrap>
        <div>Item 1</div>
        <div>Item 2</div>
      </Space>
    );

    expect(screen.getByText("Item 1")).toBeInTheDocument();
    expect(screen.getByText("Item 2")).toBeInTheDocument();
  });

  test("should render space with split", () => {
    render(
      <Space split={<div>|</div>}>
        <div>Item 1</div>
        <div>Item 2</div>
      </Space>
    );

    expect(screen.getByText("Item 1")).toBeInTheDocument();
    expect(screen.getByText("Item 2")).toBeInTheDocument();
    expect(screen.getByText("|")).toBeInTheDocument();
  });

  test("should render space with custom size", () => {
    render(
      <Space size={[16, 32]}>
        <div>Item 1</div>
        <div>Item 2</div>
      </Space>
    );

    expect(screen.getByText("Item 1")).toBeInTheDocument();
    expect(screen.getByText("Item 2")).toBeInTheDocument();
  });
});
