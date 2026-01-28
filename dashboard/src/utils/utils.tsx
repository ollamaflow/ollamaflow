import React from "react";
import { Select } from "antd";
import moment from "moment";

const { Option } = Select;

/**
 * Utility function to generate Select options from data array
 * @param data - Array of objects to generate options from
 * @param labelKey - Key to use for option label
 * @param valueKey - Key to use for option value
 * @returns Array of Option components
 */
export const generateSelectOptions = (
  data: any[],
  labelKey: string,
  valueKey: string
): React.ReactNode[] => {
  if (!Array.isArray(data)) {
    return [];
  }

  return data.map((item, index) => {
    const label = item[labelKey] || "";
    const value = item[valueKey] || "";

    return (
      <Option key={`${value}-${index}`} value={value}>
        {label}
      </Option>
    );
  });
};

/**
 * Utility function to generate option objects for Select components
 * @param data - Array of objects to generate options from
 * @param labelKey - Key to use for option label
 * @param valueKey - Key to use for option value
 * @returns Array of option objects with label and value properties
 */
export const generateOptionObjects = (
  data: any[],
  labelKey: string,
  valueKey: string
): { label: string; value: string }[] => {
  if (!Array.isArray(data)) {
    return [];
  }

  return data.map((item) => ({
    label: item[labelKey] || "",
    value: item[valueKey] || "",
  }));
};

/**
 * Utility function to generate Select options with custom label formatter
 * @param data - Array of objects to generate options from
 * @param valueKey - Key to use for option value
 * @param labelFormatter - Function to format the label
 * @returns Array of Option components
 */
export const generateSelectOptionsWithFormatter = (
  data: any[],
  valueKey: string,
  labelFormatter: (item: any) => string
): React.ReactNode[] => {
  if (!Array.isArray(data)) {
    return [];
  }

  return data.map((item, index) => {
    const value = item[valueKey] || "";
    const label = labelFormatter(item);

    return (
      <Option key={`${value}-${index}`} value={value}>
        {label}
      </Option>
    );
  });
};

export function formatDate(date: string) {
  if (!date) return "Invalid Date";
  try {
    return moment(date).format("MM/DD/YYYY HH:mm:ss");
  } catch {
    return "Invalid Date";
  }
}
