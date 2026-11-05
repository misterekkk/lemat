// @Role: Układ strefy Ucznia: strażnik sesji i nawigacja (StudentShell) | @DependsOn: features/accounts, react | @UsedBy: Next.js App Router | @Task: TASK-006, TASK-133
import type { ReactNode } from "react";
import { StudentShell } from "@/features/accounts";

export default function Layout({ children }: { children: ReactNode }) {
  return <StudentShell>{children}</StudentShell>;
}
