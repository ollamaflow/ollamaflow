import { ColumnsType } from "antd/es/table";
import { BackendHealth } from "#/lib/store/slice/types";
import { Tag, Popover } from "antd";
import OllamaFlowTooltip from "#/components/base/tooltip/Tooltip";
import {
  EditOutlined,
  DeleteOutlined,
  InfoCircleOutlined,
} from "@ant-design/icons";
import OllamaFlowFlex from "#/components/base/flex/Flex";
import Link from "next/link";
import { paths } from "../../constants/constant";
import { BackendWithHealth } from "./types";
import BackendHealthInfo from "./components/BackendHealthInfo";
import { formatDate } from "#/utils/utils";

export const columns: (
  deleteBackendHandler: (backend: BackendWithHealth) => void
) => ColumnsType<BackendWithHealth> = (deleteBackendHandler) => [
  {
    title: (
      <OllamaFlowTooltip title="A unique identifier for the backend">
        Identifier
      </OllamaFlowTooltip>
    ),
    dataIndex: "Identifier",
    key: "Identifier",
    width: 150,
    render: (text: string) => (
      <OllamaFlowTooltip title={text}>{text}</OllamaFlowTooltip>
    ),
  },
  {
    title: (
      <OllamaFlowTooltip title="The name of the backend">
        Name
      </OllamaFlowTooltip>
    ),
    dataIndex: "Name",
    key: "Name",
    width: 200,
  },
  {
    title: (
      <OllamaFlowTooltip title="Custom labels for the backend">
        Labels
      </OllamaFlowTooltip>
    ),
    dataIndex: "Labels",
    key: "Labels",
    width: 300,
    render: (labels: string[]) => {
      if (!labels || labels.length === 0) {
        return <Tag color="default">No labels</Tag>;
      }

      const maxVisibleLabels = 6;
      const visibleLabels = labels.slice(0, maxVisibleLabels);
      const remainingCount = labels.length - maxVisibleLabels;

      return (
        <div style={{ display: "flex", flexWrap: "wrap", gap: "4px" }}>
          {visibleLabels.map((label, index) => (
            <Tag key={index} color="blue">
              {label}
            </Tag>
          ))}
          {remainingCount > 0 && (
            <OllamaFlowTooltip
              title={labels.slice(maxVisibleLabels).join(", ")}
            >
              <Tag color="default">... ({remainingCount} more)</Tag>
            </OllamaFlowTooltip>
          )}
        </div>
      );
    },
  },
  {
    title: (
      <OllamaFlowTooltip title="The hostname and port to which requests will be proxied">
        Hostname:Port
      </OllamaFlowTooltip>
    ),
    dataIndex: "Hostname",
    key: "Hostname",
    width: 150,
    render: (text: string, record: BackendWithHealth) => (
      <span>
        {text}:{record.Port}
      </span>
    ),
  },
  {
    title: (
      <OllamaFlowTooltip title="Indicates whether SSL is enabled or disabled">
        SSL
      </OllamaFlowTooltip>
    ),
    dataIndex: "Ssl",
    key: "Ssl",
    width: 80,
    render: (ssl: boolean) => (
      <Tag color={ssl ? "success" : "error"}>{ssl ? "Yes" : "No"}</Tag>
    ),
  },
  {
    title: (
      <OllamaFlowTooltip title="The expected format of API requests expected by this backend">
        API Format
      </OllamaFlowTooltip>
    ),
    dataIndex: "ApiFormat",
    key: "ApiFormat",
    width: 100,
    render: (apiFormat: string) => (
      <Tag color={apiFormat === "Ollama" ? "blue" : "green"}>
        {apiFormat || "Ollama"}
      </Tag>
    ),
  },
  {
    title: (
      <OllamaFlowTooltip title="Bearer token forwarded with proxied requests (optional)">
        Bearer Token
      </OllamaFlowTooltip>
    ),
    dataIndex: "BearerToken",
    key: "BearerToken",
    width: 180,
    render: (token?: string) =>
      token ? <Tag>{token}</Tag> : <Tag color="default">Not set</Tag>,
  },
  {
    title: (
      <OllamaFlowTooltip title="Querystring appended to requests (optional)">
        Querystring
      </OllamaFlowTooltip>
    ),
    dataIndex: "Querystring",
    key: "Querystring",
    width: 200,
    render: (query?: string) =>
      query ? <Tag>{query}</Tag> : <Tag color="default">Not set</Tag>,
  },
  {
    title: (
      <OllamaFlowTooltip title="Custom headers applied to backend requests">
        Headers
      </OllamaFlowTooltip>
    ),
    dataIndex: "Headers",
    key: "Headers",
    width: 220,
    render: (headers?: Record<string, string>) => {
      if (!headers || Object.keys(headers).length === 0) {
        return <Tag color="default">None</Tag>;
      }
      const entries = Object.entries(headers);
      const maxVisibleHeaders = 3;
      const visible = entries.slice(0, maxVisibleHeaders);
      const remaining = entries.length - maxVisibleHeaders;
      return (
        <OllamaFlowFlex style={{ flexWrap: "wrap", gap: 4 }}>
          {visible.map(([key, value]) => (
            <Tag key={key}>{`${key}: ${value}`}</Tag>
          ))}
          {remaining > 0 && (
            <OllamaFlowTooltip
              title={entries
                .slice(maxVisibleHeaders)
                .map(([k, v]) => `${k}: ${v}`)
                .join(", ")}
            >
              <Tag color="default">... ({remaining} more)</Tag>
            </OllamaFlowTooltip>
          )}
        </OllamaFlowFlex>
      );
    },
  },
  {
    title: (
      <OllamaFlowTooltip title="Indicates whether or not the backend is active or administratively disabled">
        Status
      </OllamaFlowTooltip>
    ),
    dataIndex: "Active",
    key: "Active",
    width: 100,
    render: (active: boolean) => (
      <Tag color={active ? "success" : "error"}>
        {active ? "Active" : "Inactive"}
      </Tag>
    ),
  },
  {
    title: (
      <OllamaFlowTooltip title="Health of the backend">
        Health
      </OllamaFlowTooltip>
    ),
    dataIndex: "health",
    key: "health",
    width: 150,
    render: (health: BackendHealth) => {
      return (
        <Popover
          placement="left"
          content={<BackendHealthInfo health={health} />}
        >
          <Tag color={health.HealthySinceUtc ? "success" : "error"}>
            {health.HealthySinceUtc ? "Healthy" : "Unhealthy"}{" "}
          </Tag>
          <InfoCircleOutlined />
        </Popover>
      );
    },
  },
  {
    title: (
      <OllamaFlowTooltip title="Timestamp from which the backend was created">
        Created
      </OllamaFlowTooltip>
    ),
    dataIndex: "CreatedUtc",
    key: "CreatedUtc",
    width: 150,
    render: (date: string) => {
      return formatDate(date);
    },
  },
  {
    title: "Actions",
    key: "actions",
    width: 120,
    render: (record: BackendWithHealth) => (
      <OllamaFlowFlex align="center" justify="space-around">
        <Link href={`${paths.EditBackend}/${record.Identifier}`}>
          <EditOutlined
            style={{ cursor: "pointer", color: "#1890ff" }}
            title="Edit Backend"
          />{" "}
        </Link>
        <DeleteOutlined
          style={{ cursor: "pointer", color: "#ff4d4f" }}
          title="Delete Backend"
          onClick={() => {
            // This will be handled by the parent component
            deleteBackendHandler(record);
          }}
        />
      </OllamaFlowFlex>
    ),
  },
];
