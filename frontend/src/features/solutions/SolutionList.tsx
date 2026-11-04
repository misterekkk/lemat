// @Role: Lista własnych Rozwiązań Ucznia (GET /rozwiazania): status po polsku, punkty, data i link do ekranu Rozwiązania, stan pusty i błąd z ponowieniem | @DependsOn: features/solutions/use-solution, lib/i18n/format, lib/api/use-error-message, ui/Button | @UsedBy: features/solutions/index, app/(student)/solutions/page | @Task: TASK-137
"use client";

import { useTranslations } from "next-intl";
import Link from "next/link";
import { useKomunikatBledu } from "@/lib/api/use-error-message";
import { formatujDateCzas, formatujLiczbe } from "@/lib/i18n/format";
import { Button } from "@/ui/Button";
import { useRozwiazania } from "./use-solution";

export function SolutionList() {
  const t = useTranslations("Rozwiazanie");
  const komunikatBledu = useKomunikatBledu();
  const zapytanie = useRozwiazania();

  if (zapytanie.isError) {
    return (
      <div className="flex flex-col gap-3">
        <h1 className="text-2xl font-bold">{t("listaNaglowek")}</h1>
        <p role="alert" className="rounded-md bg-red-50 p-3 text-red-800">
          {komunikatBledu(zapytanie.error)}
        </p>
        <Button wariant="drugorzedny" onClick={() => void zapytanie.refetch()}>
          {t("ponow")}
        </Button>
      </div>
    );
  }
  if (!zapytanie.data) {
    return (
      <p role="status" className="p-2">
        {t("listaLadowanie")}
      </p>
    );
  }

  const elementy = zapytanie.data.pages.flatMap((strona) => strona.elementy);
  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-bold">{t("listaNaglowek")}</h1>
      {elementy.length === 0 ? (
        <section className="rounded-lg border border-slate-200 p-4">
          <p>{t("listaPusta")}</p>
          <Link href="/exercises" className="mt-2 inline-block font-medium text-blue-700 underline">
            {t("listaPustaLink")}
          </Link>
        </section>
      ) : (
        <ul className="flex flex-col gap-2">
          {elementy.map((r) => (
            <li
              key={r.id}
              className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 rounded-md border border-slate-200 p-3"
            >
              <span className="font-medium">{t(`statusy.${r.status}`)}</span>
              {r.ocena && r.ocena.punkty !== null ? (
                <span>
                  {t("punkty", {
                    punkty: formatujLiczbe(r.ocena.punkty),
                    punktyMax: formatujLiczbe(r.ocena.punkty_max),
                  })}
                </span>
              ) : null}
              <span className="text-sm text-slate-600">{formatujDateCzas(r.created_at)}</span>
              <Link
                href={`/solutions/${r.id}`}
                className="inline-flex min-h-11 items-center font-medium text-blue-700 underline"
              >
                {t("otworz")}
              </Link>
            </li>
          ))}
        </ul>
      )}
      {zapytanie.hasNextPage ? (
        <div>
          <Button
            wariant="drugorzedny"
            disabled={zapytanie.isFetchingNextPage}
            onClick={() => void zapytanie.fetchNextPage()}
          >
            {t("wiecej")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
