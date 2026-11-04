// @Role: Model błędów klienta API: ProblemDetailsLemat (RFC 9457), BladApi, kody serwera i kody tylko-klienckie | @DependsOn: docs/POC_CONTRACT.md 1.1-1.2 | @UsedBy: lib/api/client, features/* (komunikaty błędów) | @Task: TASK-131

export interface BladPola {
  pole: string;
  kod_reguly: string;
  komunikat: string;
}

export interface ProblemDetailsLemat {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance: string;
  kod: string;
  trace_id: string;
  szczegoly: Record<string, unknown> | null;
  bledy_pol?: BladPola[];
}

/** Kody zwracane przez serwer w POC (kontrakt 1.2). */
export const KODY_SERWERA = [
  "ZAPYTANIE_NIEPRAWIDLOWE",
  "KURSOR_NIEPRAWIDLOWY",
  "NAGLOWEK_NIEPRAWIDLOWY",
  "NIEUWIERZYTELNIONY",
  "SESJA_UNIEWAZNIONA",
  "BRAK_UPRAWNIEN",
  "KONTO_NIEAKTYWNE",
  "NIE_ZNALEZIONO",
  "METODA_NIEDOZWOLONA",
  "EMAIL_ZAJETY",
  "ROZWIAZANIE_NIEPRAWIDLOWY_STAN",
  "TRANSKRYPCJA_NIEAKTUALNA",
  "WERSJA_ZADANIA_NIEAKTUALNA",
  "KONFLIKT_WERSJI",
  "ZAPYTANIE_ZA_DUZE",
  "ZDJECIE_ZA_DUZE",
  "NIEOBSLUGIWANY_FORMAT",
  "WALIDACJA",
  "ZA_DUZO_ZDJEC",
  "ZDJECIE_NIEPRAWIDLOWE",
  "WYMAGANY_IF_MATCH",
  "BLAD_WEWNETRZNY",
  "BAZA_NIEDOSTEPNA",
] as const;

/** Kody generowane wyłącznie po stronie klienta (nigdy z serwera). */
export const KODY_KLIENTA = [
  "BLAD_SIECI",
  "PRZERWANO",
  "BLAD_BRAMY",
  "SESJA_TYMCZASOWO_NIEDOSTEPNA",
] as const;

export const WSZYSTKIE_KODY_BLEDOW = [...KODY_SERWERA, ...KODY_KLIENTA] as const;

export type KodBledu = (typeof WSZYSTKIE_KODY_BLEDOW)[number];

export function czyZnanyKod(kod: string): kod is KodBledu {
  return (WSZYSTKIE_KODY_BLEDOW as readonly string[]).includes(kod);
}

/** Klucz w messages/pl-PL.json (`bledy.<KOD>`) dla znanego kodu, w przeciwnym razie null. */
export function kluczKomunikatuBledu(kod: string): string | null {
  return czyZnanyKod(kod) ? `bledy.${kod}` : null;
}

export class BladApi extends Error {
  readonly status: number;
  readonly kod: string;
  readonly problem: ProblemDetailsLemat | null;
  readonly bledyPol: BladPola[];

  constructor(opcje: {
    status: number;
    kod: string;
    problem?: ProblemDetailsLemat | null;
    wiadomosc?: string;
  }) {
    super(opcje.problem?.detail ?? opcje.wiadomosc ?? opcje.kod);
    this.name = "BladApi";
    this.status = opcje.status;
    this.kod = opcje.kod;
    this.problem = opcje.problem ?? null;
    this.bledyPol = opcje.problem?.bledy_pol ?? [];
  }

  get szczegoly(): Record<string, unknown> | null {
    return this.problem?.szczegoly ?? null;
  }

  /** Dla 412 KONFLIKT_WERSJI: aktualny zasób z `szczegoly.aktualny`. */
  get aktualny(): unknown {
    return this.szczegoly?.["aktualny"] ?? null;
  }
}

export function czyBladApi(e: unknown): e is BladApi {
  return e instanceof BladApi;
}
