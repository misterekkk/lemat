// @Role: Lista Zadań (kafelki: Dział, poziom trudności, punkty_max, treść z KaTeX) z filtrem Działu oraz stanami ładowania, pustym i błędu | @DependsOn: lib/api/queries, lib/katex/MarkdownContent, ui/Select, ui/Button | @UsedBy: app/(student)/exercises/page | @Task: TASK-135
"use client";

import { useTranslations } from "next-intl";
import Link from "next/link";
import { useState } from "react";
import { useKomunikatBledu } from "@/lib/api/use-error-message";
import { useDzialy, useZadania } from "@/lib/api/queries";
import { TrescMarkdown } from "@/lib/katex/MarkdownContent";
import { Button } from "@/ui/Button";
import { Select } from "@/ui/Select";

export function ExerciseList() {
  const t = useTranslations("Zadania");
  const komunikatBledu = useKomunikatBledu();
  const [dzialId, ustawDzialId] = useState<number | undefined>(undefined);
  const dzialy = useDzialy();
  const zadania = useZadania(dzialId);

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-bold">{t("naglowek")}</h1>
      <div className="sm:max-w-xs">
        <Select
          etykieta={t("filtrDzialu")}
          value={dzialId === undefined ? "" : String(dzialId)}
          onChange={(e) => ustawDzialId(e.target.value === "" ? undefined : Number(e.target.value))}
        >
          <option value="">{t("wszystkieDzialy")}</option>
          {(dzialy.data ?? []).map((d) => (
            <option key={d.id} value={d.id}>
              {d.nazwa}
            </option>
          ))}
        </Select>
      </div>

      {zadania.isError ? (
        <div className="flex flex-col gap-3">
          <p role="alert" className="rounded-md bg-red-50 p-3 text-red-800">
            {komunikatBledu(zadania.error)}
          </p>
          <Button wariant="drugorzedny" onClick={() => void zadania.refetch()}>
            {t("ponow")}
          </Button>
        </div>
      ) : !zadania.data ? (
        <p role="status" className="p-2">
          {t("ladowanie")}
        </p>
      ) : zadania.data.elementy.length === 0 ? (
        <p>{t("pusta")}</p>
      ) : (
        <ul className="grid gap-3 sm:grid-cols-2">
          {zadania.data.elementy.map((z) => (
            <li key={z.id} className="flex flex-col gap-2 rounded-lg border border-slate-200 p-4">
              <p className="text-sm text-slate-600">{t("dzial", { dzial: z.dzial.nazwa })}</p>
              <TrescMarkdown tresc={z.tresc_md} className="line-clamp-3 break-words" />
              <p className="text-sm">
                {t("trudnoscPunkty", { poziom: z.poziom_trudnosci, punkty: z.punkty_max })}
              </p>
              <Link
                href={`/exercises/${z.id}`}
                className="inline-flex min-h-11 items-center font-medium text-blue-700 underline"
              >
                {t("otworz")}
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
