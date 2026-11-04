// @Role: Zapytania TanStack Query dla danych Ucznia, Działów i uproszczonego Profilu (wspólne klucze i unieważnianie) | @DependsOn: lib/api/client, @tanstack/react-query | @UsedBy: features/accounts, features/profile, features/content, features/solutions | @Task: TASK-134, TASK-135, TASK-137
"use client";

import { useQuery } from "@tanstack/react-query";
import {
  pobierzDzialy,
  pobierzProfilUproszczony,
  pobierzUcznia,
  pobierzZadania,
  pobierzZadanie,
} from "./client";

export const kluczeZapytan = {
  uczen: ["uczen"] as const,
  dzialy: ["dzialy"] as const,
  profilUproszczony: ["profil-uproszczony"] as const,
  zadania: (dzialId?: number) => ["zadania", dzialId ?? null] as const,
  zadanie: (zadanieId: string) => ["zadanie", zadanieId] as const,
  rozwiazania: ["rozwiazania"] as const,
  rozwiazanie: (rozwiazanieId: string) => ["rozwiazanie", rozwiazanieId] as const,
  adresZdjecia: (rozwiazanieId: string, zdjecieId: string) =>
    ["adres-zdjecia", rozwiazanieId, zdjecieId] as const,
};

export function useUczen() {
  return useQuery({ queryKey: kluczeZapytan.uczen, queryFn: pobierzUcznia });
}

export function useDzialy() {
  return useQuery({
    queryKey: kluczeZapytan.dzialy,
    queryFn: pobierzDzialy,
    staleTime: 60 * 60 * 1000,
  });
}

export function useProfilUproszczony() {
  return useQuery({
    queryKey: kluczeZapytan.profilUproszczony,
    queryFn: pobierzProfilUproszczony,
  });
}

export function useZadania(dzialId?: number) {
  return useQuery({
    queryKey: kluczeZapytan.zadania(dzialId),
    queryFn: () => pobierzZadania(dzialId === undefined ? {} : { dzial_id: dzialId }),
  });
}

export function useZadanie(zadanieId: string) {
  return useQuery({
    queryKey: kluczeZapytan.zadanie(zadanieId),
    queryFn: () => pobierzZadanie(zadanieId),
  });
}
