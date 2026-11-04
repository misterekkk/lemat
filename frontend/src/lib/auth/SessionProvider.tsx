// @Role: Sesja Ucznia: token dostępu tylko w pamięci, jedno odświeżenie przy starcie (single-flight), proaktywne odświeżenie 60 s przed wygaśnięciem, wstrzyknięcie dostawcy tokenu do klienta API, logowanie/rejestracja/wylogowanie | @DependsOn: lib/api/client, lib/api/errors, lib/api/types, @tanstack/react-query | @UsedBy: app/layout.tsx, features/accounts | @Task: TASK-133
"use client";

import { useQueryClient } from "@tanstack/react-query";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import * as api from "@/lib/api/client";
import { czyBladApi } from "@/lib/api/errors";
import type {
  LogowanieWejscie,
  RejestracjaWejscie,
  RejestracjaWyjscie,
  SesjaWyjscie,
  UzytkownikSesji,
} from "@/lib/api/types";

export type StanSesji = "ladowanie" | "zalogowany" | "anonimowy";

export interface KontekstSesji {
  stan: StanSesji;
  uzytkownik: UzytkownikSesji | null;
  zaloguj: (wejscie: LogowanieWejscie) => Promise<void>;
  zarejestruj: (wejscie: RejestracjaWejscie) => Promise<RejestracjaWyjscie>;
  wyloguj: () => Promise<void>;
}

/** Odświeżenie tokenu z takim zapasem przed `wygasa_at` (kontrakt 3.3, ADR-054). */
export const ZAPAS_ODSWIEZENIA_MS = 60_000;

const Kontekst = createContext<KontekstSesji | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const klientZapytan = useQueryClient();
  const [stan, ustawStan] = useState<StanSesji>("ladowanie");
  const [uzytkownik, ustawUzytkownika] = useState<UzytkownikSesji | null>(null);

  // Token dostępu wyłącznie w pamięci (ref) — nigdy w localStorage/sessionStorage/cookie.
  const tokenRef = useRef<string | null>(null);
  const lotRef = useRef<Promise<string | null> | null>(null);
  const zegarRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const zamontowanyRef = useRef(false);
  const odswiezRef = useRef<() => Promise<string | null>>(() => Promise.resolve(null));

  const zatrzymajZegar = useCallback(() => {
    if (zegarRef.current !== null) clearTimeout(zegarRef.current);
    zegarRef.current = null;
  }, []);

  const wyczysc = useCallback(() => {
    tokenRef.current = null;
    zatrzymajZegar();
    klientZapytan.clear();
    if (zamontowanyRef.current) {
      ustawUzytkownika(null);
      ustawStan("anonimowy");
    }
  }, [klientZapytan, zatrzymajZegar]);

  const zastosuj = useCallback(
    (sesja: SesjaWyjscie) => {
      tokenRef.current = sesja.access_token;
      zatrzymajZegar();
      const opoznienie = Math.max(
        0,
        new Date(sesja.wygasa_at).getTime() - Date.now() - ZAPAS_ODSWIEZENIA_MS,
      );
      zegarRef.current = setTimeout(() => {
        odswiezRef.current().catch(() => undefined);
      }, opoznienie);
      if (zamontowanyRef.current) {
        ustawUzytkownika(sesja.uzytkownik);
        ustawStan("zalogowany");
      }
    },
    [zatrzymajZegar],
  );

  /** Single-flight: równoległe wywołania w obrębie karty współdzielą jedno żądanie. */
  const odswiez = useCallback((): Promise<string | null> => {
    if (lotRef.current) return lotRef.current;
    const lot = api
      .odswiez()
      .then((sesja) => {
        zastosuj(sesja);
        return sesja.access_token as string | null;
      })
      .catch((e: unknown) => {
        if (czyBladApi(e) && e.kod === "SESJA_UNIEWAZNIONA") {
          wyczysc();
          return null;
        }
        throw e;
      })
      .finally(() => {
        lotRef.current = null;
      });
    lotRef.current = lot;
    return lot;
  }, [wyczysc, zastosuj]);

  useEffect(() => {
    odswiezRef.current = odswiez;
  }, [odswiez]);

  useEffect(() => {
    zamontowanyRef.current = true;
    api.skonfigurujKlienta({
      pobierzToken: () => tokenRef.current,
      odswiezSesje: () => odswiezRef.current(),
    });
    // Jedno odświeżenie przy starcie: przywraca sesję z ciasteczka lemat_rt albo kończy jako anonimowy.
    odswiezRef
      .current()
      .then((token) => {
        if (!token && zamontowanyRef.current) ustawStan("anonimowy");
      })
      .catch(() => {
        if (zamontowanyRef.current) ustawStan("anonimowy");
      });
    return () => {
      zamontowanyRef.current = false;
      zatrzymajZegar();
      api.skonfigurujKlienta({
        pobierzToken: () => null,
        odswiezSesje: () => Promise.resolve(null),
      });
    };
  }, [zatrzymajZegar]);

  const zaloguj = useCallback(
    async (wejscie: LogowanieWejscie) => {
      const sesja = await api.zaloguj(wejscie);
      klientZapytan.clear();
      zastosuj(sesja);
    },
    [klientZapytan, zastosuj],
  );

  const zarejestruj = useCallback(
    async (wejscie: RejestracjaWejscie) => {
      const wynik = await api.zarejestruj(wejscie);
      if (wynik.sesja) {
        klientZapytan.clear();
        zastosuj(wynik.sesja);
      }
      return wynik;
    },
    [klientZapytan, zastosuj],
  );

  const wyloguj = useCallback(async () => {
    try {
      await api.wyloguj();
    } catch {
      // Sesja lokalna i tak jest czyszczona (np. wygasły token).
    } finally {
      wyczysc();
    }
  }, [wyczysc]);

  const wartosc = useMemo<KontekstSesji>(
    () => ({ stan, uzytkownik, zaloguj, zarejestruj, wyloguj }),
    [stan, uzytkownik, zaloguj, zarejestruj, wyloguj],
  );

  return <Kontekst.Provider value={wartosc}>{children}</Kontekst.Provider>;
}

export function useSession(): KontekstSesji {
  const wartosc = useContext(Kontekst);
  if (!wartosc) throw new Error("useSession wymaga SessionProvider.");
  return wartosc;
}
