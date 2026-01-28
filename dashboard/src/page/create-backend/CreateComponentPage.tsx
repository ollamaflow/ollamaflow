import React from "react";
import { message } from "antd";
import { useRouter } from "next/navigation";
import PageContainer from "#/components/base/pageContainer/PageContainer";
import CreateEditBackend from "./CreateEditBackend";
import OllamaFlowText from "#/components/base/typograpghy/Text";
import {
  useCreateBackendMutation,
  CreateBackendPayload,
} from "#/lib/store/slice/apiSlice";
import OllamaFlowFlex from "#/components/base/flex/Flex";
import { paths } from "#/constants/constant";

const CreateComponentPage: React.FC = () => {
  const router = useRouter();
  const [createBackend, { isLoading }] = useCreateBackendMutation();

  const handleSubmit = async (values: CreateBackendPayload) => {
    try {
      console.log("Creating backend with values:", values);

      const result = await createBackend(values).unwrap();

      console.log("Backend created successfully:", result);
      message.success("Backend created successfully!");

      // Navigate to the backends listing page
      router.push("/dashboard/backends");
    } catch (error: any) {
      console.error("Error creating backend:", error);

      // Handle specific error messages from the API
      const errorMessage =
        error?.data?.message ||
        error?.message ||
        "Failed to create backend. Please try again.";
      message.error(errorMessage);
    }
  };

  return (
    <PageContainer
      navBackUrl={paths.DashboardBackends}
      pageTitle={
        <OllamaFlowFlex vertical>
          <OllamaFlowText>Create Backend</OllamaFlowText>
          <OllamaFlowText className="text-secondary mt-0">
            The backend physical virtual Ollama servers that are workers for
            your frontend virtual Ollama servers.
          </OllamaFlowText>
        </OllamaFlowFlex>
      }
    >
      <CreateEditBackend
        mode="create"
        onSubmit={handleSubmit}
        loading={isLoading}
      />
    </PageContainer>
  );
};

export default CreateComponentPage;
