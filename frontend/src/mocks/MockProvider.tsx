// @Role: Uruchamia Service Worker MSW tylko gdy NEXT_PUBLIC_LEMAT_API=mock (import dynamiczny — kod MSW nie trafia do buildu real) i wstrzymuje render dzieci do gotowości workera | @DependsOn: mocks/browser (dynamicznie), react | @UsedBy: app/layout.tsx | @Task: TASK-132
"use client";

import { useEffect, useState, type ReactNode } from "react";

export function MockProvider({ children }: { children: ReactNode }) {
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

  return gotowy ? <>{children}</> : null;
}
