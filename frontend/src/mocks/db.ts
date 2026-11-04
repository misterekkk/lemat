// @Role: Stan mocka w pamięci (konta, sesje, Rozwiązania) i przejścia stanów na timerach 3000/2000 ms wg POC_CONTRACT 7.1 i 7.11 | @DependsOn: mocks/fixtures/*, lib/api/types | @UsedBy: mocks/handlers, mocks/*.test | @Task: TASK-132
import type {
  OcenaWyjscie,
  PoziomMatury,
  ProfilUproszczonyWyjscie,
  RozwiazanieWyjscie,
  ScenariuszMock,
  SesjaWyjscie,
  StatusRozwiazania,
  TranskrypcjaWyjscie,
  UczenWyjscie,
  ZadanieUczniaWyjscie,
} from "@/lib/api/types";
import { KONTA_DEMO, PIERWSZY_NUMER_KONTA_REJESTROWANEGO } from "./fixtures/konta";
import { KRYTERIA_PER_ZADANIE, OCENY_PER_ZADANIE, zbudujUzasadnienie } from "./fixtures/oceny";
import { KROKI_PER_ZADANIE, zbudujKrok } from "./fixtures/transkrypcje";
import { ZADANIA } from "./fixtures/zadania";

/** Czasy z kontraktu 7.1 (MSW bez opóźnienia „podjęcia” pracy). */
export const OPOZNIENIE_ROZPOZNANIA_MS = 3000;
export const OPOZNIENIE_OCENY_MS = 2000;
export const WAZNOSC_TOKENU_MS = 15 * 60 * 1000;
const KLUCZ_STANU = "lemat_mock_stan";

export interface Konto {
  id: string;
  email: string;
  haslo: string;
  imie: string;
  nazwisko: string;
  poziom_matury: PoziomMatury | null;
  termin_egzaminu: string | null;
  wybrany_dzial_id: number | null;
  zgoda_na_trening: boolean;
  version: number;
  demo: boolean;
}

export interface RozwiazanieMocka {
  uczenId: string;
  numerZadania: number;
  scenariusz: ScenariuszMock;
  wyjscie: RozwiazanieWyjscie;
}

interface Sesja {
  uczenId: string;
  wygasaMs: number;
}

interface StanTrwaly {
  rt: { token: string; uczenId: string } | null;
  rejestracje: Konto[];
  licznikKont: number;
}

interface Stan {
  konta: Konto[];
  tokeny: Map<string, Sesja>;
  rozwiazania: RozwiazanieMocka[];
  licznikRozwiazan: number;
  licznikKont: number;
  licznikTokenow: number;
  timery: ReturnType<typeof setTimeout>[];
  zaladowano: boolean;
}

function nowyStan(): Stan {
  return {
    konta: KONTA_DEMO.map((k) => ({
      ...k,
      poziom_matury: null,
      termin_egzaminu: null,
      wybrany_dzial_id: null,
      zgoda_na_trening: false,
      version: 1,
      demo: true,
    })),
    tokeny: new Map(),
    rozwiazania: [],
    licznikRozwiazan: 10,
    licznikKont: PIERWSZY_NUMER_KONTA_REJESTROWANEGO,
    licznikTokenow: 0,
    timery: [],
    zaladowano: false,
  };
}

let stan = nowyStan();

