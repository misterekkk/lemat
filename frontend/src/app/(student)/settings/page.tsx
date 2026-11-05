// @Role: Strona /settings: dane Ucznia (poziom matury, termin egzaminu, Dział) | @DependsOn: features/accounts | @UsedBy: Next.js App Router | @Task: TASK-134
import { SettingsForm } from "@/features/accounts";

export default function Page() {
  return <SettingsForm />;
}
