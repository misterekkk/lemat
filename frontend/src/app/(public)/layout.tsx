// @Role: Układ stref publicznych (bez logowania) | @DependsOn: react | @UsedBy: Next.js App Router | @Task: TASK-006
import type { ReactNode } from "react";

export default function Layout({ children }: { children: ReactNode }) {
  return <>{children}</>;
}