// --- Trwałość tylko w trybie MSW: refresh token (odpowiednik cookie lemat_rt) i konta z rejestracji ---
const pamiec = new Map<string, string>();
const magazynPamieciowy: Pick<Storage, "getItem" | "setItem" | "removeItem"> = {
  getItem: (k) => pamiec.get(k) ?? null,
  setItem: (k, v) => void pamiec.set(k, v),
  removeItem: (k) => void pamiec.delete(k),
};
/** localStorage w przeglądarce (i jsdom); w Node (Vitest/Playwright) magazyn w pamięci procesu. */
function magazyn(): Pick<Storage, "getItem" | "setItem" | "removeItem"> {
  try {
    if (typeof window !== "undefined" && window.localStorage) return window.localStorage;
  } catch {
    // dostęp zablokowany — magazyn w pamięci
  }
  return magazynPamieciowy;
}
function odczytajTrwaly(): StanTrwaly {
  try {
    const surowy = magazyn().getItem(KLUCZ_STANU);
    if (surowy) return JSON.parse(surowy) as StanTrwaly;
  } catch {
    // uszkodzony wpis — start od zera
  }
  return { rt: null, rejestracje: [], licznikKont: PIERWSZY_NUMER_KONTA_REJESTROWANEGO };
}
function zapiszTrwaly(zmiana: Partial<StanTrwaly> = {}): void {
  const nowy: StanTrwaly = {
    ...odczytajTrwaly(),
    rejestracje: stan.konta.filter((k) => !k.demo),
    licznikKont: stan.licznikKont,
    ...zmiana,
  };
  try {
    magazyn().setItem(KLUCZ_STANU, JSON.stringify(nowy));
  } catch {
    // brak magazynu — mock działa w pamięci
  }
}
function zapewnijZaladowanie(): void {
  if (stan.zaladowano) return;
  stan.zaladowano = true;
  const trwaly = odczytajTrwaly();
  stan.konta.push(...trwaly.rejestracje);
  stan.licznikKont = Math.max(stan.licznikKont, trwaly.licznikKont);
}

/** Czyści stan w pamięci, timery i wpis w localStorage (testy, przeładowanie). */
export function resetujStan(): void {
  for (const t of stan.timery) clearTimeout(t);
  try {
    magazyn().removeItem(KLUCZ_STANU);
  } catch {
    // ignoruj
  }
  stan = nowyStan();
  stan.zaladowano = true;
}

// --- Pomocnicze ---
export function terazIso(): string {
  return new Date().toISOString().replace(/\.\d{3}Z$/, "Z");
}
function uuid(prefiks: string, numer: number): string {
  return `${prefiks}-0000-4000-8000-${String(numer).padStart(12, "0")}`;
}
export const CZY_UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// --- Konta i sesje ---
export function znajdzKontoPoEmailu(email: string): Konto | undefined {
  zapewnijZaladowanie();
  const wzorzec = email.trim().toLowerCase();
  return stan.konta.find((k) => k.email.toLowerCase() === wzorzec);
}
export function znajdzKontoPoId(id: string): Konto | undefined {
  zapewnijZaladowanie();
  return stan.konta.find((k) => k.id === id);
}
export function dodajKonto(dane: Pick<Konto, "email" | "haslo" | "imie" | "nazwisko">): Konto {
  zapewnijZaladowanie();
  const konto: Konto = {
    ...dane,
    id: uuid("c1000000", stan.licznikKont),
    poziom_matury: null,
    termin_egzaminu: null,
    wybrany_dzial_id: null,
    zgoda_na_trening: false,
    version: 1,
    demo: false,
  };
  stan.licznikKont += 1;
  stan.konta.push(konto);
  zapiszTrwaly();
  return konto;
}

function nowyToken(prefiks: string): string {
  stan.licznikTokenow += 1;
  return `${prefiks}.${stan.licznikTokenow}.${Math.random().toString(36).slice(2, 12)}`;
}

/** Wystawia token dostępu (15 min) i rotuje „cookie” refresh (localStorage). */
export function wystawSesje(konto: Konto): SesjaWyjscie {
  const token = nowyToken("mock-access");
  const wygasaMs = Date.now() + WAZNOSC_TOKENU_MS;
  stan.tokeny.set(token, { uczenId: konto.id, wygasaMs });
  zapiszTrwaly({ rt: { token: nowyToken("mock-refresh"), uczenId: konto.id } });
  return {
    access_token: token,
    wygasa_at: new Date(wygasaMs).toISOString().replace(/\.\d{3}Z$/, "Z"),
    uzytkownik: {
      id: konto.id,
      rola: "UCZEN",
      imie: konto.imie,
      flagi: { weryfikator_globalny: false, recenzent_tresci: false },
    },
  };
}

