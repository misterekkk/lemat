// @Role: Globalna granica błędów aplikacji (poza strefą Ucznia) z komunikatem po polsku i ponowieniem | @DependsOn: next-intl, ui/Button | @UsedBy: Next.js App Router | @Task: TASK-138
"use client";

import { useTranslations } from "next-intl";
import Link from "next/link";
import { Button } from "@/ui/Button";

export default function Blad({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  const t = useTranslations("Stany");
  return (
    <main className="mx-auto flex max-w-md flex-col gap-3 p-6">
      <h1 className="text-2xl font-bold">{t("bladTytul")}</h1>
      <p role="alert">{t("bladOpis")}</p>
      <Button onClick={reset}>{t("ponow")}</Button>
      <Link href="/" className="inline-flex min-h-11 items-center text-blue-700 underline">
        {t("wrocNaStrone")}
      </Link>
    </main>
  );
}
