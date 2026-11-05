// @Role: Strona /solutions/[solutionId]: ekran Rozwiązania (mapowanie solutionId na rozwiazanieId) | @DependsOn: features/solutions | @UsedBy: Next.js App Router | @Task: TASK-137
"use client";

import { useParams } from "next/navigation";
import { SolutionView } from "@/features/solutions";

export default function Page() {
  const { solutionId: rozwiazanieId } = useParams<{ solutionId: string }>();
  return <SolutionView rozwiazanieId={rozwiazanieId} />;
}
