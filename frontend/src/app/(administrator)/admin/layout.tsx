// @Role: Układ strefy Administratora (nawigacja i strażnik roli ADMINISTRATOR dojdą w kolejnych zadaniach) | @DependsOn: react | @UsedBy: Next.js App Router | @Task: TASK-006
import type { ReactNode } from "react";

export default function Layout({ children }: { children: ReactNode }) {
  return <>{children}</>;
}
