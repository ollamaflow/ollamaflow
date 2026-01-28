import React from "react";
import {
  Form,
  Input,
  Button,
  Space,
  Row,
  Col,
  InputNumber,
  Select,
  Switch,
  Alert,
} from "antd";
import dynamic from "next/dynamic";
import {
  CreateFrontendPayload,
  useGetBackendQuery,
} from "#/lib/store/slice/apiSlice";
import { generateSelectOptionsWithFormatter } from "#/utils/utils";
import PageLoading from "#/components/base/loading/PageLoading";

// Dynamically import JsonEditor to avoid SSR issues
const JsonEditor = dynamic(
  () => import("jsoneditor-react").then((mod) => mod.JsonEditor),
  {
    ssr: false,
    loading: () => <PageLoading message="Loading JSON Editor..." />,
  }
) as any;

const { Option } = Select;

interface CreateEditFrontendProps {
  mode?: "create" | "edit";
  initialValues?: Partial<CreateFrontendPayload>;
  onSubmit?: (values: CreateFrontendPayload) => void;
  loading?: boolean;
}

const CreateEditFrontend: React.FC<CreateEditFrontendProps> = ({
  mode = "create",
  initialValues,
  onSubmit,
  loading = false,
}) => {
  const [form] = Form.useForm();
  const [logRequestFull, setLogRequestFull] = React.useState(false);
  const [logRequestBody, setLogRequestBody] = React.useState(false);
  const [logResponseBody, setLogResponseBody] = React.useState(false);

  // Sync state with initial values
  React.useEffect(() => {
    if (initialValues) {
      setLogRequestFull(initialValues.LogRequestFull || false);
      setLogRequestBody(initialValues.LogRequestBody || false);
      setLogResponseBody(initialValues.LogResponseBody || false);
    }
  }, [initialValues]);

  // Fetch backends for the Backend Identifiers field
  const {
    data: backends = [],
    isLoading: backendsLoading,
    isError: backendsError,
  } = useGetBackendQuery();

  // Default values for a new frontend
  const defaultValues: Partial<CreateFrontendPayload> = {
    Hostname: "localhost",
    TimeoutMs: 60000,
    LoadBalancing: "RoundRobin",
    BlockHttp10: true,
    AllowRetries: false,
    LogRequestFull: false,
    LogRequestBody: false,
    LogResponseBody: false,
    MaxRequestBodySize: 536870912,
    Backends: [],
    RequiredModels: ["all-minilm"],
    UseStickySessions: false,
    StickySessionExpirationMs: 1800000,
    PinnedEmbeddingsProperties: {},
    PinnedCompletionsProperties: {},
    AllowEmbeddings: true,
    AllowCompletions: true,
  };

  const handleSubmit = (values: CreateFrontendPayload) => {
    console.log("Frontend form values:", values);
    onSubmit?.(values);
  };

  const isEditMode = mode === "edit";

  return (
    <Form
      form={form}
      layout="vertical"
      onFinish={handleSubmit}
      initialValues={initialValues || defaultValues}
      autoComplete="off"
    >
      {/* Basic Information */}
      <Row gutter={16}>
        <Col xs={24} sm={24} md={24} lg={12} xl={12}>
          <Form.Item
            label="Identifier"
            name="Identifier"
            rules={[
              { required: true, message: "Please enter an identifier" },
              {
                min: 2,
                message: "Identifier must be at least 2 characters",
              },
              {
                pattern: /^\S*$/,
                message: "Identifier cannot contain spaces",
              },
            ]}
          >
            <Input placeholder="e.g., frontend" disabled={isEditMode} />
          </Form.Item>
        </Col>
        <Col xs={24} sm={24} md={24} lg={12} xl={12}>
          <Form.Item
            label="Name"
            name="Name"
            rules={[
              { required: true, message: "Please enter a frontend name" },
              { min: 2, message: "Name must be at least 2 characters" },
            ]}
          >
            <Input placeholder="e.g., Default Ollama frontend" />
          </Form.Item>
        </Col>
      </Row>

      {/* Network Configuration */}
      <Row gutter={16}>
        <Col xs={24} sm={24} md={24} lg={12} xl={12}>
          <Form.Item
            label="Hostname"
            name="Hostname"
            rules={[{ required: true, message: "Please enter a hostname" }]}
          >
            <Input placeholder="localhost" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={24} md={24} lg={12} xl={12}>
          <Form.Item
            label="Timeout (ms)"
            name="TimeoutMs"
            tooltip="The number of milliseconds that must pass before a proxied request is considered timed out"
            rules={[
              { required: true, message: "Please enter a timeout value" },
            ]}
          >
            <InputNumber
              placeholder="60000"
              style={{ width: "100%" }}
              min={1000}
              max={300000}
            />
          </Form.Item>
        </Col>
      </Row>

      {/* Load Balancing and Request Size */}
      <Row gutter={16}>
        <Col xs={24} sm={24} md={24} lg={12} xl={12}>
          <Form.Item
            label="Load Balancing"
            name="LoadBalancing"
            rules={[
              {
                required: true,
                message: "Please select a load balancing method",
              },
            ]}
          >
            <Select placeholder="Select load balancing method">
              <Option value="RoundRobin">Round Robin</Option>
              <Option value="LeastConnections">Least Connections</Option>
              <Option value="Weighted">Weighted</Option>
            </Select>
          </Form.Item>
        </Col>
        <Col xs={24} sm={24} md={24} lg={12} xl={12}>
          <Form.Item
            label="Max Request Body Size"
            name="MaxRequestBodySize"
            tooltip="The maximum request body size allowed before a request will be denied"
            rules={[
              {
                required: true,
                message: "Please enter max request body size",
              },
            ]}
          >
            <InputNumber
              placeholder="536870912"
              style={{ width: "100%" }}
              min={1024}
              max={1073741824}
            />
          </Form.Item>
        </Col>
      </Row>

      {/* Boolean Switches */}
      <Row gutter={16}>
        <Col xs={24} sm={24} md={6} lg={6} xl={6}>
          <Form.Item
            label="Allow Retries"
            name="AllowRetries"
            valuePropName="checked"
            tooltip="Allow the frontend to retry failed upstream requests"
          >
            <Switch />
          </Form.Item>
        </Col>
        <Col xs={24} sm={24} md={6} lg={6} xl={6}>
          <Form.Item
            label="Log Full Request"
            name="LogRequestFull"
            valuePropName="checked"
            tooltip="Indicates whether or not the full request should be logged to syslog; useful for debugging purposes, but implicitly disables response streaming"
          >
            <Switch onChange={(checked) => setLogRequestFull(checked)} />
          </Form.Item>
        </Col>
        <Col xs={24} sm={24} md={6} lg={6} xl={6}>
          <Form.Item
            label="Log Request Body"
            name="LogRequestBody"
            valuePropName="checked"
            tooltip="Indicates whether or not the request body should be logged to syslog; useful for debugging purposes, but implicitly disables response streaming"
          >
            <Switch onChange={(checked) => setLogRequestBody(checked)} />
          </Form.Item>
        </Col>
        <Col xs={24} sm={24} md={6} lg={6} xl={6}>
          <Form.Item
            label="Log Response Body"
            name="LogResponseBody"
            valuePropName="checked"
            tooltip="Indicates whether or not the response body should be logged to syslog; useful for debugging purposes, but implicitly disables response streaming"
          >
            <Switch onChange={(checked) => setLogResponseBody(checked)} />
          </Form.Item>
        </Col>
      </Row>

      {/* Sticky Session Configuration */}
      <Row gutter={16}>
        <Col xs={12} sm={24} md={6} lg={6} xl={6}>
          <Form.Item
            label="Use Sticky Sessions"
            name="UseStickySessions"
            valuePropName="checked"
            tooltip="Setting to true will cause all requests from a given source to be pinned to the selected Ollama backend"
          >
            <Switch />
          </Form.Item>
        </Col>
        <Col xs={24} sm={24} md={18} lg={18} xl={18}>
          <Form.Item
            label="Sticky Session Expiration (ms)"
            name="StickySessionExpirationMs"
            rules={[
              {
                required: true,
                message: "Please enter sticky session expiration time",
              },
            ]}
            tooltip="How long to keep a source pinned to a selected Ollama backend without any activity (default: 30 minutes)"
          >
            <InputNumber
              placeholder="1800000"
              style={{ width: "100%" }}
              min={60000}
              max={3600000}
            />
          </Form.Item>
        </Col>
      </Row>

      {/* Backend and Models Configuration */}
      <Row gutter={16}>
        <Col span={24}>
          <Form.Item
            label="Backend Identifiers"
            name="Backends"
            tooltip="The backends to which requests will be proxied when a request is received by this frontend"
            rules={[
              {
                required: true,
                message: "Please select at least one backend identifier",
              },
            ]}
          >
            <Select
              mode="multiple"
              placeholder={
                backendsLoading
                  ? "Loading backends..."
                  : backendsError
                  ? "Error loading backends"
                  : "Select backend identifiers"
              }
              style={{ width: "100%" }}
              loading={backendsLoading}
              disabled={backendsLoading || backendsError}
              allowClear
            >
              {generateSelectOptionsWithFormatter(
                backends,
                "Identifier",
                (backend) => `${backend.Name} (${backend.Identifier})`
              )}
            </Select>
          </Form.Item>
        </Col>
      </Row>

      <Row gutter={16}>
        <Col span={24}>
          <Form.Item
            label="Required Models"
            name="RequiredModels"
            tooltip="For Ollama backends only, specifies the models that OllamaFlow will ensure are pulled for use"
            rules={[
              {
                required: true,
                message: "Please select at least one required model",
              },
            ]}
          >
            <Select
              mode="tags"
              placeholder="Enter required models (e.g., all-minilm)"
              style={{ width: "100%" }}
            >
              <Option value="all-minilm">all-minilm</Option>
              <Option value="llama2">llama2</Option>
              <Option value="mistral">mistral</Option>
              <Option value="codellama">codellama</Option>
              <Option value="vicuna">vicuna</Option>
            </Select>
          </Form.Item>
        </Col>
      </Row>

      {/* Request Type Permissions */}
      <Row gutter={16}>
        <Col xs={24} sm={24} md={12} lg={12} xl={12}>
          <Form.Item
            label="Allow Embeddings"
            name="AllowEmbeddings"
            valuePropName="checked"
            tooltip="Indicates whether or not embeddings requests are allowed to pass and be processed"
          >
            <Switch />
          </Form.Item>
        </Col>
        <Col xs={24} sm={24} md={12} lg={12} xl={12}>
          <Form.Item
            label="Allow Completions"
            name="AllowCompletions"
            valuePropName="checked"
            tooltip="Indicates whether or not completions requests are allowed to pass and be processed"
          >
            <Switch />
          </Form.Item>
        </Col>
      </Row>

      {/* Pinned Properties Configuration */}
      <Row gutter={16}>
        <Col xs={24} sm={24} md={12} lg={12} xl={12}>
          <Form.Item
            label="Pinned Embeddings Properties"
            name="PinnedEmbeddingsProperties"
            tooltip="A JSON dictionary containing key-value pairs that will be merged into any received embeddings request"
          >
            <JsonEditor
              value={form.getFieldValue("PinnedEmbeddingsProperties") || {}}
              onChange={(value: Record<string, any>) => {
                form.setFieldValue("PinnedEmbeddingsProperties", value);
              }}
              mode="code"
              mainMenuBar={true}
              navigationBar={true}
              statusBar={true}
              style={{ height: "200px" }}
            />
          </Form.Item>
        </Col>
        <Col xs={24} sm={24} md={12} lg={12} xl={12}>
          <Form.Item
            label="Pinned Completions Properties"
            name="PinnedCompletionsProperties"
            tooltip="A JSON dictionary containing key-value pairs that will be merged into any received completions request"
          >
            <JsonEditor
              value={form.getFieldValue("PinnedCompletionsProperties") || {}}
              onChange={(value: Record<string, any>) => {
                form.setFieldValue("PinnedCompletionsProperties", value);
              }}
              mode="code"
              mainMenuBar={true}
              navigationBar={true}
              statusBar={true}
              style={{ height: "200px" }}
            />
          </Form.Item>
        </Col>
      </Row>
      {(logRequestFull || logRequestBody || logResponseBody) && (
        <Alert
          type="warning"
          className="mb mt-sm"
          message={
            <>
              Note: Enabling<strong> Log Full Request</strong> or
              <strong> Log Request Body</strong> or{" "}
              <strong>Log Response Body</strong> will implicitly disable
              response streaming
            </>
          }
        />
      )}
      {/* Submit Buttons */}
      <Form.Item>
        <Space>
          <Button type="primary" htmlType="submit" loading={loading}>
            {isEditMode ? "Update Frontend" : "Create Frontend"}
          </Button>
          <Button onClick={() => form.resetFields()} disabled={loading}>
            Reset
          </Button>
        </Space>
      </Form.Item>
    </Form>
  );
};

export default CreateEditFrontend;