/** Rotacja refresh tokenu; zwraca null, gdy brak/nieznany (SESJA_UNIEWAZNIONA). */
export function odswiezSesjeMocka(): SesjaWyjscie | null {
  zapewnijZaladowanie();
  const rt = odczytajTrwaly().rt;
  if (!rt) return null;
  const konto = znajdzKontoPoId(rt.uczenId);
  if (!konto) {
    zapiszTrwaly({ rt: null });
    return null;
  }
  return wystawSesje(konto);
}
export function uniewaznijRefresh(): void {
  zapiszTrwaly({ rt: null });
}
export function uniewaznijTokenyUcznia(uczenId: string): void {
  for (const [token, sesja] of stan.tokeny) {
    if (sesja.uczenId === uczenId) stan.tokeny.delete(token);
  }
}

/** Konto właściciela tokenu dostępu albo null (brak, nieznany, wygasły). */
export function kontoZTokenu(token: string | null): Konto | null {
  zapewnijZaladowanie();
  if (!token) return null;
  const sesja = stan.tokeny.get(token);
  if (!sesja || sesja.wygasaMs <= Date.now()) return null;
  return znajdzKontoPoId(sesja.uczenId) ?? null;
}

export function uczenWyjscie(konto: Konto): UczenWyjscie {
  return {
    id: konto.id,
    email: konto.email,
    imie: konto.imie,
    nazwisko: konto.nazwisko,
    poziom_matury: konto.poziom_matury,
    termin_egzaminu: konto.termin_egzaminu,
    wybrany_dzial_id: konto.wybrany_dzial_id,
    zgoda_na_trening: konto.zgoda_na_trening,
    test_diagnostyczny: "BRAK",
    klasy: [],
    version: konto.version,
  };
}
export function zapiszZmianyKonta(): void {
  zapiszTrwaly();
}

// --- Zadania ---
export function znajdzZadaniePoWersji(zadanieWersjaId: string): ZadanieUczniaWyjscie | undefined {
  return ZADANIA.find((z) => z.zadanie_wersja_id === zadanieWersjaId);
}
export function numerZadania(zadanie: ZadanieUczniaWyjscie): number {
  return Number(zadanie.id.slice(-12));
}

// --- Rozwiązania ---
export function rozwiazaniaUcznia(uczenId: string): RozwiazanieMocka[] {
  return stan.rozwiazania.filter((r) => r.uczenId === uczenId);
}
export function znajdzRozwiazanie(id: string, uczenId: string): RozwiazanieMocka | undefined {
  return stan.rozwiazania.find((r) => r.wyjscie.id === id && r.uczenId === uczenId);
}

const STATUSY_ODPYTYWANE: StatusRozwiazania[] = [
  "PRZESLANE",
  "ROZPOZNAWANIE",
  "ZATWIERDZONE",
  "OCENIANIE",
];
const STATUSY_KONCZACE: StatusRozwiazania[] = [
  "OCENIONE",
  "BLAD_ROZPOZNAWANIA",
  "PORZUCONE",
  "ANULOWANE",
];

/** Rozwiązanie tego Zadania, które blokuje nowe przesłanie (7.4). */
export function znajdzNieukonczone(
  uczenId: string,
  zadanieId: string,
): RozwiazanieMocka | undefined {
  return rozwiazaniaUcznia(uczenId).find(
    (r) => r.wyjscie.zadanie_id === zadanieId && !STATUSY_KONCZACE.includes(r.wyjscie.status),
  );
}

function ustawStatus(
  r: RozwiazanieMocka,
  status: StatusRozwiazania,
  zmiana: Partial<RozwiazanieWyjscie> = {},
): void {
  r.wyjscie = {
    ...r.wyjscie,
    ...zmiana,
    status,
    version: r.wyjscie.version + 1,
    updated_at: terazIso(),
    nastepne_odpytanie_ms: STATUSY_ODPYTYWANE.includes(status) ? 1000 : null,
  };
}
function zaplanuj(fn: () => void, opoznienieMs: number): void {
  stan.timery.push(setTimeout(fn, opoznienieMs));
}

