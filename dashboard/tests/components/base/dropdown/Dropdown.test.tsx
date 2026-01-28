import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import Dropdown from "#/components/base/dropdown/Dropdown";

describe("Dropdown", () => {
  test("should render dropdown with trigger", () => {
    const menu = {
      items: [
        {
          key: "1",
          label: "Item 1",
        },
        {
          key: "2",
          label: "Item 2",
        },
      ],
    };

    render(
      <Dropdown menu={menu}>
        <button>Click me</button>
      </Dropdown>
    );

    expect(screen.getByText("Click me")).toBeInTheDocument();
  });

  test("should render dropdown with custom trigger", () => {
    const menu = {
      items: [
        {
          key: "1",
          label: "Item 1",
        },
      ],
    };

    render(
      <Dropdown menu={menu} trigger={["hover"]}>
        <span>Hover me</span>
      </Dropdown>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });

  test("should render dropdown with placement", () => {
    const menu = {
      items: [
        {
          key: "1",
          label: "Item 1",
        },
      ],
    };

    render(
      <Dropdown menu={menu} placement="bottomLeft">
        <button>Click me</button>
      </Dropdown>
    );

    expect(screen.getByText("Click me")).toBeInTheDocument();
  });

  test("should render dropdown with disabled state", () => {
    const menu = {
      items: [
        {
          key: "1",
          label: "Item 1",
        },
      ],
    };

    render(
      <Dropdown menu={menu} disabled>
        <button>Click me</button>
      </Dropdown>
    );

    expect(screen.getByText("Click me")).toBeInTheDocument();
  });

  test("should render dropdown with custom className", () => {
    const menu = {
      items: [
        {
          key: "1",
          label: "Item 1",
        },
      ],
    };

    const customClass = "custom-dropdown-class";
    render(
      <Dropdown menu={menu} className={customClass}>
        <button>Click me</button>
      </Dropdown>
    );

    expect(screen.getByText("Click me")).toBeInTheDocument();
  });

  test("should render dropdown with arrow", () => {
    const menu = {
      items: [
        {
          key: "1",
          label: "Item 1",
        },
      ],
    };

    render(
      <Dropdown menu={menu} arrow>
        <button>Click me</button>
      </Dropdown>
    );

    expect(screen.getByText("Click me")).toBeInTheDocument();
  });
});
