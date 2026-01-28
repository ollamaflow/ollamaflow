import React from "react";
import classNames from "classnames";
import OllamaFlowParagraph from "../base/typograpghy/Paragraph";
import styles from "./login-layout.module.scss";
import OllamaFlowFlex from "../base/flex/Flex";
import ThemeModeSwitch from "../theme-mode-switch/ThemeModeSwitch";
import OllamaFlowLogo from "../logo/OllamaFlowLogo";
import OllamaFlowTitle from "../base/typograpghy/Title";

const LoginLayout = ({
  children,
  isAdmin,
}: {
  children: React.ReactNode;
  isAdmin?: boolean;
}) => {
  return (
    <OllamaFlowFlex className={styles.userLoginPage} vertical gap={20}>
      <OllamaFlowFlex
        className={classNames(styles.userLoginPageHeader, "mb pb pt pr pl")}
        align="center"
        justify="space-between"
      >
        <OllamaFlowLogo />
        <OllamaFlowFlex align="center" gap={10}>
          <ThemeModeSwitch />
        </OllamaFlowFlex>
      </OllamaFlowFlex>
      <div className={styles.loginTitle}>
        <OllamaFlowTitle fontSize={22} weight={600}>
          {isAdmin ? "Admin Login" : "Login"}
        </OllamaFlowTitle>
        <OllamaFlowParagraph className={styles.loginDescription}>
          {isAdmin
            ? "Please enter your access key to login"
            : "Please enter your email and password to login"}
        </OllamaFlowParagraph>
      </div>
      <div className={styles.loginBox}>{children}</div>
    </OllamaFlowFlex>
  );
};

export default LoginLayout;
