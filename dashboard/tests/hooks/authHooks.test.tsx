import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import {
  useCredentialsToLogin,
  useAdminCredentialsToLogin,
  useLogout,
} from "#/hooks/authHooks";

// Mock the store hooks
jest.mock("#/lib/store/hooks", () => ({
  useAppDispatch: jest.fn(() => jest.fn()),
}));

// Mock the store actions
jest.mock("#/lib/store/ollamaflow/reducer", () => ({
  storeAdminAccessKey: jest.fn(),
}));

// Mock the root reducer
jest.mock("#/lib/store/rootReducer", () => ({
  logout: jest.fn(),
}));

// Test component for useCredentialsToLogin hook
const TestCredentialsComponent = () => {
  const loginWithCredentials = useCredentialsToLogin();

  return (
    <div>
      <button onClick={() => loginWithCredentials("test-access-key")}>
        Login with Credentials
      </button>
    </div>
  );
};

// Test component for useAdminCredentialsToLogin hook
const TestAdminCredentialsComponent = () => {
  const loginWithAdminCredentials = useAdminCredentialsToLogin();

  return (
    <div>
      <button onClick={() => loginWithAdminCredentials("admin-access-key")}>
        Login with Admin Credentials
      </button>
    </div>
  );
};

// Test component for useLogout hook
const TestLogoutComponent = () => {
  const logOutFromSystem = useLogout();

  return (
    <div>
      <button onClick={logOutFromSystem}>Logout</button>
    </div>
  );
};

