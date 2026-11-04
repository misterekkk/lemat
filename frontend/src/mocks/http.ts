// @Role: Pomocniki odpowiedzi mocka: ProblemDetails RFC 9457 (POC_CONTRACT 1.1/1.2), nagłówki, kursor, paginacja | @DependsOn: msw, lib/api/errors | @UsedBy: mocks/handlers | @Task: TASK-132
import { HttpResponse, type JsonBodyType } from "msw";

export const WERSJA_API = "1.0.0";

/** Tytuły błędów z tabeli 1.2 kontraktu. */
const TYTULY: Record<string, string> = {
  ZAPYTANIE_NIEPRAWIDLOWE: "Niepoprawne żądanie",
  KURSOR_NIEPRAWIDLOWY: "Niepoprawny kursor",
  NAGLOWEK_NIEPRAWIDLOWY: "Niepoprawny nagłówek",
  NIEUWIERZYTELNIONY: "Wymagane logowanie",
  SESJA_UNIEWAZNIONA: "Sesja wygasła",
  BRAK_UPRAWNIEN: "Brak uprawnień",
  KONTO_NIEAKTYWNE: "Konto nieaktywne",
  NIE_ZNALEZIONO: "Nie znaleziono",
  METODA_NIEDOZWOLONA: "Metoda niedozwolona",
  EMAIL_ZAJETY: "Nie udało się założyć konta",
  ROZWIAZANIE_NIEPRAWIDLOWY_STAN: "Nieprawidłowy stan rozwiązania",
  TRANSKRYPCJA_NIEAKTUALNA: "Transkrypcja jest nieaktualna",
  WERSJA_ZADANIA_NIEAKTUALNA: "Treść zadania się zmieniła",
  KONFLIKT_WERSJI: "Dane zmieniły się w międzyczasie",
  ZAPYTANIE_ZA_DUZE: "Żądanie zbyt duże",
  ZDJECIE_ZA_DUZE: "Zdjęcie jest zbyt duże",
  NIEOBSLUGIWANY_FORMAT: "Nieobsługiwany format",
  WALIDACJA: "Błędne dane",
  ZA_DUZO_ZDJEC: "Nieprawidłowa liczba zdjęć",
  ZDJECIE_NIEPRAWIDLOWE: "Nie można odczytać zdjęcia",
  WYMAGANY_IF_MATCH: "Wymagany nagłówek If-Match",
  BLAD_WEWNETRZNY: "Błąd wewnętrzny",
  BAZA_NIEDOSTEPNA: "Usługa chwilowo niedostępna",
};

export interface BladPolaMocka {
  pole: string;
  kod_reguly: string;
  komunikat: string;
}

const NAGLOWKI_STALE = {
  "Cache-Control": "no-store",
  Vary: "Authorization",
  "X-Lemat-Wersja-Api": WERSJA_API,
};

function traceId(): string {
  return Array.from({ length: 32 }, () => Math.floor(Math.random() * 16).toString(16)).join("");
}

export function json(cialo: JsonBodyType, status = 200, naglowki: Record<string, string> = {}) {
  return HttpResponse.json(cialo, {
    status,
    headers: { ...NAGLOWKI_STALE, "Content-Type": "application/json; charset=utf-8", ...naglowki },
  });
}

export function bezTresci(status = 204) {
  return new HttpResponse(null, { status, headers: NAGLOWKI_STALE });
}

export function problem(
  request: Request,
  status: number,
  kod: string,
  detail: string,
  opcje: { szczegoly?: unknown; bledyPol?: BladPolaMocka[] } = {},
) {
  const cialo: Record<string, unknown> = {
    type: `https://lemat.pl/bledy/${kod.toLowerCase().replaceAll("_", "-")}`,
    title: TYTULY[kod] ?? kod,
    status,
    detail,
    instance: new URL(request.url).pathname,
    kod,
    trace_id: traceId(),
    szczegoly: opcje.szczegoly ?? null,
  };
  if (opcje.bledyPol) cialo.bledy_pol = opcje.bledyPol;
  return HttpResponse.json(cialo, {
    status,
    headers: { ...NAGLOWKI_STALE, "Content-Type": "application/problem+json; charset=utf-8" },
  });
}

export function walidacja(request: Request, bledyPol: BladPolaMocka[]) {
  return problem(request, 422, "WALIDACJA", "Popraw zaznaczone pola.", { bledyPol });
}

export function blad(pole: string, kodReguly: string, komunikat: string): BladPolaMocka {
  return { pole, kod_reguly: kodReguly, komunikat };
}

// --- Paginacja kursorowa (kursor nieprzezroczysty dla klienta) ---
export const LIMIT_DOMYSLNY = 50;

export type WynikLimitu = { limit: number; offset: number } | { blad: Response };

export function odczytajStronicowanie(request: Request): WynikLimitu {
  const params = new URL(request.url).searchParams;
  let limit = LIMIT_DOMYSLNY;
  const surowyLimit = params.get("limit");
  if (surowyLimit !== null) {
    const n = Number(surowyLimit);
    if (!Number.isInteger(n) || n < 1 || n > 100) {
      return {
        blad: walidacja(request, [
          blad("limit", "TYP_NIEPRAWIDLOWY", "Limit musi być liczbą całkowitą od 1 do 100."),
        ]),
      };
    }
    limit = n;
  }
  let offset = 0;
  const kursor = params.get("kursor");
  if (kursor !== null) {
    try {
      const odczyt = JSON.parse(atob(kursor)) as { o?: unknown };
      if (typeof odczyt.o !== "number" || !Number.isInteger(odczyt.o) || odczyt.o < 0) {
        throw new Error("zly kursor");
      }
      offset = odczyt.o;
    } catch {
      return { blad: problem(request, 400, "KURSOR_NIEPRAWIDLOWY", "Niepoprawny kursor.") };
    }
  }
  return { limit, offset };
}

export function strona<T>(elementy: T[], limit: number, offset: number) {
  const nastepny = offset + limit;
  return {
    elementy: elementy.slice(offset, nastepny),
    nastepny_kursor: nastepny < elementy.length ? btoa(JSON.stringify({ o: nastepny })) : null,
  };
}
