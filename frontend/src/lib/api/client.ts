// @Role: Jedyny klient HTTP do /api/v1 (fetch): Bearer, If-Match, multipart, merge-patch, jednokrotne odświeżenie po 401, mapowanie błędów na BladApi + typowane funkcje endpointów | @DependsOn: lib/api/types, lib/api/errors, docs/POC_CONTRACT.md | @UsedBy: features/* (przez index.ts), mocks (TASK-132), sesja (TASK-133) | @Task: TASK-131
import { BladApi, type ProblemDetailsLemat } from "./errors";
import type {
  AdresZdjeciaWyjscie,
  DzialWyjscie,
  LogowanieWejscie,
  OcenaWyjscie,
  ParametryRozwiazan,
  ParametryZadan,
  PrzeslanieRozwiazaniaWejscie,
  ProfilUproszczonyWyjscie,
  RejestracjaWejscie,
  RejestracjaWyjscie,
  RozwiazanieWyjscie,
  SesjaWyjscie,
  Strona,
  UczenWyjscie,
  ZadanieUczniaWyjscie,
  ZatwierdzenieWejscie,
  ZmianaUczniaWejscie,
} from "./types";

export interface KonfiguracjaKlienta {
  /** Zwraca bieżący token dostępu z pamięci (nigdy z localStorage). */
  pobierzToken: () => string | null;
  /** Odświeża sesję (single-flight po stronie wywołującego); zwraca nowy token albo null. */
  odswiezSesje: () => Promise<string | null>;
  /** Wstrzykiwany fetch (testy); domyślnie globalny. */
  fetchImpl?: typeof fetch;
  /** Adres bazowy; domyślnie /api/v1 (ten sam origin). */
  bazowyAdres?: string;
}

const BAZOWY_ADRES = "/api/v1";

let konfiguracja: KonfiguracjaKlienta = {
  pobierzToken: () => null,
  odswiezSesje: () => Promise.resolve(null),
};

export function skonfigurujKlienta(nowa: KonfiguracjaKlienta): void {
  konfiguracja = nowa;
}

type TypCiala = "json" | "merge-patch" | "multipart";

interface OpcjeZadania {
  metoda: "GET" | "POST" | "PATCH";
  sciezka: string;
  zapytanie?: Record<string, string | number | undefined>;
  cialo?: unknown;
  typCiala?: TypCiala;
  ifMatch?: number;
  naglowki?: Record<string, string>;
  sygnal?: AbortSignal;
  /** Endpoint anonimowy: bez Authorization i bez odświeżania po 401. */
  anonimowy?: boolean;
}

function zbudujAdres(sciezka: string, zapytanie: OpcjeZadania["zapytanie"]): string {
  const baza = konfiguracja.bazowyAdres ?? BAZOWY_ADRES;
  const params = new URLSearchParams();
  for (const [klucz, wartosc] of Object.entries(zapytanie ?? {})) {
    if (wartosc !== undefined) params.set(klucz, String(wartosc));
  }
  const tekst = params.toString();
  return `${baza}${sciezka}${tekst ? `?${tekst}` : ""}`;
}

function bladSieci(e: unknown): BladApi {
  const przerwano = e instanceof DOMException && e.name === "AbortError";
  return new BladApi({
    status: 0,
    kod: przerwano ? "PRZERWANO" : "BLAD_SIECI",
    wiadomosc: e instanceof Error ? e.message : undefined,
  });
}

