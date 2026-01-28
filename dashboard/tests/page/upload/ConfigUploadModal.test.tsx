import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import ConfigUploadModal from '#/page/upload/ConfigUploadModal';

// Mock antd components
jest.mock('antd', () => ({
  ...jest.requireActual('antd'),
  Tabs: ({ items }: any) => (
    <div data-testid="tabs">
      {items.map((item: any) => (
        <div key={item.key} data-testid={`tab-${item.key}`}>
          {item.children}
        </div>
      ))}
    </div>
  ),
  Upload: {
    Dragger: ({ children }: any) => <div data-testid="upload-dragger">{children}</div>,
  },
  message: {
    success: jest.fn(),
    error: jest.fn(),
    warning: jest.fn(),
  },
  Input: {
    TextArea: ({ value, onChange, placeholder }: any) => (
      <textarea data-testid="textarea" value={value} onChange={onChange} placeholder={placeholder} />
    ),
  },
}));

// Mock the base components
jest.mock('#/components/base/modal/Modal', () => {
  return function MockModal({ children, open, onCancel, footer }: any) {
    if (!open) return null;
    return (
      <div data-testid="modal">
        <div data-testid="modal-content">{children}</div>
        <div data-testid="modal-footer">
          {footer?.map((item: any, index: number) => (
            <button key={index} onClick={item.props.onClick}>
              {item.props.children}
            </button>
          ))}
        </div>
        <button data-testid="modal-close" onClick={onCancel}>
          Close
        </button>
      </div>
    );
  };
});

jest.mock('#/components/base/card/Card', () => {
  return function MockCard({ children, className }: any) {
    return (
      <div data-testid="card" className={className}>
        {children}
      </div>
    );
  };
});

jest.mock('#/components/base/button/Button', () => {
  return function MockButton({ children, onClick, type }: any) {
    return (
      <button data-testid={`button-${type || 'default'}`} onClick={onClick}>
        {children}
      </button>
    );
  };
});

jest.mock('#/components/base/typograpghy/Title', () => {
  return function MockTitle({ children, level }: any) {
    return <h5 data-testid={`title-${level}`}>{children}</h5>;
  };
});

jest.mock('#/components/base/typograpghy/Text', () => {
  return function MockText({ children, className }: any) {
    return (
      <span data-testid="text" className={className}>
        {children}
      </span>
    );
  };
});

describe('ConfigUploadModal', () => {
  const mockOnClose = jest.fn();
  const mockOnConfigLoaded = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('should render modal when visible is true', () => {
    render(<ConfigUploadModal visible={true} onClose={mockOnClose} onConfigLoaded={mockOnConfigLoaded} />);

    expect(screen.getByTestId('modal')).toBeInTheDocument();
    expect(screen.getByTestId('tabs')).toBeInTheDocument();
  });

  test('should not render modal when visible is false', () => {
    render(<ConfigUploadModal visible={false} onClose={mockOnClose} onConfigLoaded={mockOnConfigLoaded} />);

    expect(screen.queryByTestId('modal')).not.toBeInTheDocument();
  });

  test('should render upload tab content', () => {
    render(<ConfigUploadModal visible={true} onClose={mockOnClose} onConfigLoaded={mockOnConfigLoaded} />);

    expect(screen.getByTestId('tab-upload')).toBeInTheDocument();
    expect(screen.getByTestId('upload-dragger')).toBeInTheDocument();
  });

  test('should render paste tab content', () => {
    render(<ConfigUploadModal visible={true} onClose={mockOnClose} onConfigLoaded={mockOnConfigLoaded} />);

    expect(screen.getByTestId('tab-paste')).toBeInTheDocument();
    expect(screen.getByTestId('textarea')).toBeInTheDocument();
  });

  test('should handle cancel button click', () => {
    render(<ConfigUploadModal visible={true} onClose={mockOnClose} onConfigLoaded={mockOnConfigLoaded} />);

    const cancelButton = screen.getByText('Cancel');
    fireEvent.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalled();
  });

  test('should handle modal close', () => {
    render(<ConfigUploadModal visible={true} onClose={mockOnClose} onConfigLoaded={mockOnConfigLoaded} />);

    const closeButton = screen.getByTestId('modal-close');
    fireEvent.click(closeButton);

    expect(mockOnClose).toHaveBeenCalled();
  });

  test('should render upload area with correct content', () => {
    render(<ConfigUploadModal visible={true} onClose={mockOnClose} onConfigLoaded={mockOnConfigLoaded} />);

    expect(screen.getByText('Click or drag file to this area to upload')).toBeInTheDocument();
    expect(
      screen.getByText('Support for JSON, YAML, and text files. Please ensure your configuration is valid.')
    ).toBeInTheDocument();
  });

  test('should render paste section with correct content', () => {
    render(<ConfigUploadModal visible={true} onClose={mockOnClose} onConfigLoaded={mockOnConfigLoaded} />);

    expect(screen.getByText('Paste your configuration')).toBeInTheDocument();
    expect(screen.getByText('Paste your JSON, YAML, or text configuration below')).toBeInTheDocument();
  });

  test('should render load configuration button', () => {
    render(<ConfigUploadModal visible={true} onClose={mockOnClose} onConfigLoaded={mockOnConfigLoaded} />);

    expect(screen.getByText('Load Configuration')).toBeInTheDocument();
  });
});
