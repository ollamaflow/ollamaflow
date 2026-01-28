import React from "react";
import { Row, Col } from "antd";
import {
  PlusOutlined,
  CloudServerOutlined,
  FileTextOutlined,
} from "@ant-design/icons";
import OllamaFlowCard from "../../components/base/card/Card";
import OllamaFlowButton from "../../components/base/button/Button";
import OllamaFlowTitle from "../../components/base/typograpghy/Title";
import OllamaFlowText from "../../components/base/typograpghy/Text";
import OllamaFlowFlex from "#/components/base/flex/Flex";
import styles from "./home.module.scss";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { paths } from "#/constants/constant";
import PageContainer from "#/components/base/pageContainer/PageContainer";

const LandingScreen: React.FC = () => {
  const router = useRouter();
  const handleCreateFrontend = () => {
    router.push(paths.DashboardCreateFrontend);
  };

  const handleCreateBackend = () => {
    router.push(paths.DashboardCreateBackend);
  };

  return (
    <PageContainer>
      <div>
        <OllamaFlowFlex vertical align="center" className="mt-xl">
          <OllamaFlowTitle level={1}>Welcome to OllamaFlow</OllamaFlowTitle>
          <OllamaFlowText>
            Create and manage your Ollama frontends and backends with our
            intuitive interface
          </OllamaFlowText>
        </OllamaFlowFlex>

        {/* Main Options */}
        <Row gutter={[40, 40]} justify="center" className="mt-xl">
          <Col xs={24} sm={20} md={10} lg={8}>
            <OllamaFlowCard
              onClick={handleCreateFrontend}
              className={styles.card}
              hoverable
            >
              <OllamaFlowFlex vertical align="center" className="h-100">
                <div>
                  <FileTextOutlined
                    style={{
                      fontSize: "32px",
                      color: "var(--ant-color-primary)",
                    }}
                  />
                </div>
                <OllamaFlowTitle level={2} className="mt">
                  Create Frontend
                </OllamaFlowTitle>
                <OllamaFlowText className="text-center">
                  Create a virtual Ollama server exposed to your network, mapped
                  to your backends and with models that should be automatically
                  deployed across your fleet.
                </OllamaFlowText>
                <Link href={paths.DashboardCreateFrontend} className="mt-auto">
                  <OllamaFlowButton
                    type="primary"
                    size="large"
                    icon={<PlusOutlined />}
                  >
                    Create Frontend
                  </OllamaFlowButton>
                </Link>
              </OllamaFlowFlex>
            </OllamaFlowCard>
          </Col>

          <Col xs={24} sm={20} md={10} lg={8}>
            <OllamaFlowCard
              hoverable
              onClick={handleCreateBackend}
              className={styles.card}
            >
              <OllamaFlowFlex vertical align="center" className="h-100">
                <div>
                  <CloudServerOutlined
                    style={{
                      fontSize: "40px",
                      color: "var(--ant-color-primary)",
                    }}
                  />
                </div>
                <OllamaFlowTitle level={2} className="mt">
                  Create Backend
                </OllamaFlowTitle>
                <OllamaFlowText className="text-center">
                  Define the backend physical virtual Ollama servers that are
                  workers for your frontend virtual Ollama servers.
                </OllamaFlowText>
                <Link href={paths.DashboardCreateBackend} className="mt-auto">
                  <OllamaFlowButton
                    type="primary"
                    size="large"
                    icon={<PlusOutlined />}
                  >
                    Create Backend
                  </OllamaFlowButton>
                </Link>
              </OllamaFlowFlex>
            </OllamaFlowCard>
          </Col>
        </Row>
      </div>
    </PageContainer>
  );
};

export default LandingScreen;
