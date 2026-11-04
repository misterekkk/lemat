// @Role: Widok uproszczonego Profilu ucznia: karty (liczba rozwiązań, średni wynik), rozbicie na Działy, ostatnie Oceny z linkami, stan pusty | @DependsOn: lib/api/queries, lib/i18n/format, next-intl | @UsedBy: app/(student)/profile/page | @Task: TASK-134
"use client";

import { useTranslations } from "next-intl";
import Link from "next/link";
import { useKomunikatBledu } from "@/lib/api/use-error-message";
import { useProfilUproszczony } from "@/lib/api/queries";
import { formatujDateCzas, formatujLiczbe, formatujProcent } from "@/lib/i18n/format";
import { Button } from "@/ui/Button";

function Karta({ etykieta, wartosc, opis }: { etykieta: string; wartosc: string; opis?: string }) {
  return (
    <div className="rounded-lg border border-slate-200 p-4">
      <dt className="text-sm text-slate-600">{etykieta}</dt>
      <dd className="text-2xl font-bold">{wartosc}</dd>
      {opis ? <dd className="text-sm text-slate-600">{opis}</dd> : null}
    </div>
  );
}

export function ProfileView() {
  const t = useTranslations("Profil");
  const komunikatBledu = useKomunikatBledu();
  const zapytanie = useProfilUproszczony();
  const profil = zapytanie.data;

  if (zapytanie.isError) {
    return (
      <div className="flex flex-col gap-3">
        <h1 className="text-2xl font-bold">{t("naglowek")}</h1>
        <p role="alert" className="rounded-md bg-red-50 p-3 text-red-800">
          {komunikatBledu(zapytanie.error)}
        </p>
        <Button wariant="drugorzedny" onClick={() => void zapytanie.refetch()}>
          {t("ponow")}
        </Button>
      </div>
    );
  }
  if (!profil) {
    return (
      <p role="status" className="p-2">
        {t("ladowanie")}
      </p>
    );
  }

  if (profil.liczba_rozwiazan === 0) {
    return (
      <div className="flex flex-col gap-3">
        <h1 className="text-2xl font-bold">{t("naglowek")}</h1>
        <section className="rounded-lg border border-slate-200 p-4">
          <h2 className="text-lg font-semibold">{t("pustyTytul")}</h2>
          <p>{t("pustyOpis")}</p>
          <Link href="/exercises" className="mt-2 inline-block font-medium text-blue-700 underline">
            {t("pustyLink")}
          </Link>
        </section>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">{t("naglowek")}</h1>
      <dl className="grid gap-3 sm:grid-cols-3">
        <Karta etykieta={t("liczbaRozwiazan")} wartosc={formatujLiczbe(profil.liczba_rozwiazan)} />
        <Karta
          etykieta={t("liczbaOcenionych")}
          wartosc={formatujLiczbe(profil.liczba_ocenionych)}
        />
        <Karta
          etykieta={t("sredniWynik")}
          wartosc={
            profil.sredni_procent_punktow === null
              ? t("brakWyniku")
              : formatujProcent(profil.sredni_procent_punktow)
          }
          opis={
            profil.liczba_ocenionych > 0
              ? t("punkty", {
                  punkty: formatujLiczbe(profil.suma_punktow),
                  punktyMax: formatujLiczbe(profil.suma_punktow_max),
                })
              : undefined
          }
        />
      </dl>

      <section aria-labelledby="profil-dzialy">
        <h2 id="profil-dzialy" className="mb-2 text-lg font-semibold">
          {t("dzialyNaglowek")}
        </h2>
        {profil.dzialy.length === 0 ? (
          <p>{t("dzialyBrak")}</p>
        ) : (
          <ul className="flex flex-col gap-2">
            {profil.dzialy.map((d) => (
              <li
                key={d.dzial.id}
                className="flex flex-wrap items-baseline justify-between gap-x-4 rounded-md border border-slate-200 p-3"
              >
                <span className="font-medium">{d.dzial.nazwa}</span>
                <span>
                  {t("dzialyWiersz", {
                    procent: formatujProcent(d.sredni_procent_punktow),
                    liczba: formatujLiczbe(d.liczba_ocenionych),
                  })}
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section aria-labelledby="profil-ostatnie">
        <h2 id="profil-ostatnie" className="mb-2 text-lg font-semibold">
          {t("ostatnieNaglowek")}
        </h2>
        {profil.ostatnie_oceny.length === 0 ? (
          <p>{t("ostatnieBrak")}</p>
        ) : (
          <ul className="flex flex-col gap-2">
            {profil.ostatnie_oceny.map((o) => (
              <li
                key={o.rozwiazanie_id}
                className="flex flex-wrap items-baseline justify-between gap-x-4 rounded-md border border-slate-200 p-3"
              >
                <span className="font-medium">
                  {t("ostatniaOcena", {
                    dzial: o.dzial.nazwa,
                    punkty: formatujLiczbe(o.punkty),
                    punktyMax: formatujLiczbe(o.punkty_max),
                  })}
                </span>
                <span className="text-sm text-slate-600">{formatujDateCzas(o.oceniono_at)}</span>
                <Link
                  href={`/solutions/${o.rozwiazanie_id}`}
                  className="inline-flex min-h-11 items-center font-medium text-blue-700 underline"
                >
                  {t("ostatnieOtworz")}
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
