import "@testing-library/jest-dom";
import React from "react";
import {
  generateSelectOptions,
  generateOptionObjects,
  generateSelectOptionsWithFormatter,
} from "#/utils/utils";

describe("Utils Functions", () => {
  describe("generateSelectOptions", () => {
    test("should generate options from valid data array", () => {
      const data = [
        { id: "1", name: "Option 1" },
        { id: "2", name: "Option 2" },
        { id: "3", name: "Option 3" },
      ];

      const options = generateSelectOptions(data, "name", "id");

      expect(options).toHaveLength(3);
      expect(options[0]).toHaveProperty("props.value", "1");
      expect(options[0]).toHaveProperty("props.children", "Option 1");
      expect(options[1]).toHaveProperty("props.value", "2");
      expect(options[1]).toHaveProperty("props.children", "Option 2");
      expect(options[2]).toHaveProperty("props.value", "3");
      expect(options[2]).toHaveProperty("props.children", "Option 3");
    });

    test("should handle empty array", () => {
      const data: any[] = [];
      const options = generateSelectOptions(data, "name", "id");

      expect(options).toHaveLength(0);
    });

    test("should handle non-array input", () => {
      const data = null as any;
      const options = generateSelectOptions(data, "name", "id");

      expect(options).toHaveLength(0);
    });

    test("should handle undefined input", () => {
      const data = undefined as any;
      const options = generateSelectOptions(data, "name", "id");

      expect(options).toHaveLength(0);
    });

    test("should handle missing label and value keys", () => {
      const data = [
        { id: "1", name: "Option 1" },
        { id: "2", name: "Option 2" },
      ];

      const options = generateSelectOptions(data, "nonexistent", "nonexistent");

      expect(options).toHaveLength(2);
      expect(options[0]).toHaveProperty("props.value", "");
      expect(options[0]).toHaveProperty("props.children", "");
      expect(options[1]).toHaveProperty("props.value", "");
      expect(options[1]).toHaveProperty("props.children", "");
    });

    test("should generate unique keys for options", () => {
      const data = [
        { id: "1", name: "Option 1" },
        { id: "2", name: "Option 2" },
      ];

      const options = generateSelectOptions(data, "name", "id");

      expect(options[0]).toHaveProperty("key", "1-0");
      expect(options[1]).toHaveProperty("key", "2-1");
    });

    test("should handle data with duplicate values", () => {
      const data = [
        { id: "1", name: "Option 1" },
        { id: "1", name: "Option 1 Duplicate" },
      ];

      const options = generateSelectOptions(data, "name", "id");

      expect(options).toHaveLength(2);
      expect(options[0]).toHaveProperty("props.value", "1");
      expect(options[0]).toHaveProperty("props.children", "Option 1");
      expect(options[1]).toHaveProperty("props.value", "1");
      expect(options[1]).toHaveProperty("props.children", "Option 1 Duplicate");
    });

    test("should handle complex data objects", () => {
      const data = [
        {
          id: "1",
          name: "Option 1",
          description: "Description 1",
          metadata: { category: "A" },
        },
        {
          id: "2",
          name: "Option 2",
          description: "Description 2",
          metadata: { category: "B" },
        },
      ];

      const options = generateSelectOptions(data, "name", "id");

      expect(options).toHaveLength(2);
      expect(options[0]).toHaveProperty("props.value", "1");
      expect(options[0]).toHaveProperty("props.children", "Option 1");
      expect(options[1]).toHaveProperty("props.value", "2");
      expect(options[1]).toHaveProperty("props.children", "Option 2");
    });
  });

  describe("generateOptionObjects", () => {
    test("should generate option objects from valid data array", () => {
      const data = [
        { id: "1", name: "Option 1" },
        { id: "2", name: "Option 2" },
        { id: "3", name: "Option 3" },
      ];

      const options = generateOptionObjects(data, "name", "id");

      expect(options).toHaveLength(3);
      expect(options[0]).toEqual({ label: "Option 1", value: "1" });
      expect(options[1]).toEqual({ label: "Option 2", value: "2" });
      expect(options[2]).toEqual({ label: "Option 3", value: "3" });
    });

    test("should handle empty array", () => {
      const data: any[] = [];
      const options = generateOptionObjects(data, "name", "id");

      expect(options).toHaveLength(0);
    });

    test("should handle non-array input", () => {
      const data = null as any;
      const options = generateOptionObjects(data, "name", "id");

      expect(options).toHaveLength(0);
    });

    test("should handle undefined input", () => {
      const data = undefined as any;
      const options = generateOptionObjects(data, "name", "id");

      expect(options).toHaveLength(0);
    });

    test("should handle missing label and value keys", () => {
      const data = [
        { id: "1", name: "Option 1" },
        { id: "2", name: "Option 2" },
      ];

      const options = generateOptionObjects(data, "nonexistent", "nonexistent");

      expect(options).toHaveLength(2);
      expect(options[0]).toEqual({ label: "", value: "" });
      expect(options[1]).toEqual({ label: "", value: "" });
    });

    test("should handle data with null values", () => {
      const data = [
        { id: null, name: null },
        { id: "2", name: "Option 2" },
      ];

      const options = generateOptionObjects(data, "name", "id");

      expect(options).toHaveLength(2);
      expect(options[0]).toEqual({ label: "", value: "" });
      expect(options[1]).toEqual({ label: "Option 2", value: "2" });
    });

    test("should handle data with undefined values", () => {
      const data = [
        { id: undefined, name: undefined },
        { id: "2", name: "Option 2" },
      ];

      const options = generateOptionObjects(data, "name", "id");

      expect(options).toHaveLength(2);
      expect(options[0]).toEqual({ label: "", value: "" });
      expect(options[1]).toEqual({ label: "Option 2", value: "2" });
    });

    test("should handle complex data objects", () => {
      const data = [
        {
          id: "1",
          name: "Option 1",
          description: "Description 1",
          metadata: { category: "A" },
        },
        {
          id: "2",
          name: "Option 2",
          description: "Description 2",
          metadata: { category: "B" },
        },
      ];

      const options = generateOptionObjects(data, "name", "id");

      expect(options).toHaveLength(2);
      expect(options[0]).toEqual({ label: "Option 1", value: "1" });
      expect(options[1]).toEqual({ label: "Option 2", value: "2" });
    });
  });

  describe("generateSelectOptionsWithFormatter", () => {
    test("should generate options with custom formatter", () => {
      const data = [
        { id: "1", name: "Option 1", category: "A" },
        { id: "2", name: "Option 2", category: "B" },
        { id: "3", name: "Option 3", category: "A" },
      ];

      const labelFormatter = (item: any) => `${item.name} (${item.category})`;
      const options = generateSelectOptionsWithFormatter(
        data,
        "id",
        labelFormatter
      );

      expect(options).toHaveLength(3);
      expect(options[0]).toHaveProperty("props.value", "1");
      expect(options[0]).toHaveProperty("props.children", "Option 1 (A)");
      expect(options[1]).toHaveProperty("props.value", "2");
      expect(options[1]).toHaveProperty("props.children", "Option 2 (B)");
      expect(options[2]).toHaveProperty("props.value", "3");
      expect(options[2]).toHaveProperty("props.children", "Option 3 (A)");
    });

    test("should handle empty array", () => {
      const data: any[] = [];
      const labelFormatter = (item: any) => item.name;
      const options = generateSelectOptionsWithFormatter(
        data,
        "id",
        labelFormatter
      );

      expect(options).toHaveLength(0);
    });

    test("should handle non-array input", () => {
      const data = null as any;
      const labelFormatter = (item: any) => item.name;
      const options = generateSelectOptionsWithFormatter(
        data,
        "id",
        labelFormatter
      );

      expect(options).toHaveLength(0);
    });

    test("should handle undefined input", () => {
      const data = undefined as any;
      const labelFormatter = (item: any) => item.name;
      const options = generateSelectOptionsWithFormatter(
        data,
        "id",
        labelFormatter
      );

      expect(options).toHaveLength(0);
    });

    test("should handle missing value key", () => {
      const data = [
        { id: "1", name: "Option 1" },
        { id: "2", name: "Option 2" },
      ];

      const labelFormatter = (item: any) => item.name;
      const options = generateSelectOptionsWithFormatter(
        data,
        "nonexistent",
        labelFormatter
      );

      expect(options).toHaveLength(2);
      expect(options[0]).toHaveProperty("props.value", "");
      expect(options[0]).toHaveProperty("props.children", "Option 1");
      expect(options[1]).toHaveProperty("props.value", "");
      expect(options[1]).toHaveProperty("props.children", "Option 2");
    });

    test("should handle formatter that returns empty string", () => {
      const data = [
        { id: "1", name: "Option 1" },
        { id: "2", name: "Option 2" },
      ];

      const labelFormatter = (item: any) => "";
      const options = generateSelectOptionsWithFormatter(
        data,
        "id",
        labelFormatter
      );

      expect(options).toHaveLength(2);
      expect(options[0]).toHaveProperty("props.value", "1");
      expect(options[0]).toHaveProperty("props.children", "");
      expect(options[1]).toHaveProperty("props.value", "2");
      expect(options[1]).toHaveProperty("props.children", "");
    });

    test("should handle formatter that returns null", () => {
      const data = [
        { id: "1", name: "Option 1" },
        { id: "2", name: "Option 2" },
      ];

      const labelFormatter = (item: any) => null;
      const options = generateSelectOptionsWithFormatter(
        data,
        "id",
        labelFormatter
      );

      expect(options).toHaveLength(2);
      expect(options[0]).toHaveProperty("props.value", "1");
      expect(options[0]).toHaveProperty("props.children", null);
      expect(options[1]).toHaveProperty("props.value", "2");
      expect(options[1]).toHaveProperty("props.children", null);
    });

    test("should handle formatter that returns undefined", () => {
      const data = [
        { id: "1", name: "Option 1" },
        { id: "2", name: "Option 2" },
      ];

      const labelFormatter = (item: any) => undefined;
      const options = generateSelectOptionsWithFormatter(
        data,
        "id",
        labelFormatter
      );

      expect(options).toHaveLength(2);
      expect(options[0]).toHaveProperty("props.value", "1");
      expect(options[0]).toHaveProperty("props.children", undefined);
      expect(options[1]).toHaveProperty("props.value", "2");
      expect(options[1]).toHaveProperty("props.children", undefined);
    });

    test("should handle complex formatter function", () => {
      const data = [
        { id: "1", name: "Option 1", category: "A", priority: "high" },
        { id: "2", name: "Option 2", category: "B", priority: "low" },
        { id: "3", name: "Option 3", category: "A", priority: "medium" },
      ];

      const labelFormatter = (item: any) => {
        const priorityIcon =
          item.priority === "high"
            ? "🔥"
            : item.priority === "medium"
            ? "⚡"
            : "💤";
        return `${priorityIcon} ${item.name} [${item.category}]`;
      };

      const options = generateSelectOptionsWithFormatter(
        data,
        "id",
        labelFormatter
      );

      expect(options).toHaveLength(3);
      expect(options[0]).toHaveProperty("props.value", "1");
      expect(options[0]).toHaveProperty("props.children", "🔥 Option 1 [A]");
      expect(options[1]).toHaveProperty("props.value", "2");
      expect(options[1]).toHaveProperty("props.children", "💤 Option 2 [B]");
      expect(options[2]).toHaveProperty("props.value", "3");
      expect(options[2]).toHaveProperty("props.children", "⚡ Option 3 [A]");
    });

    test("should generate unique keys for options", () => {
      const data = [
        { id: "1", name: "Option 1" },
        { id: "2", name: "Option 2" },
      ];

      const labelFormatter = (item: any) => item.name;
      const options = generateSelectOptionsWithFormatter(
        data,
        "id",
        labelFormatter
      );

      expect(options[0]).toHaveProperty("key", "1-0");
      expect(options[1]).toHaveProperty("key", "2-1");
    });
  });
});
