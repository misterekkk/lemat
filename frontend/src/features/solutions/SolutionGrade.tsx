// @Role: Widok Oceny: punkty x/y, tabela Kryteriów z opisem w KaTeX, uzasadnienie jako Markdown, typ błędu i Wskazówka w ramce | @DependsOn: lib/api/types, lib/katex/MarkdownContent, lib/i18n/format, next-intl | @UsedBy: features/solutions/SolutionView | @Task: TASK-137
"use client";

import { useTranslations } from "next-intl";
import { formatujLiczbe } from "@/lib/i18n/format";
import type { OcenaWyjscie } from "@/lib/api/types";
import { TrescMarkdown } from "@/lib/katex/MarkdownContent";

export function SolutionGrade({ ocena }: { ocena: OcenaWyjscie }) {
  const t = useTranslations("Rozwiazanie");
  return (
    <section aria-labelledby="rozwiazanie-ocena" className="flex flex-col gap-4">
      <h2 id="rozwiazanie-ocena" className="text-lg font-semibold">
        {t("ocenaNaglowek")}
      </h2>
      <p className="text-3xl font-bold">
        {ocena.punkty === null
          ? t("brakPunktow")
          : t("punkty", {
              punkty: formatujLiczbe(ocena.punkty),
              punktyMax: formatujLiczbe(ocena.punkty_max),
            })}
      </p>

      <table className="w-full border-collapse text-left">
        <caption className="sr-only">{t("kryteriaOpis")}</caption>
        <thead>
          <tr className="border-b border-slate-300">
            <th scope="col" className="p-2">
              {t("kryteriumNr")}
            </th>
            <th scope="col" className="p-2">
              {t("kryteriumOpis")}
            </th>
            <th scope="col" className="p-2 text-right">
              {t("kryteriumPunkty")}
            </th>
          </tr>
        </thead>
        <tbody>
          {ocena.kryteria.map((kryterium) => (
            <tr key={kryterium.nr_kryterium} className="border-b border-slate-200 align-top">
              <th scope="row" className="p-2 font-medium">
                {kryterium.nr_kryterium}
              </th>
              <td className="p-2">
                <TrescMarkdown tresc={kryterium.opis} />
              </td>
              <td className="p-2 text-right whitespace-nowrap">
                {kryterium.punkty === null
                  ? t("brakPunktow")
                  : t("punktyKryterium", {
                      punkty: formatujLiczbe(kryterium.punkty),
                      punktyMax: formatujLiczbe(kryterium.punkty_max),
                    })}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {ocena.typ_bledu ? (
        <p>
          {t("typBledu", {
            typ: t(`typBleduWartosc.${ocena.typ_bledu}`),
            krok: ocena.pierwszy_bledny_krok ?? 0,
          })}
        </p>
      ) : null}

      <div>
        <h3 className="font-semibold">{t("uzasadnienieNaglowek")}</h3>
        <TrescMarkdown tresc={ocena.uzasadnienie} />
      </div>

      {ocena.wskazowka ? (
        <aside
          aria-labelledby="rozwiazanie-wskazowka"
          className="rounded-lg border-2 border-blue-600 bg-blue-50 p-4"
        >
          <h3 id="rozwiazanie-wskazowka" className="font-semibold">
            {t("wskazowkaNaglowek")}
          </h3>
          <TrescMarkdown tresc={ocena.wskazowka} />
        </aside>
      ) : ocena.wskazowka_w_przygotowaniu ? (
        <p role="status">{t("wskazowkaWPrzygotowaniu")}</p>
      ) : null}
    </section>
  );
}