async function odczytajOdpowiedz<T>(odp: Response): Promise<T> {
  const typ = odp.headers.get("content-type") ?? "";
  if (!odp.ok) {
    if (typ.includes("application/problem+json")) {
      try {
        const problem = (await odp.json()) as ProblemDetailsLemat;
        if (typeof problem?.kod === "string") {
          return Promise.reject(new BladApi({ status: odp.status, kod: problem.kod, problem }));
        }
      } catch {
        // niepoprawne cialo_http — traktowane jak błąd bramy
      }
    }
    return Promise.reject(new BladApi({ status: odp.status, kod: "BLAD_BRAMY" }));
  }
  if (odp.status === 204) return undefined as T;
  if (!typ.includes("json")) {
    return Promise.reject(new BladApi({ status: odp.status, kod: "BLAD_BRAMY" }));
  }
  try {
    return (await odp.json()) as T;
  } catch {
    return Promise.reject(new BladApi({ status: odp.status, kod: "BLAD_BRAMY" }));
  }
}

async function wyslij<T>(opcje: OpcjeZadania, token: string | null): Promise<T> {
  const naglowki = new Headers(opcje.naglowki);
  if (token && !opcje.anonimowy) naglowki.set("Authorization", `Bearer ${token}`);
  if (opcje.ifMatch !== undefined) naglowki.set("If-Match", `"${opcje.ifMatch}"`);

  let cialo_http: BodyInit | undefined;
  if (opcje.cialo !== undefined) {
    if (opcje.typCiala === "multipart") {
      // Content-Type (z boundary) ustawia przeglądarka — nie ustawiamy go ręcznie.
      cialo_http = opcje.cialo as FormData;
    } else {
      naglowki.set(
        "Content-Type",
        opcje.typCiala === "merge-patch" ? "application/merge-patch+json" : "application/json",
      );
      cialo_http = JSON.stringify(opcje.cialo);
    }
  }

  const wykonaj = konfiguracja.fetchImpl ?? ((...a: Parameters<typeof fetch>) => fetch(...a));
  let odp: Response;
  try {
    odp = await wykonaj(zbudujAdres(opcje.sciezka, opcje.zapytanie), {
      method: opcje.metoda,
      headers: naglowki,
      body: cialo_http,
      cache: "no-store",
      credentials: "same-origin",
      signal: opcje.sygnal,
    });
  } catch (e) {
    throw bladSieci(e);
  }
  return odczytajOdpowiedz<T>(odp);
}

async function zadaj<T>(opcje: OpcjeZadania): Promise<T> {
  try {
    return await wyslij<T>(opcje, konfiguracja.pobierzToken());
  } catch (e) {
    if (
      !(e instanceof BladApi) ||
      e.status !== 401 ||
      e.kod !== "NIEUWIERZYTELNIONY" ||
      opcje.anonimowy
    ) {
      throw e;
    }
    // Jedno odświeżenie i jedno ponowienie; drugi 401 kończy się błędem (bez pętli).
    let nowyToken: string | null;
    try {
      nowyToken = await konfiguracja.odswiezSesje();
    } catch (blad) {
      throw blad instanceof BladApi ? blad : e;
    }
    if (!nowyToken) throw e;
    return wyslij<T>(opcje, nowyToken);
  }
}

// --- Uwierzytelnianie (kontrakt 3) ---
export function zarejestruj(wejscie: RejestracjaWejscie): Promise<RejestracjaWyjscie> {
  return zadaj({ metoda: "POST", sciezka: "/auth/rejestracja", cialo: wejscie, anonimowy: true });
}
export function zaloguj(wejscie: LogowanieWejscie): Promise<SesjaWyjscie> {
  return zadaj({ metoda: "POST", sciezka: "/auth/logowanie", cialo: wejscie, anonimowy: true });
}
export function odswiez(sygnal?: AbortSignal): Promise<SesjaWyjscie> {
  return zadaj({ metoda: "POST", sciezka: "/auth/odswiezenie", anonimowy: true, sygnal });
}
export function wyloguj(): Promise<void> {
  return zadaj({ metoda: "POST", sciezka: "/auth/wylogowanie" });
}

