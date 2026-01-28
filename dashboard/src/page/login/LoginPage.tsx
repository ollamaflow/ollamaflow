"use client";
import React, { useEffect, useState } from "react";
import { Form, Input, message } from "antd";
import { localStorageKeys } from "#/constants/constant";

import { useAdminCredentialsToLogin } from "#/hooks/authHooks";
import LoginLayout from "#/components/layout/LoginLayout";
import {
  useGetFrontendTestMutation,
  useValidateConnectivityMutation,
} from "#/lib/store/slice/apiSlice";
import OllamaFlowButton from "#/components/base/button/Button";
import {
  changeAxiosBaseUrl,
  setAuthToken,
} from "#/lib/store/rtk/rtkApiInstance";
import { ollamaServerUrl } from "#/constants/apiConfig";
import { forGuest } from "#/hoc/hoc";

interface AdminLoginFormData {
  url: string;
  accessKey: string;
}

const AdminLoginPage = () => {
  const [form] = Form.useForm();
  const [isServerValid, setIsServerValid] = useState(false);
  const loginWithAdminCredentials = useAdminCredentialsToLogin();
  const [getFrontendTest, { isLoading: isGettingFrontendTest }] =
    useGetFrontendTestMutation();
  const [validateConnectivity, { isLoading: isValidating }] =
    useValidateConnectivityMutation();

  const handleValidateServerUrl = async (url: string) => {
    if (!url) return;
    setIsServerValid(false);
    changeAxiosBaseUrl(url);
    try {
      const isValid = await validateConnectivity().unwrap();
      setIsServerValid(!!isValid);
    } catch (error) {
      setIsServerValid(false);
    }
  };

  useEffect(() => {
    changeAxiosBaseUrl(ollamaServerUrl);
    handleValidateServerUrl(ollamaServerUrl);
  }, []);

  const handleSubmit = async (values: AdminLoginFormData) => {
    console.log(values);
    try {
      const data = await validateConnectivity().unwrap();
      if (data) {
        changeAxiosBaseUrl(values.url);
        setAuthToken(values.accessKey);
        try {
          const isValid = await getFrontendTest().unwrap();
          if (isValid) {
            localStorage.setItem(
              localStorageKeys.adminAccessKey,
              values.accessKey
            );
            localStorage.setItem(localStorageKeys.serverUrl, values.url);
            loginWithAdminCredentials(values.accessKey);
          }
        } catch (_error) {}
      }
    } catch (error) {
      message.error("Please try again.");
    }
  };

  return (
    <LoginLayout isAdmin>
      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        autoComplete="off"
        initialValues={{
          url: ollamaServerUrl,
        }}
        onKeyPress={(e) => {
          if (e.key === "Enter") {
            form.submit();
          }
        }}
      >
        <Form.Item
          label="OllamaFlow Server URL"
          name="url"
          rules={[
            {
              required: true,
              message: "Please enter the Ollama Server URL!",
            },
            {
              validator: (_, value) => {
                if (!value) return Promise.resolve();
                try {
                  const parsedUrl = new URL(value);
                  if (!["http:", "https:"].includes(parsedUrl.protocol)) {
                    return Promise.reject(
                      "Only HTTP or HTTPS URLs are allowed!"
                    );
                  }
                  return Promise.resolve();
                } catch (err) {
                  return Promise.reject("Please enter a valid URL!");
                }
              },
            },
          ]}
        >
          <Input
            placeholder="https://your-ollama-server.com"
            onBlur={() =>
              handleValidateServerUrl(form.getFieldValue("url") as string)
            }
            disabled={isValidating}
          />
        </Form.Item>

        <Form.Item
          label="Access Key"
          name="accessKey"
          rules={[{ required: true, message: "Please input your access key!" }]}
        >
          <Input.Password
            key={isServerValid ? "enabled" : "disabled"}
            placeholder="Enter your access key"
            onPressEnter={() => form.submit()}
            disabled={!isServerValid}
            autoFocus
          />
        </Form.Item>

        <Form.Item>
          <OllamaFlowButton
            type="primary"
            htmlType="submit"
            className="w-100"
            loading={isValidating}
            disabled={isValidating}
          >
            Login
          </OllamaFlowButton>
        </Form.Item>
      </Form>
    </LoginLayout>
  );
};

export default forGuest(AdminLoginPage);
