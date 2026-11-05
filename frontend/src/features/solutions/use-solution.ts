// @Role: Hooki TanStack Query ekranu Rozwiązania: odpytywanie statusu wg nastepne_odpytanie_ms z limitem 60 s, zatwierdzenie Transkrypcji (If-Match), lista Rozwiązań, adres zdjęcia | @DependsOn: lib/api/client, lib/api/queries, @tanstack/react-query | @UsedBy: features/solutions/SolutionView, SolutionList, SolutionPhotos | @Task: TASK-137
"use client";

import {
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
  type Query,
} from "@tanstack/react-query";
import { useEffect, useState } from "react";
import {
  pobierzAdresZdjecia,
  pobierzRozwiazania,
  pobierzRozwiazanie,
  zatwierdzRozwiazanie,
} from "@/lib/api/client";
import { kluczeZapytan } from "@/lib/api/queries";
import type { RozwiazanieWyjscie } from "@/lib/api/types";

/** Kontrakt 7.5: po 60 s klient przerywa odpytywanie i pokazuje komunikat z przyciskiem odświeżenia. */
export const LIMIT_ODPYTYWANIA_MS = 60_000;

/** Faza odpytywania: limit 60 s liczy się osobno dla odczytu i dla oceniania. */
function faza(status: RozwiazanieWyjscie["status"] | undefined): string {
  return status === "ZATWIERDZONE" || status === "OCENIANIE" ? "ocena" : "odczyt";
}

export function useRozwiazanie(rozwiazanieId: string) {
  const [odswiezenia, ustawOdswiezenia] = useState(0);
  const [przekroczonyKlucz, ustawPrzekroczonyKlucz] = useState<string | null>(null);

  const kluczFazy = (status: RozwiazanieWyjscie["status"] | undefined) =>
    `${odswiezenia}-${faza(status)}`;

  const zapytanie = useQuery({
    queryKey: kluczeZapytan.rozwiazanie(rozwiazanieId),
    // Bez `signal` z TanStack Query: AbortSignal z jsdom nie jest akceptowany przez fetch w testach (undici).
    queryFn: () => pobierzRozwiazanie(rozwiazanieId),
    refetchInterval: (query: Query<RozwiazanieWyjscie>) =>
      przekroczonyKlucz === kluczFazy(query.state.data?.status)
        ? false
        : (query.state.data?.nastepne_odpytanie_ms ?? false),
  });

  const dane = zapytanie.data;
  const aktywne = dane !== undefined && dane.nastepne_odpytanie_ms !== null;
  const klucz = kluczFazy(dane?.status);
  const przekroczony = przekroczonyKlucz === klucz;

  useEffect(() => {
    if (!aktywne) return;
    const timer = setTimeout(() => ustawPrzekroczonyKlucz(klucz), LIMIT_ODPYTYWANIA_MS);
    return () => clearTimeout(timer);
  }, [aktywne, klucz]);

  return {
    ...zapytanie,
    przekroczonyLimit: przekroczony && aktywne,
    /** Wznawia odpytywanie (nowy klucz limitu) i od razu pobiera aktualny stan. */
    odswiez: () => {
      ustawOdswiezenia((n) => n + 1);
      void zapytanie.refetch();
    },
  };
}

export function useZatwierdzenie(rozwiazanieId: string) {
  const klient = useQueryClient();
  return useMutation({
    mutationFn: (rozwiazanie: RozwiazanieWyjscie) => {
      if (!rozwiazanie.transkrypcja) throw new Error("Brak Transkrypcji do zatwierdzenia.");
      return zatwierdzRozwiazanie(
        rozwiazanieId,
        { transkrypcja_id: rozwiazanie.transkrypcja.id, wersja: rozwiazanie.transkrypcja.wersja },
        rozwiazanie.version,
      );
    },
    onSuccess: (wynik) => {
      klient.setQueryData(kluczeZapytan.rozwiazanie(rozwiazanieId), wynik);
      void klient.invalidateQueries({ queryKey: kluczeZapytan.rozwiazania });
    },
    // 409/412: stan po stronie serwera jest już inny — pobieramy go ponownie.
    onError: () => klient.invalidateQueries({ queryKey: kluczeZapytan.rozwiazanie(rozwiazanieId) }),
  });
}

export function useRozwiazania() {
  return useInfiniteQuery({
    queryKey: kluczeZapytan.rozwiazania,
    queryFn: ({ pageParam }) => pobierzRozwiazania(pageParam ? { kursor: pageParam } : {}),
    initialPageParam: "",
    getNextPageParam: (ostatnia) => ostatnia.nastepny_kursor ?? undefined,
  });
}

/** Adres podpisany pobierany tuż przed wyświetleniem i odnawiany przed `wygasa_at`. */
export function useAdresZdjecia(rozwiazanieId: string, zdjecieId: string) {
  return useQuery({
    queryKey: kluczeZapytan.adresZdjecia(rozwiazanieId, zdjecieId),
    queryFn: () => pobierzAdresZdjecia(rozwiazanieId, zdjecieId),
    staleTime: Infinity,
    gcTime: 0,
    refetchInterval: (query) => {
      const wygasa = query.state.data?.wygasa_at;
      if (!wygasa) return false;
      return Math.max(5_000, Date.parse(wygasa) - Date.now() - 30_000);
    },
  });
}