// --- Uczeń, Działy, Profil (kontrakt 4) ---
export function pobierzUcznia(): Promise<UczenWyjscie> {
  return zadaj({ metoda: "GET", sciezka: "/uczniowie/ja" });
}
/** `version` pochodzi z ostatniego odczytu UczenWyjscie (nagłówek If-Match). */
export function zmienUcznia(zmiana: ZmianaUczniaWejscie, version: number): Promise<UczenWyjscie> {
  return zadaj({
    metoda: "PATCH",
    sciezka: "/uczniowie/ja",
    cialo: zmiana,
    typCiala: "merge-patch",
    ifMatch: version,
  });
}
export function pobierzProfilUproszczony(): Promise<ProfilUproszczonyWyjscie> {
  return zadaj({ metoda: "GET", sciezka: "/uczniowie/ja/profil-uproszczony" });
}
export function pobierzDzialy(): Promise<DzialWyjscie[]> {
  return zadaj({ metoda: "GET", sciezka: "/dzialy" });
}

// --- Zadania (kontrakt 5) ---
export function pobierzZadania(
  parametry: ParametryZadan = {},
): Promise<Strona<ZadanieUczniaWyjscie>> {
  return zadaj({ metoda: "GET", sciezka: "/zadania", zapytanie: { ...parametry } });
}
export function pobierzZadanie(zadanieId: string): Promise<ZadanieUczniaWyjscie> {
  return zadaj({ metoda: "GET", sciezka: `/zadania/${encodeURIComponent(zadanieId)}` });
}

// --- Rozwiązania (kontrakt 7) ---
export function przeslijRozwiazanie(
  wejscie: PrzeslanieRozwiazaniaWejscie,
): Promise<RozwiazanieWyjscie> {
  const formularz = new FormData();
  formularz.append("zadanie_wersja_id", wejscie.zadanie_wersja_id);
  wejscie.zdjecia.forEach((zdjecie, i) => {
    formularz.append("zdjecia", zdjecie, `kartka-${i + 1}.jpg`);
  });
  return zadaj({
    metoda: "POST",
    sciezka: "/rozwiazania",
    cialo: formularz,
    typCiala: "multipart",
    naglowki: wejscie.scenariusz ? { "X-Lemat-Mock-Scenariusz": wejscie.scenariusz } : undefined,
  });
}
export function pobierzRozwiazanie(
  rozwiazanieId: string,
  sygnal?: AbortSignal,
): Promise<RozwiazanieWyjscie> {
  return zadaj({
    metoda: "GET",
    sciezka: `/rozwiazania/${encodeURIComponent(rozwiazanieId)}`,
    sygnal,
  });
}
export function pobierzRozwiazania(
  parametry: ParametryRozwiazan = {},
): Promise<Strona<RozwiazanieWyjscie>> {
  return zadaj({ metoda: "GET", sciezka: "/rozwiazania", zapytanie: { ...parametry } });
}
/** `version` pochodzi z ostatniego odczytu RozwiazanieWyjscie (nagłówek If-Match). */
export function zatwierdzRozwiazanie(
  rozwiazanieId: string,
  wejscie: ZatwierdzenieWejscie,
  version: number,
): Promise<RozwiazanieWyjscie> {
  return zadaj({
    metoda: "POST",
    sciezka: `/rozwiazania/${encodeURIComponent(rozwiazanieId)}/zatwierdzenie`,
    cialo: wejscie,
    ifMatch: version,
  });
}
export function pobierzOcene(rozwiazanieId: string): Promise<OcenaWyjscie> {
  return zadaj({
    metoda: "GET",
    sciezka: `/rozwiazania/${encodeURIComponent(rozwiazanieId)}/ocena`,
  });
}
export function pobierzAdresZdjecia(
  rozwiazanieId: string,
  zdjecieRozwiazaniaId: string,
): Promise<AdresZdjeciaWyjscie> {
  return zadaj({
    metoda: "GET",
    sciezka: `/rozwiazania/${encodeURIComponent(rozwiazanieId)}/zdjecia/${encodeURIComponent(zdjecieRozwiazaniaId)}/adres`,
  });
}
