// @Role: Strona główna / (linki do logowania i rejestracji, przekierowanie zalogowanego) | @DependsOn: features/accounts | @UsedBy: Next.js App Router (trasa /) | @Task: TASK-006, TASK-138
import { HomeView } from "@/features/accounts";

export default function StronaGlowna() {
  return <HomeView />;
}
