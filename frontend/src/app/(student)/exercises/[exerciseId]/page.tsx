// @Role: Strona /exercises/[exerciseId]: szczegóły Zadania (mapowanie exerciseId na zadanieId) | @DependsOn: features/content | @UsedBy: Next.js App Router | @Task: TASK-135
"use client";

import { useParams } from "next/navigation";
import { ExerciseDetail } from "@/features/content";

export default function Page() {
  const { exerciseId: zadanieId } = useParams<{ exerciseId: string }>();
  return <ExerciseDetail zadanieId={zadanieId} />;
}
