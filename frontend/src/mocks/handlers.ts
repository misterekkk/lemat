// @Role: Handlery MSW dla endpointów 1-18 kontraktu POC (stanowe, błędy w kształcie 1.1, walidacje 7.4) | @DependsOn: msw, mocks/db, mocks/http, mocks/fixtures/* | @UsedBy: mocks/browser, mocks/server | @Task: TASK-132
import { HttpResponse, http } from "msw";
import type { ScenariuszMock, ZatwierdzenieWejscie } from "@/lib/api/types";
import {
  CZY_UUID,
  dodajKonto,
  kontoZTokenu,
  odswiezSesjeMocka,
  profilUproszczony,
  rozwiazaniaUcznia,
  uczenWyjscie,
  uniewaznijRefresh,
  uniewaznijTokenyUcznia,
  utworzRozwiazanie,
  wystawSesje,
  zapiszZmianyKonta,
  zatwierdzRozwiazanieMocka,
  znajdzKontoPoEmailu,
  znajdzNieukonczone,
  znajdzRozwiazanie,
  znajdzZadaniePoWersji,
  type Konto,
} from "./db";
import { MINIATURA_JPEG_BASE64 } from "./fixtures/konta";
import { DZIALY } from "./fixtures/dzialy";
import { ZADANIA } from "./fixtures/zadania";
import {
  bezTresci,
  blad,
  json,
  odczytajStronicowanie,
  problem,
  strona,
  walidacja,
  type BladPolaMocka,
} from "./http";

const BAZA = "*/api/v1";
const MAKS_ZDJECIE_B = 10 * 1024 * 1024;
const MAKS_ZADANIE_B = 52 * 1024 * 1024;
const MAKS_ZDJEC = 5;
const SCENARIUSZE: ScenariuszMock[] = ["domyslny", "rozpoznanie-nieczytelne"];
const WZORZEC_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

type Wynik<T> = { ok: T } | { blad: Response };

// --- Wspólne ---
function wymagajUcznia(request: Request): Wynik<Konto> {
  const naglowek = request.headers.get("Authorization") ?? "";
  const token = naglowek.startsWith("Bearer ") ? naglowek.slice(7).trim() : null;
  const konto = kontoZTokenu(token);
  if (!konto) {
    return { blad: problem(request, 401, "NIEUWIERZYTELNIONY", "Zaloguj się, aby kontynuować.") };
  }
  return { ok: konto };
}

async function odczytajJson(request: Request): Promise<Wynik<Record<string, unknown>>> {
  const typ = request.headers.get("Content-Type") ?? "";
  if (!typ.includes("json")) {
    return {
      blad: problem(request, 415, "NIEOBSLUGIWANY_FORMAT", "Nieobsługiwany typ treści żądania."),
    };
  }
  try {
    const wartosc: unknown = await request.json();
    if (wartosc === null || typeof wartosc !== "object" || Array.isArray(wartosc)) {
      throw new Error("nie obiekt");
    }
    return { ok: wartosc as Record<string, unknown> };
  } catch {
    return { blad: problem(request, 400, "ZAPYTANIE_NIEPRAWIDLOWE", "Niepoprawne żądanie.") };
  }
}

function odczytajIfMatch(request: Request): Wynik<number> {
  const surowy = request.headers.get("If-Match");
  if (surowy === null) {
    return {
      blad: problem(request, 428, "WYMAGANY_IF_MATCH", "Ta operacja wymaga nagłówka If-Match."),
    };
  }
  const dopasowanie = /^"(\d+)"$/.exec(surowy.trim());
  if (!dopasowanie) {
    return {
      blad: problem(
        request,
        400,
        "NAGLOWEK_NIEPRAWIDLOWY",
        'Nagłówek If-Match musi mieć postać "<wersja>" (liczba w cudzysłowach).',
      ),
    };
  }
  return { ok: Number(dopasowanie[1]) };
}

function nieZnaleziono(request: Request, detail = "Nie znaleziono zasobu.") {
  return problem(request, 404, "NIE_ZNALEZIONO", detail);
}

function nieDozwolona(request: Request) {
  return problem(
    request,
    405,
    "METODA_NIEDOZWOLONA",
    "Ta metoda nie jest dozwolona dla tego adresu.",
  );
}

