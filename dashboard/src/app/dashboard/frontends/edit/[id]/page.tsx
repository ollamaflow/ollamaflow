"use client";
import React from "react";
import { useParams, useRouter } from "next/navigation";
import { message } from "antd";
import PageContainer from "#/components/base/pageContainer/PageContainer";
import CreateEditFrontend from "#/page/create-frontend/CreateEditFrontend";
import {
  useEditFrontendMutation,
  EditFrontendPayload,
  useGetFrontendByIdQuery,
} from "#/lib/store/slice/apiSlice";
import OllamaFlowFlex from "#/components/base/flex/Flex";
import OllamaFlowText from "#/components/base/typograpghy/Text";
import OllamaFlowButton from "#/components/base/button/Button";
import { ArrowLeftOutlined } from "@ant-design/icons";
import { paths } from "#/constants/constant";

const EditFrontendPage: React.FC = () => {
  const params = useParams();
  const router = useRouter();
  const frontendId = params?.id as string;

  const [editFrontend, { isLoading: isEditLoading }] =
    useEditFrontendMutation();

  // Fetch the specific frontend data
  const {
    data: currentFrontend,
    isLoading: isFrontendLoading,
    isError: isFrontendError,
  } = useGetFrontendByIdQuery(frontendId);

  // Handle form submission for editing
  const handleEditSubmit = async (
    values: Omit<EditFrontendPayload, "Identifier">
  ) => {
    try {
      // Add the identifier to the payload for edit requests
      const editPayload: EditFrontendPayload = {
        ...values,
        Identifier: frontendId,
      };

      await editFrontend(editPayload).unwrap();
      message.success("Frontend updated successfully");
      router.push(paths.DashboardFrontends);
    } catch (error) {
      console.error("Failed to edit frontend:", error);
      message.error("Failed to update frontend");
    }
  };

  // Handle back navigation
  const handleBack = () => {
    router.push("/dashboard/frontends");
  };

  if (isFrontendLoading) {
    return (
      <PageContainer>
        <OllamaFlowFlex
          justify="center"
          align="center"
          style={{ height: "400px" }}
        >
          <OllamaFlowText>Loading frontend data...</OllamaFlowText>
        </OllamaFlowFlex>
      </PageContainer>
    );
  }

  if (isFrontendError || !currentFrontend) {
    return (
      <PageContainer>
        <OllamaFlowFlex
          vertical
          gap={20}
          align="center"
          style={{ height: "400px" }}
        >
          <OllamaFlowText>Frontend not found</OllamaFlowText>
          <OllamaFlowButton onClick={handleBack}>
            <ArrowLeftOutlined /> Back to Frontends
          </OllamaFlowButton>
        </OllamaFlowFlex>
      </PageContainer>
    );
  }

  return (
    <PageContainer
      pageTitle={
        <OllamaFlowFlex align="center" gap={10}>
          <OllamaFlowButton
            type="text"
            icon={<ArrowLeftOutlined />}
            onClick={handleBack}
            style={{ padding: 0 }}
          />
          <OllamaFlowText>Edit Frontend: {currentFrontend.Name}</OllamaFlowText>
        </OllamaFlowFlex>
      }
    >
      <CreateEditFrontend
        mode="edit"
        initialValues={currentFrontend}
        onSubmit={handleEditSubmit}
        loading={isEditLoading}
      />
    </PageContainer>
  );
};

export default EditFrontendPage;
