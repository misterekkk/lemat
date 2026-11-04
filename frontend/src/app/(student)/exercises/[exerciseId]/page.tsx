// @Role: Strona /exercises/[exerciseId]: szczegóły Zadania (mapowanie exerciseId na zadanieId) | @DependsOn: features/content, features/solutions | @UsedBy: Next.js App Router | @Task: TASK-135, TASK-136
"use client";

import { useParams } from "next/navigation";
import { ExerciseDetail } from "@/features/content";
import { PhotoUpload } from "@/features/solutions";

export default function Page() {
  const { exerciseId: zadanieId } = useParams<{ exerciseId: string }>();
  return (
    <ExerciseDetail zadanieId={zadanieId} przeslanie={<PhotoUpload zadanieId={zadanieId} />} />
  );
}
