"use client";

import React, { useState, useEffect, useCallback } from "react";
import PageContainer from "#/components/base/pageContainer/PageContainer";
import {
  ApiExplorerConfig,
  ApiExplorerRequest,
  ApiExplorerResponse,
} from "./components";
import { useApiExplorer } from "#/hooks/useApiExplorer";
import styles from "./api-explorer.module.scss";

export default function ApiExplorerPage() {
  const apiExplorer = useApiExplorer();
  const [localSettings, setLocalSettings] = useState({
    apiType: "ollama",
    requestType: "chat",
    modelName: "llama2",
    streamEnabled: true,
  });

  const handleUrlChange = useCallback((url: string) => {
    apiExplorer.updateBaseUrl(url);
  }, []);

  const handleApiTypeChange = useCallback((apiType: string) => {
    setLocalSettings((prev) => ({ ...prev, apiType }));
  }, []);

  const handleRequestTypeChange = useCallback((requestType: string) => {
    setLocalSettings((prev) => ({ ...prev, requestType }));
    apiExplorer.clearResponse();
  }, []);

  const handleModelChange = useCallback((model: string) => {
    setLocalSettings((prev) => ({ ...prev, modelName: model }));
  }, []);

  const handleStreamToggle = useCallback((stream: boolean) => {
    setLocalSettings((prev) => ({ ...prev, streamEnabled: stream }));
  }, []);

  useEffect(() => {
    apiExplorer.updateRequestBody(
      localSettings.apiType,
      localSettings.requestType,
      localSettings.modelName,
      localSettings.streamEnabled
    );
  }, [localSettings]);

  return (
    <PageContainer pageTitle="API Explorer">
      <div className={styles.container}>
        <ApiExplorerConfig
          onUrlChange={handleUrlChange}
          onApiTypeChange={handleApiTypeChange}
          onRequestTypeChange={handleRequestTypeChange}
          onModelChange={handleModelChange}
          onStreamToggle={handleStreamToggle}
        />

        <div className={styles.mainContent}>
          <ApiExplorerRequest
            requestBody={apiExplorer.requestBody}
            onRequestBodyChange={(body) => {
              apiExplorer.setRequestBodyManual(body);
            }}
            onSendRequest={apiExplorer.sendRequest}
            onStopRequest={apiExplorer.stopRequest}
            isSending={apiExplorer.isSending}
          />

          <ApiExplorerResponse
            responseBody={apiExplorer.responseBody}
            responsePreview={apiExplorer.responsePreview}
            responseHeaders={apiExplorer.responseHeaders}
            responseStatus={apiExplorer.responseStatus}
          />
        </div>
      </div>
    </PageContainer>
  );
}
