// @Role: Szczegóły Zadania: pełna treść z KaTeX, Dział, trudność, punkty, Umiejętności, przycisk „Prześlij rozwiązanie” (slot na przesłanie) i strona „Nie znaleziono zadania” | @DependsOn: lib/api/queries, lib/api/errors, lib/katex/MarkdownContent, ui/Button | @UsedBy: app/(student)/exercises/[exerciseId]/page | @Task: TASK-135
"use client";

import { useTranslations } from "next-intl";
import Link from "next/link";
import { useState, type ReactNode } from "react";
import { BladApi } from "@/lib/api/errors";
import { useKomunikatBledu } from "@/lib/api/use-error-message";
import { useZadanie } from "@/lib/api/queries";
import { TrescMarkdown } from "@/lib/katex/MarkdownContent";
import { Button } from "@/ui/Button";

export interface ExerciseDetailProps {
  zadanieId: string;
  /** Slot sekcji przesyłania rozwiązania (TASK-136). */
  przeslanie?: ReactNode;
}

export function ExerciseDetail({ zadanieId, przeslanie }: ExerciseDetailProps) {
  const t = useTranslations("Zadania");
  const komunikatBledu = useKomunikatBledu();
  const zapytanie = useZadanie(zadanieId);
  const [przeslanieOtwarte, ustawPrzeslanieOtwarte] = useState(false);
  const zadanie = zapytanie.data;

  const linkPowrotu = (
    <Link href="/exercises" className="inline-flex min-h-11 items-center text-blue-700 underline">
      {t("wrocDoListy")}
    </Link>
  );

  if (zapytanie.isError) {
    const nieZnaleziono = zapytanie.error instanceof BladApi && zapytanie.error.status === 404;
    if (nieZnaleziono) {
      return (
        <div className="flex flex-col gap-3">
          <h1 className="text-2xl font-bold">{t("nieZnalezionoTytul")}</h1>
          <p>{t("nieZnalezionoOpis")}</p>
          {linkPowrotu}
        </div>
      );
    }
    return (
      <div className="flex flex-col gap-3">
        <p role="alert" className="rounded-md bg-red-50 p-3 text-red-800">
          {komunikatBledu(zapytanie.error)}
        </p>
        <Button wariant="drugorzedny" onClick={() => void zapytanie.refetch()}>
          {t("ponow")}
        </Button>
        {linkPowrotu}
      </div>
    );
  }
  if (!zadanie) {
    return (
      <p role="status" className="p-2">
        {t("szczegolyLadowanie")}
      </p>
    );
  }

  return (
    <article className="flex flex-col gap-4">
      {linkPowrotu}
      <h1 className="text-2xl font-bold">{t("szczegolyNaglowek")}</h1>
      <p className="text-sm">
        {t("dzialTrudnoscPunkty", {
          dzial: zadanie.dzial.nazwa,
          poziom: zadanie.poziom_trudnosci,
          punkty: zadanie.punkty_max,
        })}
      </p>
      <TrescMarkdown tresc={zadanie.tresc_md} className="break-words text-lg" />

      <section aria-labelledby="zadanie-umiejetnosci">
        <h2 id="zadanie-umiejetnosci" className="mb-2 text-lg font-semibold">
          {t("umiejetnosci")}
        </h2>
        {zadanie.umiejetnosci.length === 0 ? (
          <p>{t("brakUmiejetnosci")}</p>
        ) : (
          <ul className="flex flex-col gap-1">
            {zadanie.umiejetnosci.map((u) => (
              <li key={u.id}>
                {u.nazwa}
                {u.glowna ? ` (${t("umiejetnoscGlowna")})` : null}
              </li>
            ))}
          </ul>
        )}
      </section>

      <div>
        <Button
          aria-expanded={przeslanieOtwarte}
          aria-controls="zadanie-przeslanie"
          onClick={() => ustawPrzeslanieOtwarte((o) => !o)}
        >
          {t("przeslij")}
        </Button>
      </div>
      {przeslanieOtwarte ? (
        <section id="zadanie-przeslanie" aria-label={t("sekcjaPrzeslania")}>
          {przeslanie}
        </section>
      ) : null}
    </article>
  );
}
