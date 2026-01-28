/* eslint-disable react-hooks/exhaustive-deps */
"use client";
import React, { useState } from "react";

import { ReloadOutlined, PlusOutlined } from "@ant-design/icons";
import PageContainer from "#/components/base/pageContainer/PageContainer";
import OllamaFlowTable from "#/components/base/table/Table";
import FallBack from "#/components/fallback/FallBack";
import {
  useGetBackendHealthQuery,
  useDeleteBackendMutation,
  useGetBackendQuery,
} from "#/lib/store/slice/apiSlice";
import { columns } from "./constants";
import OllamaFlowFlex from "#/components/base/flex/Flex";
import OllamaFlowText from "#/components/base/typograpghy/Text";
import OllamaFlowButton from "#/components/base/button/Button";
import { TableColumnType, Modal, message } from "antd";
import { Backend, BackendHealth } from "#/lib/store/slice/types";
import { useRouter } from "next/navigation";
import { paths } from "#/constants/constant";
import styles from "./be-listing.module.scss";
import { getBackendWithHealth } from "./utils";

const BackendsListingPage: React.FC = () => {
  const router = useRouter();
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [deletingBackend, setDeletingBackend] = useState<
    BackendHealth | Backend | null
  >(null);

  const {
    data: backendsHealth = [],
    isLoading,
    isFetching,
    error,
    isError: isE1,
    refetch: refetchBackendHealth,
  } = useGetBackendHealthQuery();
  const {
    data: backends,
    isLoading: isL1,
    isError,
    isFetching: isF1,
    refetch: refetchBackend,
  } = useGetBackendQuery();
  const isBackendLoading = isL1 || isF1;
  const isBackendHealthLoading = isLoading || isFetching;
  const backendsWithHealth = getBackendWithHealth(backends, backendsHealth);

  const [deleteBackend, { isLoading: isDeleteLoading }] =
    useDeleteBackendMutation();

  // Handle create backend navigation
  const handleCreateBackend = () => {
    router.push(paths.DashboardCreateBackend);
  };
  const refetch = () => {
    refetchBackend();
    refetchBackendHealth();
  };
  // Handle delete backend
  const handleDeleteBackend = (backend: BackendHealth | Backend) => {
    setDeletingBackend(backend);
    setIsDeleteModalVisible(true);
  };

  // Handle delete confirmation
  const handleDeleteConfirm = async () => {
    if (!deletingBackend) return;

    try {
      await deleteBackend(deletingBackend.Identifier).unwrap();
      message.success("Backend deleted successfully");
      setIsDeleteModalVisible(false);
      setDeletingBackend(null);
      // Refresh the backends list
      refetch();
    } catch (error) {
      console.error("Failed to delete backend:", error);
      message.error("Failed to delete backend");
    }
  };

  // Handle modal close
  const handleDeleteModalClose = () => {
    setIsDeleteModalVisible(false);
    setDeletingBackend(null);
  };

  if (isError || isE1) {
    return <FallBack retry={refetch} error={error} />;
  }

  return (
    <PageContainer
      pageTitle={
        <OllamaFlowFlex align="center" gap={10}>
          <OllamaFlowText>Backends</OllamaFlowText>
          <ReloadOutlined onClick={refetch} title="Refresh" />
        </OllamaFlowFlex>
      }
      pageTitleRightContent={
        <OllamaFlowButton
          type="primary"
          variant="link"
          icon={<PlusOutlined />}
          onClick={handleCreateBackend}
        >
          Create Backend
        </OllamaFlowButton>
      }
    >
      <OllamaFlowTable
        className={styles.tableContainer}
        columns={
          columns(handleDeleteBackend) as TableColumnType<Partial<Backend>>[]
        }
        onRow={(record: Partial<Backend>) => {
          return {
            className: !record.Active ? "in-active" : undefined,
          };
        }}
        dataSource={backendsWithHealth}
        loading={isBackendLoading || isBackendHealthLoading}
        rowKey="Identifier"
        scroll={{ x: 1600 }}
        pagination={{
          showSizeChanger: true,
          showQuickJumper: true,
          showTotal: (total, range) =>
            `${range[0]}-${range[1]} of ${total} backends`,
        }}
        size="middle"
      />

      {/* Delete Confirmation Modal */}
      <Modal
        title="Delete Backend"
        open={isDeleteModalVisible}
        onOk={handleDeleteConfirm}
        onCancel={handleDeleteModalClose}
        confirmLoading={isDeleteLoading}
        okText="Delete"
        cancelText="Cancel"
        okButtonProps={{ danger: true }}
      >
        {deletingBackend && (
          <p>
            Are you sure you want to delete the backend{" "}
            <strong>&ldquo;{deletingBackend.Name}&rdquo;</strong> (
            {deletingBackend.Identifier})?
          </p>
        )}
      </Modal>
    </PageContainer>
  );
};

export default BackendsListingPage;
