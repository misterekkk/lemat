// @Role: Ekran Rozwiązania: postęp wg statusu (odpytywanie), Transkrypcja z zatwierdzeniem, Ocena i Wskazówka, błąd rozpoznawania, podgląd zdjęć | @DependsOn: features/solutions/use-solution, SolutionTranscript, SolutionGrade, SolutionPhotos, lib/api/errors, ui/Button | @UsedBy: features/solutions/index, app/(student)/solutions/[solutionId]/page | @Task: TASK-137
"use client";

import { useTranslations } from "next-intl";
import Link from "next/link";
import { BladApi } from "@/lib/api/errors";
import { useKomunikatBledu } from "@/lib/api/use-error-message";
import { Button } from "@/ui/Button";
import { SolutionGrade } from "./SolutionGrade";
import { SolutionPhotos } from "./SolutionPhotos";
import { SolutionTranscript } from "./SolutionTranscript";
import { useRozwiazanie, useZatwierdzenie } from "./use-solution";

function Postep({ tekst }: { tekst: string }) {
  return (
    <div className="flex flex-col gap-2 rounded-lg border border-slate-200 p-4">
      <p role="status" aria-live="polite" className="font-medium">
        {tekst}
      </p>
      <div aria-hidden="true" className="h-2 w-full overflow-hidden rounded-full bg-slate-200">
        <div className="h-full w-1/3 rounded-full bg-blue-700 motion-safe:animate-pulse" />
      </div>
    </div>
  );
}

export function SolutionView({ rozwiazanieId }: { rozwiazanieId: string }) {
  const t = useTranslations("Rozwiazanie");
  const komunikatBledu = useKomunikatBledu();
  const zapytanie = useRozwiazanie(rozwiazanieId);
  const zatwierdzenie = useZatwierdzenie(rozwiazanieId);
  const rozwiazanie = zapytanie.data;

  const linkListy = (
    <Link href="/solutions" className="inline-flex min-h-11 items-center text-blue-700 underline">
      {t("wrocDoListy")}
    </Link>
  );

  if (!rozwiazanie) {
    if (zapytanie.isError) {
      if (zapytanie.error instanceof BladApi && zapytanie.error.status === 404) {
        return (
          <div className="flex flex-col gap-3">
            <h1 className="text-2xl font-bold">{t("nieZnalezionoTytul")}</h1>
            <p>{t("nieZnalezionoOpis")}</p>
            {linkListy}
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
          {linkListy}
        </div>
      );
    }
    return (
      <p role="status" className="p-2">
        {t("ladowanie")}
      </p>
    );
  }

  const { status, transkrypcja, ocena } = rozwiazanie;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h1 className="text-2xl font-bold">{t("naglowek")}</h1>
        {linkListy}
      </div>
      <p>{t("status", { status: t(`statusy.${status}`) })}</p>

      {zapytanie.isError ? (
        <p role="alert" className="rounded-md bg-amber-50 p-3 text-amber-900">
          {komunikatBledu(zapytanie.error)}
        </p>
      ) : null}

      {zapytanie.przekroczonyLimit ? (
        <div role="alert" className="flex flex-col gap-2 rounded-md bg-amber-50 p-3 text-amber-900">
          <p>{t("przekroczonyLimit")}</p>
          <div>
            <Button wariant="drugorzedny" onClick={zapytanie.odswiez}>
              {t("odswiez")}
            </Button>
          </div>
        </div>
      ) : null}

      {status === "PRZESLANE" ? <Postep tekst={t("postep.PRZESLANE")} /> : null}
      {status === "ROZPOZNAWANIE" ? <Postep tekst={t("postep.ROZPOZNAWANIE")} /> : null}
      {status === "ZATWIERDZONE" || status === "OCENIANIE" ? (
        <Postep tekst={t("postep.OCENIANIE")} />
      ) : null}

      {status === "BLAD_ROZPOZNAWANIA" ? (
        <section className="flex flex-col gap-3 rounded-lg border border-red-300 bg-red-50 p-4">
          <h2 className="text-lg font-semibold">{t("bladRozpoznawaniaTytul")}</h2>
          <p>{t("bladRozpoznawaniaOpis")}</p>
          <div>
            <Link
              href={`/exercises/${rozwiazanie.zadanie_id}`}
              className="inline-flex min-h-11 items-center justify-center rounded-md bg-blue-700 px-4 py-2 font-medium text-white hover:bg-blue-800"
            >
              {t("przeslijPonownie")}
            </Link>
          </div>
        </section>
      ) : null}

      {transkrypcja ? (
        <SolutionTranscript
          transkrypcja={transkrypcja}
          bladnyKrok={ocena?.pierwszy_bledny_krok ?? null}
        />
      ) : null}

      {status === "TRANSKRYPCJA_GOTOWA" && transkrypcja ? (
        <div className="flex flex-col gap-2">
          <p>{t("zatwierdzPodpowiedz")}</p>
          {zatwierdzenie.isError ? (
            <p role="alert" className="rounded-md bg-red-50 p-3 text-red-800">
              {komunikatBledu(zatwierdzenie.error)}
            </p>
          ) : null}
          <div>
            <Button
              disabled={zatwierdzenie.isPending}
              onClick={() => zatwierdzenie.mutate(rozwiazanie)}
            >
              {zatwierdzenie.isPending ? t("zatwierdzanie") : t("zatwierdz")}
            </Button>
          </div>
        </div>
      ) : null}

      {status === "OCENIONE" && ocena ? <SolutionGrade ocena={ocena} /> : null}

      <SolutionPhotos rozwiazanieId={rozwiazanie.id} zdjecia={rozwiazanie.zdjecia} />
    </div>
  );
}
