import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import Modal from "#/components/base/modal/Modal";

describe("Modal", () => {
  test("should render modal with title and content", () => {
    render(
      <Modal title="Test Modal" open>
        <div>Modal Content</div>
      </Modal>
    );

    expect(screen.getByText("Test Modal")).toBeInTheDocument();
    expect(screen.getByText("Modal Content")).toBeInTheDocument();
  });

  test("should not render modal when closed", () => {
    render(
      <Modal title="Test Modal" open={false}>
        <div>Modal Content</div>
      </Modal>
    );

    expect(screen.queryByText("Test Modal")).not.toBeInTheDocument();
    expect(screen.queryByText("Modal Content")).not.toBeInTheDocument();
  });

  test("should render modal with footer", () => {
    const footer = [
      <button key="cancel">Cancel</button>,
      <button key="ok">OK</button>,
    ];

    render(
      <Modal title="Test Modal" open footer={footer}>
        <div>Modal Content</div>
      </Modal>
    );

    expect(screen.getByText("Cancel")).toBeInTheDocument();
    expect(screen.getByText("OK")).toBeInTheDocument();
  });

  test("should render modal with custom width", () => {
    render(
      <Modal title="Test Modal" open width={800}>
        <div>Modal Content</div>
      </Modal>
    );

    expect(screen.getByText("Test Modal")).toBeInTheDocument();
  });

  test("should render modal with custom className", () => {
    const customClass = "custom-modal-class";
    render(
      <Modal title="Test Modal" open className={customClass}>
        <div>Modal Content</div>
      </Modal>
    );

    expect(screen.getByText("Test Modal")).toBeInTheDocument();
  });

  test("should render modal with centered prop", () => {
    render(
      <Modal title="Test Modal" open centered>
        <div>Modal Content</div>
      </Modal>
    );

    expect(screen.getByText("Test Modal")).toBeInTheDocument();
  });

  test("should render modal with maskClosable prop", () => {
    render(
      <Modal title="Test Modal" open maskClosable={false}>
        <div>Modal Content</div>
      </Modal>
    );

    expect(screen.getByText("Test Modal")).toBeInTheDocument();
  });

  test("should render modal with destroyOnClose prop", () => {
    render(
      <Modal title="Test Modal" open destroyOnClose>
        <div>Modal Content</div>
      </Modal>
    );

    expect(screen.getByText("Test Modal")).toBeInTheDocument();
  });

  test("should handle onOk callback", () => {
    const handleOk = jest.fn();
    render(
      <Modal title="Test Modal" open onOk={handleOk}>
        <div>Modal Content</div>
      </Modal>
    );

    const okButton = screen.getByText("OK");
    fireEvent.click(okButton);

    expect(handleOk).toHaveBeenCalled();
  });

  test("should handle onCancel callback", () => {
    const handleCancel = jest.fn();
    render(
      <Modal title="Test Modal" open onCancel={handleCancel}>
        <div>Modal Content</div>
      </Modal>
    );

    const cancelButton = screen.getByText("Cancel");
    fireEvent.click(cancelButton);

    expect(handleCancel).toHaveBeenCalled();
  });
});
