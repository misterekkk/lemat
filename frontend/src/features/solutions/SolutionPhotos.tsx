// @Role: Podgląd przesłanych zdjęć kartki wyłącznie przez podpisany adres z GET …/zdjecia/{id}/adres (bez Authorization), z odnawianiem adresu | @DependsOn: features/solutions/use-solution, lib/api/types, next-intl | @UsedBy: features/solutions/SolutionView | @Task: TASK-137
"use client";

import { useTranslations } from "next-intl";
import type { ZdjecieRozwiazaniaWyjscie } from "@/lib/api/types";
import { useAdresZdjecia } from "./use-solution";

function Zdjecie({
  rozwiazanieId,
  zdjecie,
  nr,
}: {
  rozwiazanieId: string;
  zdjecie: ZdjecieRozwiazaniaWyjscie;
  nr: number;
}) {
  const t = useTranslations("Rozwiazanie");
  const adres = useAdresZdjecia(rozwiazanieId, zdjecie.id);

  if (adres.isError) {
    return <p role="alert">{t("zdjecieBlad", { nr })}</p>;
  }
  if (!adres.data) {
    return <p role="status">{t("zdjecieLadowanie", { nr })}</p>;
  }
  return (
    // Adres podpisany (HMAC) otwierany bez nagłówka Authorization — next/image nie ma tu zastosowania.
    // eslint-disable-next-line @next/next/no-img-element
    <img
      src={adres.data.url}
      alt={t("zdjecieAlt", { nr })}
      onError={() => void adres.refetch()}
      className="w-full rounded-md border border-slate-200"
    />
  );
}

export function SolutionPhotos({
  rozwiazanieId,
  zdjecia,
}: {
  rozwiazanieId: string;
  zdjecia: ZdjecieRozwiazaniaWyjscie[];
}) {
  const t = useTranslations("Rozwiazanie");
  const dostepne = zdjecia.filter((z) => z.dostepne);
  if (dostepne.length === 0) return null;
  return (
    <section aria-labelledby="rozwiazanie-zdjecia" className="flex flex-col gap-2">
      <h2 id="rozwiazanie-zdjecia" className="text-lg font-semibold">
        {t("zdjeciaNaglowek")}
      </h2>
      <ul className="grid gap-3 sm:grid-cols-2">
        {dostepne.map((zdjecie, indeks) => (
          <li key={zdjecie.id}>
            <Zdjecie rozwiazanieId={rozwiazanieId} zdjecie={zdjecie} nr={indeks + 1} />
          </li>
        ))}
      </ul>
    </section>
  );
}