describe("Auth Hooks", () => {
  const mockDispatch = jest.fn();
  const mockStoreAdminAccessKey =
    require("#/lib/store/ollamaflow/reducer").storeAdminAccessKey;
  const mockLogout = require("#/lib/store/rootReducer").logout;

  beforeEach(() => {
    jest.clearAllMocks();
    require("#/lib/store/hooks").useAppDispatch.mockReturnValue(mockDispatch);
  });

  describe("useCredentialsToLogin", () => {
    test("should return login function", () => {
      render(<TestCredentialsComponent />);

      expect(screen.getByText("Login with Credentials")).toBeInTheDocument();
    });

    test("should call dispatch with storeAdminAccessKey when login is triggered", () => {
      render(<TestCredentialsComponent />);

      const loginButton = screen.getByText("Login with Credentials");
      fireEvent.click(loginButton);

      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("test-access-key")
      );
    });

    test("should handle different access keys", () => {
      const TestComponent = () => {
        const loginWithCredentials = useCredentialsToLogin();

        return (
          <div>
            <button onClick={() => loginWithCredentials("key1")}>
              Login with Key 1
            </button>
            <button onClick={() => loginWithCredentials("key2")}>
              Login with Key 2
            </button>
            <button onClick={() => loginWithCredentials("")}>
              Login with Empty Key
            </button>
          </div>
        );
      };

      render(<TestComponent />);

      const key1Button = screen.getByText("Login with Key 1");
      const key2Button = screen.getByText("Login with Key 2");
      const emptyKeyButton = screen.getByText("Login with Empty Key");

      fireEvent.click(key1Button);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("key1")
      );

      fireEvent.click(key2Button);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("key2")
      );

      fireEvent.click(emptyKeyButton);
      expect(mockDispatch).toHaveBeenCalledWith(mockStoreAdminAccessKey(""));
    });

    test("should handle multiple login calls", () => {
      render(<TestCredentialsComponent />);

      const loginButton = screen.getByText("Login with Credentials");

      // First call
      fireEvent.click(loginButton);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("test-access-key")
      );
      expect(mockDispatch).toHaveBeenCalledTimes(1);

      // Second call
      fireEvent.click(loginButton);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("test-access-key")
      );
      expect(mockDispatch).toHaveBeenCalledTimes(2);
    });
  });

  describe("useAdminCredentialsToLogin", () => {
    test("should return admin login function", () => {
      render(<TestAdminCredentialsComponent />);

      expect(
        screen.getByText("Login with Admin Credentials")
      ).toBeInTheDocument();
    });

    test("should call dispatch with storeAdminAccessKey when admin login is triggered", () => {
      render(<TestAdminCredentialsComponent />);

      const loginButton = screen.getByText("Login with Admin Credentials");
      fireEvent.click(loginButton);

      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("admin-access-key")
      );
    });

    test("should handle different admin access keys", () => {
      const TestComponent = () => {
        const loginWithAdminCredentials = useAdminCredentialsToLogin();

        return (
          <div>
            <button onClick={() => loginWithAdminCredentials("admin1")}>
              Login with Admin 1
            </button>
            <button onClick={() => loginWithAdminCredentials("admin2")}>
              Login with Admin 2
            </button>
            <button onClick={() => loginWithAdminCredentials("super-admin")}>
              Login with Super Admin
            </button>
          </div>
        );
      };

      render(<TestComponent />);

      const admin1Button = screen.getByText("Login with Admin 1");
      const admin2Button = screen.getByText("Login with Admin 2");
      const superAdminButton = screen.getByText("Login with Super Admin");

      fireEvent.click(admin1Button);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("admin1")
      );

      fireEvent.click(admin2Button);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("admin2")
      );

      fireEvent.click(superAdminButton);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("super-admin")
      );
    });

    test("should handle multiple admin login calls", () => {
      render(<TestAdminCredentialsComponent />);

      const loginButton = screen.getByText("Login with Admin Credentials");

      // First call
      fireEvent.click(loginButton);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("admin-access-key")
      );
      expect(mockDispatch).toHaveBeenCalledTimes(1);

      // Second call
      fireEvent.click(loginButton);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("admin-access-key")
      );
      expect(mockDispatch).toHaveBeenCalledTimes(2);
    });

    test("should handle empty admin access key", () => {
      const TestComponent = () => {
        const loginWithAdminCredentials = useAdminCredentialsToLogin();

        return (
          <div>
            <button onClick={() => loginWithAdminCredentials("")}>
              Login with Empty Admin Key
            </button>
          </div>
        );
      };

      render(<TestComponent />);

      const loginButton = screen.getByText("Login with Empty Admin Key");
      fireEvent.click(loginButton);

      expect(mockDispatch).toHaveBeenCalledWith(mockStoreAdminAccessKey(""));
    });
  });

  describe("useLogout", () => {
    test("should return logout function", () => {
      render(<TestLogoutComponent />);

      expect(screen.getByText("Logout")).toBeInTheDocument();
    });

    test("should call dispatch with logout when logout is triggered", () => {
      render(<TestLogoutComponent />);

      const logoutButton = screen.getByText("Logout");
      fireEvent.click(logoutButton);

      expect(mockDispatch).toHaveBeenCalledWith(mockLogout());
    });

    test("should handle multiple logout calls", () => {
      render(<TestLogoutComponent />);

      const logoutButton = screen.getByText("Logout");

      // First call
      fireEvent.click(logoutButton);
      expect(mockDispatch).toHaveBeenCalledWith(mockLogout());
      expect(mockDispatch).toHaveBeenCalledTimes(1);

      // Second call
      fireEvent.click(logoutButton);
      expect(mockDispatch).toHaveBeenCalledWith(mockLogout());
      expect(mockDispatch).toHaveBeenCalledTimes(2);
    });

    test("should work with different component structures", () => {
      const TestComponent = () => {
        const logOutFromSystem = useLogout();

        return (
          <div>
            <header>
              <h1>Dashboard</h1>
              <button onClick={logOutFromSystem}>Sign Out</button>
            </header>
            <main>
              <p>Dashboard content</p>
            </main>
          </div>
        );
      };

      render(<TestComponent />);

      expect(screen.getByText("Dashboard")).toBeInTheDocument();
      expect(screen.getByText("Sign Out")).toBeInTheDocument();
      expect(screen.getByText("Dashboard content")).toBeInTheDocument();

      const logoutButton = screen.getByText("Sign Out");
      fireEvent.click(logoutButton);

      expect(mockDispatch).toHaveBeenCalledWith(mockLogout());
    });
  });

  describe("Hook Integration", () => {
    test("should work with multiple hooks in same component", () => {
      const TestComponent = () => {
        const loginWithCredentials = useCredentialsToLogin();
        const loginWithAdminCredentials = useAdminCredentialsToLogin();
        const logOutFromSystem = useLogout();

        return (
          <div>
            <button onClick={() => loginWithCredentials("user-key")}>
              User Login
            </button>
            <button onClick={() => loginWithAdminCredentials("admin-key")}>
              Admin Login
            </button>
            <button onClick={logOutFromSystem}>Logout</button>
          </div>
        );
      };

      render(<TestComponent />);

      const userLoginButton = screen.getByText("User Login");
      const adminLoginButton = screen.getByText("Admin Login");
      const logoutButton = screen.getByText("Logout");

      fireEvent.click(userLoginButton);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("user-key")
      );

      fireEvent.click(adminLoginButton);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("admin-key")
      );

      fireEvent.click(logoutButton);
      expect(mockDispatch).toHaveBeenCalledWith(mockLogout());
    });

    test("should handle hooks in different components", () => {
      render(
        <div>
          <TestCredentialsComponent />
          <TestAdminCredentialsComponent />
          <TestLogoutComponent />
        </div>
      );

      const credentialsButton = screen.getByText("Login with Credentials");
      const adminButton = screen.getByText("Login with Admin Credentials");
      const logoutButton = screen.getByText("Logout");

      fireEvent.click(credentialsButton);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("test-access-key")
      );

      fireEvent.click(adminButton);
      expect(mockDispatch).toHaveBeenCalledWith(
        mockStoreAdminAccessKey("admin-access-key")
      );

      fireEvent.click(logoutButton);
      expect(mockDispatch).toHaveBeenCalledWith(mockLogout());
    });
  });
});