// --- Walidacje ---
const POLA_REJESTRACJI = [
  "email",
  "haslo",
  "imie",
  "nazwisko",
  "rola",
  "oswiadczenie_wieku_16",
  "zgoda_regulamin",
  "kod_zaproszenia",
];

function tekst(wartosc: unknown): string | null {
  return typeof wartosc === "string" ? wartosc : null;
}

function walidujDlugosc(
  bledy: BladPolaMocka[],
  pole: string,
  wartosc: unknown,
  min: number,
  max: number,
  nazwa: string,
): void {
  const t = tekst(wartosc);
  if (t === null || t.trim().length === 0) {
    bledy.push(blad(pole, "WYMAGANE", `Pole „${nazwa}” jest wymagane.`));
  } else if (t.length < min) {
    bledy.push(blad(pole, "ZA_KROTKIE", `Pole „${nazwa}” musi mieć co najmniej ${min} znaków.`));
  } else if (t.length > max) {
    bledy.push(blad(pole, "ZA_DLUGIE", `Pole „${nazwa}” może mieć najwyżej ${max} znaków.`));
  }
}

function walidujRejestracje(cialo: Record<string, unknown>): BladPolaMocka[] {
  const bledy: BladPolaMocka[] = [];
  for (const pole of Object.keys(cialo)) {
    if (!POLA_REJESTRACJI.includes(pole)) {
      bledy.push(blad(pole, "POLE_NIEZNANE", `Nieznane pole „${pole}”.`));
    }
  }
  const email = tekst(cialo.email);
  if (email === null || email.trim() === "") {
    bledy.push(blad("email", "WYMAGANE", "Podaj adres e-mail."));
  } else if (email.length > 254) {
    bledy.push(blad("email", "ZA_DLUGIE", "Adres e-mail może mieć najwyżej 254 znaki."));
  } else if (!WZORZEC_EMAIL.test(email.trim())) {
    bledy.push(blad("email", "FORMAT_EMAIL", "Podaj poprawny adres e-mail."));
  }
  const haslo = tekst(cialo.haslo);
  if (haslo === null || haslo === "") bledy.push(blad("haslo", "WYMAGANE", "Podaj hasło."));
  else if (haslo.length < 10) {
    bledy.push(blad("haslo", "ZA_KROTKIE", "Hasło musi mieć co najmniej 10 znaków."));
  } else if (haslo.length > 128) {
    bledy.push(blad("haslo", "ZA_DLUGIE", "Hasło może mieć najwyżej 128 znaków."));
  }
  walidujDlugosc(bledy, "imie", cialo.imie, 1, 60, "Imię");
  walidujDlugosc(bledy, "nazwisko", cialo.nazwisko, 1, 80, "Nazwisko");
  if (cialo.rola === "NAUCZYCIEL") {
    bledy.push(
      blad("rola", "ROLA_NIEOBSLUGIWANA", "Rejestracja Nauczycieli nie jest jeszcze dostępna."),
    );
  } else if (cialo.rola === undefined || cialo.rola === null) {
    bledy.push(blad("rola", "WYMAGANE", "Wybierz rolę."));
  } else if (cialo.rola !== "UCZEN") {
    bledy.push(blad("rola", "TYP_NIEPRAWIDLOWY", "Nieprawidłowa rola."));
  }
  if (cialo.oswiadczenie_wieku_16 !== true) {
    bledy.push(
      blad(
        "oswiadczenie_wieku_16",
        "WYMAGANE_OSWIADCZENIE",
        "Wymagane oświadczenie o ukończeniu 16 lat.",
      ),
    );
  }
  if (cialo.zgoda_regulamin !== true) {
    bledy.push(blad("zgoda_regulamin", "WYMAGANE_OSWIADCZENIE", "Wymagana zgoda na regulamin."));
  }
  return bledy;
}

const POLA_ZMIANY_UCZNIA = [
  "poziom_matury",
  "termin_egzaminu",
  "wybrany_dzial_id",
  "zgoda_na_trening",
];

function czyPoprawnaData(t: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(t)) return false;
  const d = new Date(`${t}T00:00:00Z`);
  return !Number.isNaN(d.getTime()) && d.toISOString().startsWith(t);
}

