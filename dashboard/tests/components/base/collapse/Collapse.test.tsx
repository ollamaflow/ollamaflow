import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import Collapse from "#/components/base/collapse/Collpase";

describe("Collapse", () => {
  test("should render collapse with items", () => {
    const items = [
      {
        key: "1",
        label: "Panel 1",
        children: "Content 1",
      },
      {
        key: "2",
        label: "Panel 2",
        children: "Content 2",
      },
    ];

    render(<Collapse items={items} />);

    expect(screen.getByText("Panel 1")).toBeInTheDocument();
    expect(screen.getByText("Panel 2")).toBeInTheDocument();
  });

  test("should render collapse with default active key", () => {
    const items = [
      {
        key: "1",
        label: "Panel 1",
        children: "Content 1",
      },
    ];

    render(<Collapse items={items} defaultActiveKey={["1"]} />);
    expect(screen.getByText("Content 1")).toBeInTheDocument();
  });

  test("should handle panel expansion", () => {
    const items = [
      {
        key: "1",
        label: "Panel 1",
        children: "Content 1",
      },
    ];

    render(<Collapse items={items} />);

    const panel = screen.getByText("Panel 1");
    fireEvent.click(panel);

    expect(screen.getByText("Content 1")).toBeInTheDocument();
  });

  test("should render collapse with custom className", () => {
    const items = [
      {
        key: "1",
        label: "Panel 1",
        children: "Content 1",
      },
    ];

    const customClass = "custom-collapse-class";
    render(<Collapse items={items} className={customClass} />);

    const collapse = screen.getByText("Panel 1").closest(".ant-collapse");
    expect(collapse).toHaveClass(customClass);
  });

  test("should render accordion style collapse", () => {
    const items = [
      {
        key: "1",
        label: "Panel 1",
        children: "Content 1",
      },
      {
        key: "2",
        label: "Panel 2",
        children: "Content 2",
      },
    ];

    render(<Collapse items={items} accordion />);

    expect(screen.getByText("Panel 1")).toBeInTheDocument();
    expect(screen.getByText("Panel 2")).toBeInTheDocument();
  });
});
