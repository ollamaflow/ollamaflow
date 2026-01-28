import { ColumnsType } from "antd/es/table";
import { Frontend } from "#/lib/store/slice/types";
import { Tag } from "antd";
import OllamaFlowTooltip from "#/components/base/tooltip/Tooltip";
import { EditOutlined, DeleteOutlined } from "@ant-design/icons";
import Link from "next/link";
import OllamaFlowFlex from "#/components/base/flex/Flex";
import { paths } from "../../constants/constant";
import { formatDate } from "#/utils/utils";

export const columns: (
  deleteFrontendHandler: (frontend: Frontend) => void
) => ColumnsType<Frontend> = (deleteFrontendHandler) => [
  {
    title: (
      <OllamaFlowTooltip title="A unique identifier for the frontend">
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
      <OllamaFlowTooltip title="The name of the frontend">
        Name
      </OllamaFlowTooltip>
    ),
    dataIndex: "Name",
    key: "Name",
    width: 200,
  },
  {
    title: (
      <OllamaFlowTooltip title="The hostname on which a received request must match to be routed to the frontend (an asterisk indicates a catch-all hostname)">
        Hostname
      </OllamaFlowTooltip>
    ),
    dataIndex: "Hostname",
    key: "Hostname",
    width: 150,
    render: (text: string, _record: Frontend) => <span>{text}</span>,
  },
  {
    title: (
      <OllamaFlowTooltip title="The load-balancing algorithm used for the backends mapped to this frontend">
        Load Balancing
      </OllamaFlowTooltip>
    ),
    dataIndex: "LoadBalancing",
    key: "LoadBalancing",
    width: 150,
    render: (text: string) => <Tag color="blue">{text}</Tag>,
  },
  {
    title: (
      <OllamaFlowTooltip title="The number of backends mapped to this frontend">
        Backends
      </OllamaFlowTooltip>
    ),
    dataIndex: "Backends",
    key: "Backends",
    width: 100,
    render: (backends: string[]) => <Tag color="green">{backends.length}</Tag>,
  },
  {
    title: (
      <OllamaFlowTooltip title="For Ollama backends, the number of models to synchronize to the backends">
        Required Models
      </OllamaFlowTooltip>
    ),
    dataIndex: "RequiredModels",
    key: "RequiredModels",
    width: 150,
    render: (models: string[]) => (
      <OllamaFlowTooltip title={models.join(", ")}>
        <Tag color="orange">{models.length} models</Tag>
      </OllamaFlowTooltip>
    ),
  },
  {
    title: (
      <OllamaFlowTooltip title="Indicates whether or not session stickiness is enabled, that is, clients are routed to the same backend on subsequent requests">
        Sticky Sessions
      </OllamaFlowTooltip>
    ),
    dataIndex: "UseStickySessions",
    key: "UseStickySessions",
    width: 120,
    render: (useStickySessions: boolean) => (
      <Tag color={useStickySessions ? "blue" : "default"}>
        {useStickySessions ? "Enabled" : "Disabled"}
      </Tag>
    ),
  },
  {
    title: (
      <OllamaFlowTooltip title="Indicates whether or not OllamaFlow will retry requests if any proxied requests to a backend fail with a 500-series error">
        Allow Retries
      </OllamaFlowTooltip>
    ),
    dataIndex: "AllowRetries",
    key: "AllowRetries",
    width: 120,
    render: (allowRetries: boolean) => (
      <Tag color={allowRetries ? "blue" : "default"}>
        {allowRetries ? "Enabled" : "Disabled"}
      </Tag>
    ),
  },
  {
    title: (
      <OllamaFlowTooltip title="The length of time a client is pinned to a backend before being proxied to a new backend">
        Sticky Session Expiration (ms)
      </OllamaFlowTooltip>
    ),
    dataIndex: "StickySessionExpirationMs",
    key: "StickySessionExpirationMs",
    width: 180,
    render: (expirationMs: number) => {
      const minutes = Math.round(expirationMs / 1000 / 60);
      return (
        <span>
          {expirationMs.toLocaleString()}ms ({minutes} min)
        </span>
      );
    },
  },
  {
    title: (
      <OllamaFlowTooltip title="Indicates whether or not the frontend is active or administratively disabled">
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
      <OllamaFlowTooltip title="Timestamp from which the frontend was created">
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
    render: (record: Frontend) => (
      <OllamaFlowFlex align="center" justify="space-around">
        <Link href={`${paths.EditFrontend}/${record.Identifier}`}>
          <EditOutlined
            style={{ cursor: "pointer", color: "#1890ff" }}
            title="Edit Frontend"
          />
        </Link>
        <DeleteOutlined
          style={{ cursor: "pointer", color: "#ff4d4f" }}
          title="Delete Frontend"
          onClick={() => {
            // This will be handled by the parent component
            deleteFrontendHandler(record);
          }}
        />
      </OllamaFlowFlex>
    ),
  },
];
