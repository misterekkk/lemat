// @Role: Stan ładowania trasy (komunikat dla czytników ekranu) | @DependsOn: next-intl | @UsedBy: Next.js App Router | @Task: TASK-138
import { useTranslations } from "next-intl";

export default function Ladowanie() {
  const t = useTranslations("Stany");
  return (
    <p role="status" aria-live="polite" className="p-6">
      {t("ladowanie")}
    </p>
  );
}
