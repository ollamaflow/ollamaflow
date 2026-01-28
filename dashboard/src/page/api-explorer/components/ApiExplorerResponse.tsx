"use client";

import React, { useEffect, useRef } from "react";
import OllamaFlowCard from "#/components/base/card/Card";
import OllamaFlowSpace from "#/components/base/space/Space";
import { Tabs } from "antd";
import styles from "./apiExplorerResponse.module.scss";

interface StatusInfo {
  status: number;
  statusText: string;
  requestTime: number;
  type: "success" | "error" | "cancelled";
  timeToFirstToken?: number;
  totalStreamingTime?: number;
}

interface ApiExplorerResponseProps {
  responseBody: string;
  responsePreview: string;
  responseHeaders: string;
  responseStatus: StatusInfo | null;
}

export default function ApiExplorerResponse({
  responseBody,
  responsePreview,
  responseHeaders,
  responseStatus,
}: ApiExplorerResponseProps) {
  const previewRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (previewRef.current) {
      previewRef.current.scrollTop = previewRef.current.scrollHeight;
    }
  }, [responsePreview]);

  const renderMarkdownPreview = (content: string) => {
    try {
      // This is a simplified markdown renderer - in production you might want to use a proper markdown library
      const html = content
        .replace(/^### (.*$)/gim, "<h3>$1</h3>")
        .replace(/^## (.*$)/gim, "<h2>$1</h2>")
        .replace(/^# (.*$)/gim, "<h1>$1</h1>")
        .replace(/\*\*(.*?)\*\*/gim, "<strong>$1</strong>")
        .replace(/\*(.*?)\*/gim, "<em>$1</em>")
        .replace(/```([\s\S]*?)```/gim, "<pre><code>$1</code></pre>")
        .replace(/`([^`]*)`/gim, "<code>$1</code>")
        .replace(/\n/gim, "<br>");

      return { __html: html };
    } catch (error) {
      return { __html: content };
    }
  };

  const tabItems = [
    {
      key: "preview",
      label: "Preview",
      children: (
        <div
          ref={previewRef}
          className={styles.responsePreview}
          dangerouslySetInnerHTML={renderMarkdownPreview(responsePreview)}
        />
      ),
    },
    {
      key: "body",
      label: "Body",
      children: <div className={styles.responseDisplay}>{responseBody}</div>,
    },
    {
      key: "headers",
      label: "Headers",
      children: <pre className={styles.responseDisplay}>{responseHeaders}</pre>,
    },
    {
      key: "status",
      label: "Status",
      children: (
        <div className={styles.responseDisplay}>
          {responseStatus && (
            <OllamaFlowSpace
              direction="vertical"
              size="small"
              style={{ width: "100%" }}
            >
              <div className={styles.statusItem}>
                <span className={styles.statusLabel}>Status Code:</span>
                <span
                  className={
                    responseStatus.type === "success"
                      ? styles.statusOk
                      : responseStatus.type === "cancelled"
                      ? ""
                      : styles.statusError
                  }
                >
                  {responseStatus.status} {responseStatus.statusText}
                </span>
              </div>
              <div className={styles.statusItem}>
                <span className={styles.statusLabel}>Request Time:</span>
                <span>{responseStatus.requestTime}ms</span>
              </div>
              {responseStatus.timeToFirstToken !== undefined && (
                <div className={styles.statusItem}>
                  <span className={styles.statusLabel}>
                    Time to First Token:
                  </span>
                  <span>{responseStatus.timeToFirstToken}ms</span>
                </div>
              )}
              {responseStatus.totalStreamingTime !== undefined && (
                <div className={styles.statusItem}>
                  <span className={styles.statusLabel}>
                    Total Streaming Time:
                  </span>
                  <span>{responseStatus.totalStreamingTime}ms</span>
                </div>
              )}
            </OllamaFlowSpace>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className={styles.rightPanel}>
      <OllamaFlowCard title="Response" className={styles.responseCard}>
        <Tabs
          defaultActiveKey="preview"
          items={tabItems}
          className={styles.responseTabs}
        />
      </OllamaFlowCard>
    </div>
  );
}
