import React from "react";
import { LoadingOutlined } from "@ant-design/icons";
import styles from "./pageLoding.module.scss";
import OllamaFlowText from "../typograpghy/Text";
import OllamaFlowFlex from "../flex/Flex";
import PageContainer from "../pageContainer/PageContainer";

const PageLoading = ({
  message = "Loading...",
  withoutWhiteBG = false,
  className,
}: {
  message?: string;
  withoutWhiteBG?: boolean;
  className?: string;
}) => {
  return (
    <PageContainer withoutWhiteBG={withoutWhiteBG} className={className}>
      <OllamaFlowFlex justify="center" align="center" vertical>
        <OllamaFlowText data-testid="loading-message">{message}</OllamaFlowText>
        <LoadingOutlined className={styles.pageLoader} />
      </OllamaFlowFlex>
    </PageContainer>
  );
};

export default PageLoading;