function zbudujTranskrypcje(r: RozwiazanieMocka, numer: number): TranskrypcjaWyjscie {
  return {
    id: uuid("f1000000", numer),
    wersja: 1,
    zrodlo: "AGENT_ROZPOZNAJACY",
    przeslanie_nr: 1,
    zgodnosc_z_zadaniem: 0.95,
    kroki: (KROKI_PER_ZADANIE[r.numerZadania] ?? []).map((k, i) => zbudujKrok(i + 1, k)),
    created_at: terazIso(),
    zakres_poprawek: null,
    konflikt_filtra: false,
    zgodnosc_poprawek: null,
  };
}

function zbudujOcene(r: RozwiazanieMocka, numer: number): OcenaWyjscie {
  const dane = OCENY_PER_ZADANIE[r.numerZadania];
  const kryteria = KRYTERIA_PER_ZADANIE[r.numerZadania] ?? [];
  const punktyKryteriow = dane?.punktyKryteriow ?? [];
  return {
    id: uuid("f2000000", numer),
    wersja: 1,
    status: "OSTATECZNA",
    zrodlo: "AUTOMATYCZNA",
    widocznosc_dla_ucznia: "PELNA",
    punkty: punktyKryteriow.reduce((a, b) => a + b, 0),
    punkty_max: kryteria.length,
    wariant: 1,
    kryteria: kryteria.map((k, i) => ({
      nr_kryterium: k.nr,
      opis: k.opis,
      punkty: punktyKryteriow[i] ?? 0,
      punkty_max: 1,
      prawdopodobienstwo: 0.995,
    })),
    pierwszy_bledny_krok: dane?.pierwszyBlednyKrok ?? null,
    brakujacy_krok: false,
    typ_bledu: dane?.typBledu ?? null,
    krok_podejrzany_odczyt: null,
    mozliwy_blad_odczytu: false,
    wskazowka: dane?.wskazowka ?? null,
    wskazowka_w_przygotowaniu: false,
    uzasadnienie: zbudujUzasadnienie(r.numerZadania),
    pewnosc_oceny: 0.995,
    komunikat: null,
    mozliwe_zgloszenie_do: null,
    created_at: terazIso(),
  };
}

/** POST /rozwiazania: tworzy Rozwiązanie w PRZESLANE i planuje przejścia (7.11). */
export function utworzRozwiazanie(
  konto: Konto,
  zadanie: ZadanieUczniaWyjscie,
  liczbaZdjec: number,
  scenariusz: ScenariuszMock,
): RozwiazanieWyjscie {
  const numer = stan.licznikRozwiazan;
  stan.licznikRozwiazan += 1;
  const teraz = terazIso();
  const r: RozwiazanieMocka = {
    uczenId: konto.id,
    numerZadania: numerZadania(zadanie),
    scenariusz,
    wyjscie: {
      id: uuid("d1000000", numer),
      zadanie_id: zadanie.id,
      zadanie_wersja_id: zadanie.zadanie_wersja_id,
      status: "PRZESLANE",
      element_sciezki_id: uuid("e5000000", numer),
      przypisanie_id: null,
      zdjecia: Array.from({ length: liczbaZdjec }, (_, i) => ({
        id: uuid("e1000000", numer + i * 1000),
        przeslanie_nr: 1,
        kolejnosc: i + 1,
        dostepne: true,
      })),
      liczba_przeslan: 1,
      liczba_ponowien_rozpoznania: 0,
      kod_bledu_rozpoznawania: null,
      transkrypcja: null,
      ocena: null,
      weryfikacja: null,
      version: 1,
      created_at: teraz,
      updated_at: teraz,
      nastepne_odpytanie_ms: 1000,
      szkic_transkrypcji: null,
    },
  };
  stan.rozwiazania.push(r);
  const pierwsze = r.wyjscie;

  zaplanuj(() => ustawStatus(r, "ROZPOZNAWANIE"), 0);
  zaplanuj(() => {
    if (r.scenariusz === "rozpoznanie-nieczytelne") {
      ustawStatus(r, "BLAD_ROZPOZNAWANIA", { kod_bledu_rozpoznawania: "NIECZYTELNE" });
    } else {
      ustawStatus(r, "TRANSKRYPCJA_GOTOWA", { transkrypcja: zbudujTranskrypcje(r, numer) });
    }
  }, OPOZNIENIE_ROZPOZNANIA_MS);
  return pierwsze;
}

