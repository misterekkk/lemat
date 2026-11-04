// @Role: Granica błędów strefy Ucznia (nawigacja pozostaje widoczna) z komunikatem i ponowieniem | @DependsOn: next-intl, ui/Button | @UsedBy: Next.js App Router | @Task: TASK-138
"use client";

import { useTranslations } from "next-intl";
import { Button } from "@/ui/Button";

export default function BladStrefyUcznia({
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  const t = useTranslations("Stany");
  return (
    <div className="flex flex-col gap-3">
      <h1 className="text-2xl font-bold">{t("bladTytul")}</h1>
      <p role="alert">{t("bladOpis")}</p>
      <Button onClick={reset} className="self-start">
        {t("ponow")}
      </Button>
    </div>
  );
}
