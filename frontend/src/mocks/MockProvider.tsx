// @Role: Uruchamia Service Worker MSW tylko gdy NEXT_PUBLIC_LEMAT_API=mock (import dynamiczny — kod MSW nie trafia do buildu real) i wstrzymuje render dzieci do gotowości workera; w trybie mock pokazuje baner „Dane przykładowe” | @DependsOn: mocks/browser (dynamicznie), react, next-intl | @UsedBy: app/layout.tsx | @Task: TASK-132, TASK-138
"use client";

import { useTranslations } from "next-intl";
import { useEffect, useState, type ReactNode } from "react";

export function MockProvider({ children }: { children: ReactNode }) {
  const t = useTranslations("Mock");
  const [gotowy, ustawGotowy] = useState(process.env.NEXT_PUBLIC_LEMAT_API !== "mock");

  useEffect(() => {
    let anulowano = false;
    // Warunek stały w buildzie (NEXT_PUBLIC_*): w trybie real blok jest usuwany razem z importem MSW.
    if (process.env.NEXT_PUBLIC_LEMAT_API === "mock") {
      void import("./browser")
        .then((modul) => modul.uruchomMock())
        .then(() => {
          if (!anulowano) ustawGotowy(true);
        });
    }
    return () => {
      anulowano = true;
    };
  }, []);

  if (!gotowy) return null;
  return (
    <>
      {process.env.NEXT_PUBLIC_LEMAT_API === "mock" && (
        <div
          role="note"
          className="bg-amber-100 px-4 py-2 text-center text-sm font-medium text-amber-950"
        >
          {t("baner")}
        </div>
      )}
      {children}
    </>
  );
}
