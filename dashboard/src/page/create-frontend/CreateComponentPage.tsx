import React from "react";
import { message } from "antd";
import { useRouter } from "next/navigation";
import PageContainer from "#/components/base/pageContainer/PageContainer";
import CreateEditFrontend from "./CreateEditFrontend";
import OllamaFlowText from "#/components/base/typograpghy/Text";
import {
  useCreateFrontendMutation,
  CreateFrontendPayload,
} from "#/lib/store/slice/apiSlice";
import OllamaFlowFlex from "#/components/base/flex/Flex";
import { paths } from "#/constants/constant";

const CreateComponentPage: React.FC = () => {
  const router = useRouter();
  const [createFrontend, { isLoading, error }] = useCreateFrontendMutation();

  const handleSubmit = async (values: CreateFrontendPayload) => {
    try {
      console.log("Creating frontend with values:", values);

      const result = await createFrontend(values).unwrap();

      console.log("Frontend created successfully:", result);
      message.success("Frontend created successfully!");

      // Navigate to the frontends listing page
      router.push("/dashboard/frontends");
    } catch (error: any) {
      console.error("Error creating frontend:", error);

      // Handle specific error messages from the API
      const errorMessage =
        error?.data?.message ||
        error?.message ||
        "Failed to create frontend. Please try again.";
      message.error(errorMessage);
    }
  };

  return (
    <PageContainer
      navBackUrl={paths.DashboardFrontends}
      pageTitle={
        <OllamaFlowFlex vertical>
          <OllamaFlowText>Create Frontend</OllamaFlowText>
          <OllamaFlowText className="text-secondary mt-0">
            A virtual Ollama server exposed to your network, mapped to your
            backends and with models that should be automatically deployed
            across your fleet.
          </OllamaFlowText>
        </OllamaFlowFlex>
      }
    >
      <CreateEditFrontend
        mode="create"
        onSubmit={handleSubmit}
        loading={isLoading}
      />
    </PageContainer>
  );
};

export default CreateComponentPage;
