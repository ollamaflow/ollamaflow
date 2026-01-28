"use client";
import { StyleProvider } from "@ant-design/cssinjs";
import { AntdRegistry } from "@ant-design/nextjs-registry";
import { AppContext } from "../hooks/appHooks";
import React, { useState, useEffect } from "react";
import { ThemeEnum } from "../types/types";
import { darkTheme, primaryTheme } from "../theme/theme";
import { ConfigProvider } from "antd";
import { localStorageKeys } from "../constants/constant";
import StoreProvider from "#/lib/store/StoreProvider";
import AuthLayout from "./AuthLayout";

const getThemeFromLocalStorage = () => {
  let theme;
  if (typeof localStorage !== "undefined") {
    theme = localStorage.getItem(localStorageKeys.theme);
  }
  return theme ? (theme as ThemeEnum) : ThemeEnum.LIGHT;
};

const AppProviders = ({ children }: { children: React.ReactNode }) => {
  const [theme, setTheme] = useState<ThemeEnum>(ThemeEnum.LIGHT);
  const [mounted, setMounted] = useState(false);

  // Load theme after mount to prevent hydration mismatch
  useEffect(() => {
    setMounted(true);
    const savedTheme = getThemeFromLocalStorage();
    setTheme(savedTheme);
  }, []);

  const handleThemeChange = (theme: ThemeEnum) => {
    localStorage.setItem(localStorageKeys.theme, theme);
    setTheme(theme);
  };

  return (
    <StoreProvider>
      <AppContext.Provider
        value={{
          theme,
          setTheme: handleThemeChange,
        }}
      >
        <StyleProvider hashPriority="high">
          <AntdRegistry>
            <ConfigProvider
              theme={theme === ThemeEnum.LIGHT ? primaryTheme : darkTheme}
            >
              <AuthLayout
                className={theme === ThemeEnum.DARK ? "theme-dark-mode" : ""}
              >
                {children}
              </AuthLayout>
            </ConfigProvider>
          </AntdRegistry>
        </StyleProvider>
      </AppContext.Provider>
    </StoreProvider>
  );
};

export default AppProviders;
