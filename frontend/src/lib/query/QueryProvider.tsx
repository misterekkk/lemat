// @Role: Dostawca TanStack Query (QueryClientProvider) dla komponentów klienckich | @DependsOn: @tanstack/react-query | @UsedBy: app/layout.tsx | @Task: TASK-006
"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";

export function QueryProvider({ children }: { children: ReactNode }) {
  const [klient] = useState(
    () => new QueryClient({ defaultOptions: { queries: { staleTime: 30_000, retry: 1 } } }),
  );
  return <QueryClientProvider client={klient}>{children}</QueryClientProvider>;
}
