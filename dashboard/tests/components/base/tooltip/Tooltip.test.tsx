import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import Tooltip from "#/components/base/tooltip/Tooltip";

describe("Tooltip", () => {
  test("should render tooltip with children", () => {
    render(
      <Tooltip title="Tooltip text">
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });

  test("should render tooltip with custom title", () => {
    render(
      <Tooltip title="Custom tooltip">
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });

  test("should render tooltip with placement", () => {
    render(
      <Tooltip title="Tooltip text" placement="top">
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });

  test("should render tooltip with color", () => {
    render(
      <Tooltip title="Tooltip text" color="red">
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });

  test("should render tooltip with custom className", () => {
    const customClass = "custom-tooltip-class";
    render(
      <Tooltip title="Tooltip text" className={customClass}>
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });

  test("should render tooltip with mouseEnterDelay", () => {
    render(
      <Tooltip title="Tooltip text" mouseEnterDelay={0.5}>
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });

  test("should render tooltip with mouseLeaveDelay", () => {
    render(
      <Tooltip title="Tooltip text" mouseLeaveDelay={0.5}>
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });

  test("should render tooltip with overlayClassName", () => {
    const overlayClass = "custom-overlay-class";
    render(
      <Tooltip title="Tooltip text" overlayClassName={overlayClass}>
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });

  test("should render tooltip with overlayStyle", () => {
    const overlayStyle = { backgroundColor: "red" };
    render(
      <Tooltip title="Tooltip text" overlayStyle={overlayStyle}>
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });

  test("should render tooltip with open prop", () => {
    render(
      <Tooltip title="Tooltip text" open>
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });

  test("should render tooltip with trigger", () => {
    render(
      <Tooltip title="Tooltip text" trigger="click">
        <button>Click me</button>
      </Tooltip>
    );

    expect(screen.getByText("Click me")).toBeInTheDocument();
  });

  test("should render tooltip with destroyTooltipOnHide", () => {
    render(
      <Tooltip title="Tooltip text" destroyTooltipOnHide>
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText("Hover me")).toBeInTheDocument();
  });
});