function walidujZmianeUcznia(cialo: Record<string, unknown>): BladPolaMocka[] {
  const bledy: BladPolaMocka[] = [];
  for (const pole of Object.keys(cialo)) {
    if (!POLA_ZMIANY_UCZNIA.includes(pole)) {
      bledy.push(blad(pole, "POLE_NIEZNANE", `Nieznane pole „${pole}”.`));
    }
  }
  if ("poziom_matury" in cialo) {
    if (cialo.poziom_matury === null) {
      bledy.push(
        blad("poziom_matury", "NIE_MOZNA_WYCZYSCIC", "Poziomu matury nie można wyczyścić."),
      );
    } else if (cialo.poziom_matury !== "PODSTAWOWY" && cialo.poziom_matury !== "ROZSZERZONY") {
      bledy.push(
        blad("poziom_matury", "TYP_NIEPRAWIDLOWY", "Wybierz poziom podstawowy lub rozszerzony."),
      );
    }
  }
  if ("zgoda_na_trening" in cialo) {
    if (cialo.zgoda_na_trening === null) {
      bledy.push(blad("zgoda_na_trening", "NIE_MOZNA_WYCZYSCIC", "Zgody nie można wyczyścić."));
    } else if (typeof cialo.zgoda_na_trening !== "boolean") {
      bledy.push(
        blad("zgoda_na_trening", "TYP_NIEPRAWIDLOWY", "Zgoda musi być wartością logiczną."),
      );
    }
  }
  if ("termin_egzaminu" in cialo && cialo.termin_egzaminu !== null) {
    const t = tekst(cialo.termin_egzaminu);
    if (t === null || !czyPoprawnaData(t)) {
      bledy.push(blad("termin_egzaminu", "TYP_NIEPRAWIDLOWY", "Podaj poprawną datę (RRRR-MM-DD)."));
    }
  }
  if ("wybrany_dzial_id" in cialo && cialo.wybrany_dzial_id !== null) {
    const id = cialo.wybrany_dzial_id;
    if (typeof id !== "number" || !Number.isInteger(id)) {
      bledy.push(
        blad(
          "wybrany_dzial_id",
          "TYP_NIEPRAWIDLOWY",
          "Identyfikator Działu musi być liczbą całkowitą.",
        ),
      );
    } else if (!DZIALY.some((d) => d.id === id)) {
      bledy.push(blad("wybrany_dzial_id", "DZIAL_NIEZNANY", "Nie ma takiego Działu."));
    }
  }
  return bledy;
}

type StanJpeg = "OK" | "INNY_FORMAT" | "NIEODCZYTYWALNY";

/** Sygnatura FF D8 FF oraz czytelny nagłówek SOF z niezerowymi wymiarami (7.4). */
function sprawdzJpeg(b: Uint8Array): StanJpeg {
  if (b.length < 3 || b[0] !== 0xff || b[1] !== 0xd8 || b[2] !== 0xff) return "INNY_FORMAT";
  let i = 2;
  while (i + 3 < b.length) {
    if (b[i] !== 0xff) return "NIEODCZYTYWALNY";
    const marker = b[i + 1] ?? 0;
    if (marker === 0xff) {
      i += 1;
      continue;
    }
    if (marker === 0xd8 || (marker >= 0xd0 && marker <= 0xd7) || marker === 0x01) {
      i += 2;
      continue;
    }
    const dlugosc = ((b[i + 2] ?? 0) << 8) | (b[i + 3] ?? 0);
    const czySof = marker >= 0xc0 && marker <= 0xcf && ![0xc4, 0xc8, 0xcc].includes(marker);
    if (czySof) {
      if (i + 8 >= b.length) return "NIEODCZYTYWALNY";
      const wysokosc = ((b[i + 5] ?? 0) << 8) | (b[i + 6] ?? 0);
      const szerokosc = ((b[i + 7] ?? 0) << 8) | (b[i + 8] ?? 0);
      return wysokosc > 0 && szerokosc > 0 ? "OK" : "NIEODCZYTYWALNY";
    }
    if (dlugosc < 2) return "NIEODCZYTYWALNY";
    i += 2 + dlugosc;
  }
  return "NIEODCZYTYWALNY";
}

