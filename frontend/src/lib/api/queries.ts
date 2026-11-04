// @Role: Zapytania TanStack Query dla danych Ucznia, Działów i uproszczonego Profilu (wspólne klucze i unieważnianie) | @DependsOn: lib/api/client, @tanstack/react-query | @UsedBy: features/accounts, features/profile | @Task: TASK-134
"use client";

import { useQuery } from "@tanstack/react-query";
import { pobierzDzialy, pobierzProfilUproszczony, pobierzUcznia } from "./client";

export const kluczeZapytan = {
  uczen: ["uczen"] as const,
  dzialy: ["dzialy"] as const,
  profilUproszczony: ["profil-uproszczony"] as const,
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
