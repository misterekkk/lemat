// @Role: Strona /exercises: lista Zadań z filtrem Działu | @DependsOn: features/content | @UsedBy: Next.js App Router | @Task: TASK-135
import { ExerciseList } from "@/features/content";

export default function Page() {
  return <ExerciseList />;
}