/** POST …/zatwierdzenie: ZATWIERDZONE od razu, potem OCENIANIE → OCENIONE po 2 s. */
export function zatwierdzRozwiazanieMocka(r: RozwiazanieMocka): RozwiazanieWyjscie {
  const numer = Number(r.wyjscie.id.slice(-12));
  ustawStatus(r, "ZATWIERDZONE");
  const odpowiedz = r.wyjscie;
  zaplanuj(() => ustawStatus(r, "OCENIANIE"), 0);
  zaplanuj(() => ustawStatus(r, "OCENIONE", { ocena: zbudujOcene(r, numer) }), OPOZNIENIE_OCENY_MS);
  return odpowiedz;
}

// --- Profil uproszczony (4.3) ---
function zaokraglij1(x: number): number {
  return Math.round(x * 10) / 10;
}
export function profilUproszczony(uczenId: string): ProfilUproszczonyWyjscie {
  const wszystkie = rozwiazaniaUcznia(uczenId).filter(
    (r) => r.wyjscie.status !== "ANULOWANE" && r.wyjscie.status !== "PORZUCONE",
  );
  const ocenione = wszystkie.filter((r) => r.wyjscie.status === "OCENIONE" && r.wyjscie.ocena);
  const suma = ocenione.reduce((a, r) => a + (r.wyjscie.ocena?.punkty ?? 0), 0);
  const sumaMax = ocenione.reduce((a, r) => a + (r.wyjscie.ocena?.punkty_max ?? 0), 0);

  const dzialy = new Map<number, { nazwa: string; n: number; p: number; max: number }>();
  for (const r of ocenione) {
    const zadanie = ZADANIA.find((z) => z.id === r.wyjscie.zadanie_id);
    if (!zadanie) continue;
    const wpis = dzialy.get(zadanie.dzial.id) ?? { nazwa: zadanie.dzial.nazwa, n: 0, p: 0, max: 0 };
    wpis.n += 1;
    wpis.p += r.wyjscie.ocena?.punkty ?? 0;
    wpis.max += r.wyjscie.ocena?.punkty_max ?? 0;
    dzialy.set(zadanie.dzial.id, wpis);
  }

  const ostatnie = [...ocenione]
    .sort((a, b) => {
      const ca = a.wyjscie.ocena?.created_at ?? "";
      const cb = b.wyjscie.ocena?.created_at ?? "";
      return ca === cb ? b.wyjscie.id.localeCompare(a.wyjscie.id) : cb.localeCompare(ca);
    })
    .slice(0, 5)
    .map((r) => {
      const zadanie = ZADANIA.find((z) => z.id === r.wyjscie.zadanie_id);
      return {
        rozwiazanie_id: r.wyjscie.id,
        zadanie_id: r.wyjscie.zadanie_id,
        dzial: zadanie?.dzial ?? { id: 0, nazwa: "" },
        punkty: r.wyjscie.ocena?.punkty ?? 0,
        punkty_max: r.wyjscie.ocena?.punkty_max ?? 0,
        oceniono_at: r.wyjscie.ocena?.created_at ?? r.wyjscie.updated_at,
      };
    });

  return {
    uczen_id: uczenId,
    liczba_rozwiazan: wszystkie.length,
    liczba_ocenionych: ocenione.length,
    suma_punktow: suma,
    suma_punktow_max: sumaMax,
    sredni_procent_punktow: sumaMax > 0 ? zaokraglij1((100 * suma) / sumaMax) : null,
    dzialy: [...dzialy.entries()]
      .sort(([a], [b]) => a - b)
      .map(([id, w]) => ({
        dzial: { id, nazwa: w.nazwa },
        liczba_ocenionych: w.n,
        sredni_procent_punktow: zaokraglij1((100 * w.p) / w.max),
      })),
    ostatnie_oceny: ostatnie,
  };
}
