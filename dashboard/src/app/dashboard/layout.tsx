"use client";
import DashboardLayout from "#/components/layout/DashboardLayout";
import { withAuth } from "#/hoc/hoc";
import React from "react";

function layout({ children }: { children: React.ReactNode }) {
  return <DashboardLayout>{children}</DashboardLayout>;
}

export default withAuth(layout);
