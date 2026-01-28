import React from "react";
import { Layout, Menu, Button } from "antd";
import {
  FileTextOutlined,
  HomeOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  CloudServerOutlined,
  ApiOutlined,
} from "@ant-design/icons";
import "../../../assets/css/globals.scss";
import OllamaFlowFlex from "../flex/Flex";
import styles from "./sidebar.module.scss";
import OllamaFlowText from "../typograpghy/Text";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { paths } from "#/constants/constant";

const { Sider } = Layout;

interface SidebarProps {
  collapsed?: boolean;
  onCollapse?: (collapsed: boolean) => void;
}

const Sidebar: React.FC<SidebarProps> = ({ collapsed = false, onCollapse }) => {
  const pathname = usePathname();

  // Function to determine the active key based on current pathname
  const getActiveKey = () => {
    if (pathname === paths.Dashboard) {
      return "home";
    } else if (pathname.startsWith(paths.DashboardFrontends)) {
      return "frontends";
    } else if (pathname.startsWith(paths.DashboardBackends)) {
      return "backends";
    } else if (pathname.startsWith(paths.DashboardApiExplorer)) {
      return "api-explorer";
    }
    return "home"; // default fallback
  };

  // Create menu items
  const menuItems = [
    {
      key: "home",
      icon: <HomeOutlined />,
      label: <Link href={paths.Dashboard}>Home</Link>,
    },
    {
      key: "frontends",
      icon: <FileTextOutlined />,
      label: <Link href={paths.DashboardFrontends}>Frontends</Link>,
    },
    {
      key: "backends",
      icon: <CloudServerOutlined />,
      label: <Link href={paths.DashboardBackends}>Backends</Link>,
    },
    {
      key: "api-explorer",
      icon: <ApiOutlined />,
      label: <Link href={paths.DashboardApiExplorer}>API Explorer</Link>,
    },
  ];

  return (
    <Sider
      theme="light"
      width={200}
      trigger={null}
      collapsible
      collapsed={collapsed}
      collapsedWidth={60}
      className={styles.sidebarContainer}
    >
      <OllamaFlowFlex
        justify="center"
        gap={8}
        align="center"
        className={styles.logoContainer}
      >
        <OllamaFlowText weight={600} fontSize={20}>
          {collapsed ? (
            <img
              src="/images/ollama-flow-icon.png"
              alt="OllamaFlow"
              height={40}
            />
          ) : (
            <OllamaFlowFlex align="center" gap={7} className="fade-in">
              <img
                src="/images/ollama-flow-icon.png"
                alt="OllamaFlow"
                height={40}
              />
              OllamaFlow
            </OllamaFlowFlex>
          )}
        </OllamaFlowText>
      </OllamaFlowFlex>
      <OllamaFlowFlex
        justify="center"
        className=" mt"
        vertical
        align={collapsed ? "center" : "flex-end"}
        gap={10}
      >
        <Button
          type="text"
          icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
          onClick={() => onCollapse?.(!collapsed)}
          title={collapsed ? "Expand Sidebar" : "Collapse Sidebar"}
        />
      </OllamaFlowFlex>
      <Menu mode="inline" selectedKeys={[getActiveKey()]} items={menuItems} />
    </Sider>
  );
};

export default Sidebar;
