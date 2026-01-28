import React from "react";
import classNames from "classnames";
import OllamaFlowFlex from "../flex/Flex";
import OllamaFlowText from "../typograpghy/Text";
import styles from "./pageContainer.module.scss";
import { Content } from "antd/es/layout/layout";
import { ArrowLeftOutlined } from "@ant-design/icons";
import { useRouter } from "next/navigation";

const PageContainer = ({
  children,
  className,
  withoutWhiteBG = false,
  id,
  pageTitle,
  pageTitleRightContent,
  navBackUrl,
}: {
  children: React.ReactNode;
  className?: string;
  withoutWhiteBG?: boolean;
  id?: string;
  pageTitle?: React.ReactNode | string;
  pageTitleRightContent?: React.ReactNode;
  showGraphSelector?: boolean;
  navBackUrl?: string;
}) => {
  const router = useRouter();
  return (
    <Content
      className={classNames(className, !withoutWhiteBG && styles.pageContainer)}
      id={id}
    >
      {(pageTitle || pageTitleRightContent) && (
        <>
          <OllamaFlowFlex
            className={styles.pageTitleContainer}
            wrap
            gap="small"
            align="center"
            justify="space-between"
          >
            <OllamaFlowText fontSize={16} weight={600} data-testid="heading">
              <OllamaFlowFlex gap={10} align="center">
                {navBackUrl && (
                  <ArrowLeftOutlined onClick={() => router.push(navBackUrl)} />
                )}
                {pageTitle}
              </OllamaFlowFlex>
            </OllamaFlowText>
            {pageTitleRightContent}
          </OllamaFlowFlex>
        </>
      )}
      <div className={styles.pageContent}>{children}</div>
    </Content>
  );
};

export default PageContainer;
