"use client";
import React from "react";
import {
  CloseCircleOutlined,
  WarningOutlined,
  InfoCircleOutlined,
} from "@ant-design/icons";
import styles from "./fallback.module.scss";
import classNames from "classnames";
import { Flex, Typography } from "antd";
import { TextProps } from "antd/es/typography/Text";

const { Text } = Typography;

export enum FallBackEnums {
  ERROR = "error",
  WARNING = "warn",
  INFO = "info",
}

interface FallBackProps {
  icon?: React.ReactNode;
  children?: React.ReactNode;
  type?: FallBackEnums;
  retry?: () => void;
  textProps?: TextProps;
  className?: string;
  error?: any;
}

const FallBack = ({
  icon,
  children: childrenProp = "Something went wrong.",
  type = FallBackEnums.ERROR,
  retry,
  textProps,
  className,
  error,
}: FallBackProps) => {
  const children =
    error?.code === "ERR_NETWORK"
      ? "Unable to communicate with specified OllamaFlow instance.  Please ensure your instance is online."
      : childrenProp;
  const defaultIcon =
    type === FallBackEnums.ERROR ? (
      <CloseCircleOutlined
        className={classNames(styles.colorRed, styles.icon)}
      />
    ) : type === FallBackEnums.WARNING ? (
      <WarningOutlined
        className={classNames(styles.colorYellow, styles.icon)}
      />
    ) : (
      <InfoCircleOutlined
        className={classNames(styles.colorBlue, styles.icon)}
      />
    );
  return (
    <Flex
      justify="center"
      align="center"
      vertical
      className={classNames(styles.fallback, className)}
    >
      <Text {...textProps}>{children}</Text>
      {icon ? icon : defaultIcon}
      {retry && (
        <Text className="text-link" onClick={retry}>
          Retry
        </Text>
      )}
    </Flex>
  );
};

export default FallBack;
