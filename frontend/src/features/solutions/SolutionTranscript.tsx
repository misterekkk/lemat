// @Role: Widok Transkrypcji: kroki z tekstem i wzorem KaTeX, wskazanie niskiej pewności odczytu oraz wyróżnienie pierwszego błędnego kroku z Oceny | @DependsOn: lib/api/types, lib/katex/Formula, lib/i18n/format, next-intl | @UsedBy: features/solutions/SolutionView | @Task: TASK-137
"use client";

import clsx from "clsx";
import { useTranslations } from "next-intl";
import type { TranskrypcjaWyjscie } from "@/lib/api/types";
import { Wzor } from "@/lib/katex/Formula";

/** Poniżej tej wartości `pewnosc_odczytu` krok jest oznaczany jako do sprawdzenia (kontrakt nie podaje progu). */
export const PROG_NISKIEJ_PEWNOSCI = 0.92;

export function SolutionTranscript({
  transkrypcja,
  bladnyKrok,
}: {
  transkrypcja: TranskrypcjaWyjscie;
  /** `nr` kroku wskazanego przez Ocenę jako pierwszy błędny. */
  bladnyKrok?: number | null;
}) {
  const t = useTranslations("Rozwiazanie");
  return (
    <section aria-labelledby="rozwiazanie-transkrypcja" className="flex flex-col gap-2">
      <h2 id="rozwiazanie-transkrypcja" className="text-lg font-semibold">
        {t("transkrypcjaNaglowek")}
      </h2>
      <ol className="flex flex-col gap-2">
        {transkrypcja.kroki.map((krok) => {
          const bladny = bladnyKrok === krok.nr;
          const niskaPewnosc =
            krok.wymaga_sprawdzenia ||
            (krok.pewnosc_odczytu !== null && krok.pewnosc_odczytu < PROG_NISKIEJ_PEWNOSCI);
          return (
            <li
              key={krok.nr}
              aria-current={bladny ? "step" : undefined}
              className={clsx(
                "rounded-md border p-3",
                bladny ? "border-red-500 bg-red-50" : "border-slate-200",
              )}
            >
              <p className="font-medium">{t("krok", { nr: krok.nr, tekst: krok.tekst })}</p>
              <Wzor latex={krok.latex} blokowy className="overflow-x-auto" />
              {niskaPewnosc ? (
                <p className="mt-1 text-sm text-amber-900">{t("niskaPewnosc")}</p>
              ) : null}
              {bladny ? (
                <p className="mt-1 text-sm font-medium text-red-800">{t("pierwszyBlad")}</p>
              ) : null}
            </li>
          );
        })}
      </ol>
    </section>
  );
}