// --- Handlery ---
export const handlers = [
  // 1. Rejestracja
  http.post(`${BAZA}/auth/rejestracja`, async ({ request }) => {
    const cialo = await odczytajJson(request);
    if ("blad" in cialo) return cialo.blad;
    const bledy = walidujRejestracje(cialo.ok);
    if (bledy.length > 0) return walidacja(request, bledy);
    const email = String(cialo.ok.email).trim();
    if (znajdzKontoPoEmailu(email)) {
      return problem(
        request,
        409,
        "EMAIL_ZAJETY",
        "Nie udało się założyć konta na ten adres — zaloguj się albo zresetuj hasło.",
      );
    }
    const konto = dodajKonto({
      email,
      haslo: String(cialo.ok.haslo),
      imie: String(cialo.ok.imie).trim(),
      nazwisko: String(cialo.ok.nazwisko).trim(),
    });
    return json({ status_konta: "AKTYWNE", sesja: wystawSesje(konto) }, 201);
  }),

  // 2. Logowanie
  http.post(`${BAZA}/auth/logowanie`, async ({ request }) => {
    const cialo = await odczytajJson(request);
    if ("blad" in cialo) return cialo.blad;
    const bledy: BladPolaMocka[] = [];
    if (!tekst(cialo.ok.email)?.trim())
      bledy.push(blad("email", "WYMAGANE", "Podaj adres e-mail."));
    if (!tekst(cialo.ok.haslo)) bledy.push(blad("haslo", "WYMAGANE", "Podaj hasło."));
    if (bledy.length > 0) return walidacja(request, bledy);
    const konto = znajdzKontoPoEmailu(String(cialo.ok.email));
    if (!konto || konto.haslo !== cialo.ok.haslo) {
      return problem(request, 401, "NIEUWIERZYTELNIONY", "Nieprawidłowy e-mail lub hasło.");
    }
    return json(wystawSesje(konto));
  }),

  // 3. Odświeżenie (cookie lemat_rt zastąpione wpisem w localStorage, tylko w trybie MSW)
  http.post(`${BAZA}/auth/odswiezenie`, ({ request }) => {
    const sesja = odswiezSesjeMocka();
    if (!sesja) {
      return problem(request, 401, "SESJA_UNIEWAZNIONA", "Sesja wygasła — zaloguj się ponownie.");
    }
    return json(sesja);
  }),

  // 4. Wylogowanie
  http.post(`${BAZA}/auth/wylogowanie`, ({ request }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    uniewaznijRefresh();
    uniewaznijTokenyUcznia(u.ok.id);
    return bezTresci(204);
  }),

  // 5. GET /uczniowie/ja
  http.get(`${BAZA}/uczniowie/ja`, ({ request }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    return json(uczenWyjscie(u.ok));
  }),

  // 6. PATCH /uczniowie/ja (merge-patch + If-Match)
  http.patch(`${BAZA}/uczniowie/ja`, async ({ request }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    if (!(request.headers.get("Content-Type") ?? "").includes("application/merge-patch+json")) {
      return problem(
        request,
        415,
        "NIEOBSLUGIWANY_FORMAT",
        "Użyj typu application/merge-patch+json.",
      );
    }
    const wersja = odczytajIfMatch(request);
    if ("blad" in wersja) return wersja.blad;
    const cialo = await odczytajJson(request);
    if ("blad" in cialo) return cialo.blad;
    const konto = u.ok;
    if (wersja.ok !== konto.version) {
      return problem(
        request,
        412,
        "KONFLIKT_WERSJI",
        "Dane zmieniły się w międzyczasie — odśwież je.",
        {
          szczegoly: { aktualny: uczenWyjscie(konto) },
        },
      );
    }
    const bledy = walidujZmianeUcznia(cialo.ok);
    if (bledy.length > 0) return walidacja(request, bledy);
    const zmiana = cialo.ok;
    if ("poziom_matury" in zmiana)
      konto.poziom_matury = zmiana.poziom_matury as Konto["poziom_matury"];
    if ("termin_egzaminu" in zmiana)
      konto.termin_egzaminu = zmiana.termin_egzaminu as string | null;
    if ("wybrany_dzial_id" in zmiana)
      konto.wybrany_dzial_id = zmiana.wybrany_dzial_id as number | null;
    if ("zgoda_na_trening" in zmiana) konto.zgoda_na_trening = zmiana.zgoda_na_trening as boolean;
    konto.version += 1;
    zapiszZmianyKonta();
    return json(uczenWyjscie(konto));
  }),

  // 7. Profil uproszczony
  http.get(`${BAZA}/uczniowie/ja/profil-uproszczony`, ({ request }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    return json(profilUproszczony(u.ok.id));
  }),

  // 8. Działy
  http.get(`${BAZA}/dzialy`, ({ request }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    return json(DZIALY);
  }),

  // 9. Lista Zadań
  http.get(`${BAZA}/zadania`, ({ request }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    const stronicowanie = odczytajStronicowanie(request);
    if ("blad" in stronicowanie) return stronicowanie.blad;
    let lista = [...ZADANIA];
    const surowyDzial = new URL(request.url).searchParams.get("dzial_id");
    if (surowyDzial !== null) {
      const dzialId = Number(surowyDzial);
      if (surowyDzial === "" || !Number.isInteger(dzialId)) {
        return walidacja(request, [
          blad("dzial_id", "TYP_NIEPRAWIDLOWY", "Identyfikator Działu musi być liczbą całkowitą."),
        ]);
      }
      lista = lista.filter((z) => z.dzial.id === dzialId);
    }
    lista.sort((a, b) => a.poziom_trudnosci - b.poziom_trudnosci || a.id.localeCompare(b.id));
    return json(strona(lista, stronicowanie.limit, stronicowanie.offset));
  }),

  // 10. Zadanie
  http.get(`${BAZA}/zadania/:id`, ({ request, params }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    const zadanie = ZADANIA.find((z) => z.id === String(params.id));
    return zadanie ? json(zadanie) : nieZnaleziono(request, "Nie znaleziono zadania.");
  }),

  // 11. Przesłanie Rozwiązania
  http.post(`${BAZA}/rozwiazania`, async ({ request }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    if (!(request.headers.get("Content-Type") ?? "").includes("multipart/form-data")) {
      return problem(request, 415, "NIEOBSLUGIWANY_FORMAT", "Użyj typu multipart/form-data.");
    }
    let formularz: FormData;
    try {
      formularz = await request.formData();
    } catch {
      return problem(request, 400, "ZAPYTANIE_NIEPRAWIDLOWE", "Niepoprawne żądanie.");
    }

    const bledy: BladPolaMocka[] = [];
    const naglowekScenariusza = request.headers.get("X-Lemat-Mock-Scenariusz");
    let scenariusz: ScenariuszMock = "domyslny";
    if (naglowekScenariusza !== null) {
      if (SCENARIUSZE.includes(naglowekScenariusza as ScenariuszMock)) {
        scenariusz = naglowekScenariusza as ScenariuszMock;
      } else {
        bledy.push(
          blad("X-Lemat-Mock-Scenariusz", "MOCK_SCENARIUSZ_NIEZNANY", "Nieznany scenariusz mocka."),
        );
      }
    }
    const wersjaId = formularz.get("zadanie_wersja_id");
    if (typeof wersjaId !== "string" || wersjaId === "") {
      bledy.push(blad("zadanie_wersja_id", "WYMAGANE", "Wskaż zadanie, które rozwiązujesz."));
    } else if (!CZY_UUID.test(wersjaId)) {
      bledy.push(
        blad("zadanie_wersja_id", "TYP_NIEPRAWIDLOWY", "Nieprawidłowy identyfikator zadania."),
      );
    }
    const pliki: Blob[] = [];
    for (const [nazwa, wartosc] of formularz.entries()) {
      if (typeof wartosc === "string") continue;
      if (nazwa === "zdjecia") pliki.push(wartosc);
      else {
        bledy.push(
          blad(nazwa, "PLIK_POZA_POLEM_ZDJECIA", "Zdjęcia należy przesłać w polu „zdjecia”."),
        );
      }
    }
    if (bledy.length > 0) return walidacja(request, bledy);

    const lacznie = pliki.reduce((a, p) => a + p.size, 0);
    if (lacznie > MAKS_ZADANIE_B) {
      return problem(request, 413, "ZAPYTANIE_ZA_DUZE", "Przesyłane zdjęcia są zbyt duże.");
    }
    if (pliki.length < 1 || pliki.length > MAKS_ZDJEC) {
      return problem(request, 422, "ZA_DUZO_ZDJEC", "Prześlij od 1 do 5 zdjęć.");
    }
    for (const plik of pliki) {
      if (plik.size > MAKS_ZDJECIE_B) {
        return problem(request, 413, "ZDJECIE_ZA_DUZE", "Zdjęcie może mieć najwyżej 10 MB.");
      }
      const stan = sprawdzJpeg(new Uint8Array(await plik.arrayBuffer()));
      if (stan === "INNY_FORMAT") {
        return problem(request, 415, "NIEOBSLUGIWANY_FORMAT", "Obsługiwane są tylko zdjęcia JPEG.");
      }
      if (stan === "NIEODCZYTYWALNY") {
        return problem(request, 422, "ZDJECIE_NIEPRAWIDLOWE", "Nie można odczytać zdjęcia.");
      }
    }

    const zadanie = znajdzZadaniePoWersji(String(wersjaId));
    if (!zadanie) return nieZnaleziono(request, "Nie znaleziono zadania.");
    const istniejace = znajdzNieukonczone(u.ok.id, zadanie.id);
    if (istniejace) {
      return problem(
        request,
        409,
        "ROZWIAZANIE_NIEPRAWIDLOWY_STAN",
        "Dla tego zadania masz już rozwiązanie w trakcie oceny.",
        {
          szczegoly: {
            istniejace_rozwiazanie_id: istniejace.wyjscie.id,
            status: istniejace.wyjscie.status,
          },
        },
      );
    }
    const rozwiazanie = utworzRozwiazanie(u.ok, zadanie, pliki.length, scenariusz);
    return json(rozwiazanie, 202, { Location: `/api/v1/rozwiazania/${rozwiazanie.id}` });
  }),

  // 12. Lista Rozwiązań
  http.get(`${BAZA}/rozwiazania`, ({ request }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    const stronicowanie = odczytajStronicowanie(request);
    if ("blad" in stronicowanie) return stronicowanie.blad;
    const status = new URL(request.url).searchParams.get("status");
    const dozwolone = [
      "PRZESLANE",
      "ROZPOZNAWANIE",
      "BLAD_ROZPOZNAWANIA",
      "TRANSKRYPCJA_GOTOWA",
      "ZATWIERDZONE",
      "OCENIANIE",
      "OCZEKUJE_NA_WERYFIKACJE",
      "OCENIONE",
      "PORZUCONE",
      "ANULOWANE",
    ];
    if (status !== null && !dozwolone.includes(status)) {
      return walidacja(request, [
        blad("status", "TYP_NIEPRAWIDLOWY", "Nieznany status rozwiązania."),
      ]);
    }
    const lista = rozwiazaniaUcznia(u.ok.id)
      .map((r) => r.wyjscie)
      .filter((r) => status === null || r.status === status)
      .sort((a, b) => b.created_at.localeCompare(a.created_at) || b.id.localeCompare(a.id));
    return json(strona(lista, stronicowanie.limit, stronicowanie.offset));
  }),

  // 13. Rozwiązanie (odpytywanie)
  http.get(`${BAZA}/rozwiazania/:id`, ({ request, params }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    const r = znajdzRozwiazanie(String(params.id), u.ok.id);
    return r ? json(r.wyjscie) : nieZnaleziono(request, "Nie znaleziono rozwiązania.");
  }),

  // 14. Zatwierdzenie Transkrypcji
  http.post(`${BAZA}/rozwiazania/:id/zatwierdzenie`, async ({ request, params }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    const wersja = odczytajIfMatch(request);
    if ("blad" in wersja) return wersja.blad;
    const r = znajdzRozwiazanie(String(params.id), u.ok.id);
    if (!r) return nieZnaleziono(request, "Nie znaleziono rozwiązania.");
    const cialo = await odczytajJson(request);
    if ("blad" in cialo) return cialo.blad;
    const bledy: BladPolaMocka[] = [];
    for (const pole of Object.keys(cialo.ok)) {
      if (pole !== "transkrypcja_id" && pole !== "wersja") {
        bledy.push(blad(pole, "POLE_NIEZNANE", `Nieznane pole „${pole}”.`));
      }
    }
    const tid = cialo.ok.transkrypcja_id;
    if (typeof tid !== "string" || !CZY_UUID.test(tid)) {
      bledy.push(blad("transkrypcja_id", "WYMAGANE", "Podaj identyfikator Transkrypcji."));
    }
    const wersjaTr = cialo.ok.wersja;
    if (typeof wersjaTr !== "number" || !Number.isInteger(wersjaTr)) {
      bledy.push(blad("wersja", "WYMAGANE", "Podaj wersję Transkrypcji."));
    }
    if (bledy.length > 0) return walidacja(request, bledy);
    const wejscie = cialo.ok as unknown as ZatwierdzenieWejscie;

    if (r.wyjscie.status !== "TRANSKRYPCJA_GOTOWA") {
      return problem(
        request,
        409,
        "ROZWIAZANIE_NIEPRAWIDLOWY_STAN",
        "Tego rozwiązania nie można teraz zatwierdzić.",
        { szczegoly: { status: r.wyjscie.status } },
      );
    }
    if (wersja.ok !== r.wyjscie.version) {
      return problem(request, 412, "KONFLIKT_WERSJI", "Rozwiązanie zmieniło się w międzyczasie.", {
        szczegoly: { aktualny: r.wyjscie },
      });
    }
    const transkrypcja = r.wyjscie.transkrypcja;
    if (
      !transkrypcja ||
      transkrypcja.id !== wejscie.transkrypcja_id ||
      transkrypcja.wersja !== wejscie.wersja
    ) {
      return problem(request, 409, "TRANSKRYPCJA_NIEAKTUALNA", "Transkrypcja jest nieaktualna.");
    }
    return json(zatwierdzRozwiazanieMocka(r), 202);
  }),

  // 15. Ocena
  http.get(`${BAZA}/rozwiazania/:id/ocena`, ({ request, params }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    const r = znajdzRozwiazanie(String(params.id), u.ok.id);
    if (!r) return nieZnaleziono(request, "Nie znaleziono rozwiązania.");
    if (r.wyjscie.status !== "OCENIONE" || !r.wyjscie.ocena) {
      return nieZnaleziono(request, "Ocena nie jest jeszcze gotowa.");
    }
    return json(r.wyjscie.ocena);
  }),

  // 16. Adres zdjęcia (w MSW: statyczny plik z public/mock)
  http.get(`${BAZA}/rozwiazania/:id/zdjecia/:zdjecieId/adres`, ({ request, params }) => {
    const u = wymagajUcznia(request);
    if ("blad" in u) return u.blad;
    const r = znajdzRozwiazanie(String(params.id), u.ok.id);
    const zdjecie = r?.wyjscie.zdjecia.find((z) => z.id === String(params.zdjecieId));
    if (!r || !zdjecie) return nieZnaleziono(request, "Nie znaleziono zdjęcia.");
    return json({
      url: "/mock/kartka-przyklad.jpg",
      wygasa_at: new Date(Date.now() + 5 * 60 * 1000).toISOString().replace(/\.\d{3}Z$/, "Z"),
    });
  }),

  // 17. Magazyn lokalny (w MSW nieużywany przez adres z 16; zwraca miniaturę dla poprawnego tokenu)
  http.get(`${BAZA}/magazyn-lokalny/:token`, ({ request, params }) => {
    const token = String(params.token);
    if (!/^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$/.test(token)) return nieZnaleziono(request);
    const bajty = Uint8Array.from(atob(MINIATURA_JPEG_BASE64), (c) => c.charCodeAt(0));
    return new HttpResponse(bajty, {
      status: 200,
      headers: { "Content-Type": "image/jpeg", "Cache-Control": "private, max-age=300" },
    });
  }),

  // 18. Zdrowie
  http.get(`${BAZA}/zdrowie`, () => json({ status: "OK" })),

  // 405 dla znanych ścieżek ze złą metodą, potem 404 dla reszty /api/v1/*
  ...[
    "/auth/rejestracja",
    "/auth/logowanie",
    "/auth/odswiezenie",
    "/auth/wylogowanie",
    "/uczniowie/ja",
    "/uczniowie/ja/profil-uproszczony",
    "/dzialy",
    "/zadania",
    "/zadania/:id",
    "/rozwiazania",
    "/rozwiazania/:id",
    "/rozwiazania/:id/zatwierdzenie",
    "/rozwiazania/:id/ocena",
    "/rozwiazania/:id/zdjecia/:zdjecieId/adres",
    "/magazyn-lokalny/:token",
    "/zdrowie",
  ].map((sciezka) => http.all(`${BAZA}${sciezka}`, ({ request }) => nieDozwolona(request))),
  http.all(`${BAZA}/*`, ({ request }) => nieZnaleziono(request)),
];
