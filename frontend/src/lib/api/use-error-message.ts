// @Role: Hook zamieniający BladApi/nieznany błąd na polski komunikat (bledy.<KOD>, w razie braku detail z serwera albo bledy.NIEZNANY) | @DependsOn: next-intl, lib/api/errors, messages/pl-PL.json | @UsedBy: features/accounts | @Task: TASK-133
"use client";

import { useTranslations } from "next-intl";
import { useCallback } from "react";
import { BladApi, czyZnanyKod } from "./errors";

export function useKomunikatBledu(): (blad: unknown) => string {
  const t = useTranslations("bledy");
  return useCallback(
    (blad: unknown) => {
      if (blad instanceof BladApi) {
        if (czyZnanyKod(blad.kod)) return t(blad.kod);
        if (blad.problem?.detail) return blad.problem.detail;
      }
      return t("NIEZNANY");
    },
    [t],
  );
}
