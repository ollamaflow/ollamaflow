/* eslint-disable react-hooks/exhaustive-deps */
"use client";
import React, { useState, useEffect } from "react";

import { ReloadOutlined, PlusOutlined } from "@ant-design/icons";
import PageContainer from "#/components/base/pageContainer/PageContainer";
import OllamaFlowTable from "#/components/base/table/Table";
import FallBack from "#/components/fallback/FallBack";
import {
  useGetFrontendQuery,
  useDeleteFrontendMutation,
} from "#/lib/store/slice/apiSlice";
import { columns } from "./constants";
import OllamaFlowFlex from "#/components/base/flex/Flex";
import OllamaFlowText from "#/components/base/typograpghy/Text";
import OllamaFlowButton from "#/components/base/button/Button";
import { TableColumnType, Modal, message } from "antd";
import { Frontend } from "#/lib/store/slice/types";
import { useRouter } from "next/navigation";
import { paths } from "#/constants/constant";

const FrontendsListingPage: React.FC = () => {
  const router = useRouter();
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [deletingFrontend, setDeletingFrontend] = useState<Frontend | null>(
    null
  );

  const {
    data: frontends = [],
    isLoading,
    isFetching,
    isError,
    error,
    refetch,
  } = useGetFrontendQuery();
  const isFrontendLoading = isLoading || isFetching;

  const [deleteFrontend, { isLoading: isDeleteLoading }] =
    useDeleteFrontendMutation();

  // Handle create frontend navigation
  const handleCreateFrontend = () => {
    router.push(paths.DashboardCreateFrontend);
  };

  // Handle delete frontend
  const handleDeleteFrontend = (frontend: Frontend) => {
    setDeletingFrontend(frontend);
    setIsDeleteModalVisible(true);
  };

  // Handle delete confirmation
  const handleDeleteConfirm = async () => {
    if (!deletingFrontend) return;

    try {
      await deleteFrontend(deletingFrontend.Identifier).unwrap();
      message.success("Frontend deleted successfully");
      setIsDeleteModalVisible(false);
      setDeletingFrontend(null);
      // Refresh the frontends list
      refetch();
    } catch (error) {
      console.error("Failed to delete frontend:", error);
      message.error("Failed to delete frontend");
    }
  };

  // Handle modal close
  const handleDeleteModalClose = () => {
    setIsDeleteModalVisible(false);
    setDeletingFrontend(null);
  };
  if (isError) {
    return <FallBack retry={refetch} error={error} />;
  }

  return (
    <PageContainer
      pageTitle={
        <OllamaFlowFlex align="center" gap={10}>
          <OllamaFlowText>Frontends</OllamaFlowText>
          <ReloadOutlined onClick={refetch} title="Refresh" />
        </OllamaFlowFlex>
      }
      pageTitleRightContent={
        <OllamaFlowButton
          type="primary"
          variant="link"
          icon={<PlusOutlined />}
          onClick={handleCreateFrontend}
        >
          Create Frontend
        </OllamaFlowButton>
      }
    >
      <OllamaFlowTable
        columns={
          columns(handleDeleteFrontend) as TableColumnType<Partial<Frontend>>[]
        }
        dataSource={frontends}
        loading={isFrontendLoading}
        rowKey="Identifier"
        scroll={{ x: 1600 }}
        pagination={{
          showSizeChanger: true,
          showQuickJumper: true,
          showTotal: (total, range) =>
            `${range[0]}-${range[1]} of ${total} frontends`,
        }}
        size="middle"
      />

      {/* Delete Confirmation Modal */}
      <Modal
        title="Delete Frontend"
        open={isDeleteModalVisible}
        onOk={handleDeleteConfirm}
        onCancel={handleDeleteModalClose}
        confirmLoading={isDeleteLoading}
        okText="Delete"
        cancelText="Cancel"
        okButtonProps={{ danger: true }}
      >
        {deletingFrontend && (
          <p>
            Are you sure you want to delete the frontend{" "}
            <strong>&ldquo;{deletingFrontend.Name}&rdquo;</strong> (
            {deletingFrontend.Identifier})?
          </p>
        )}
      </Modal>
    </PageContainer>
  );
};

export default FrontendsListingPage;
