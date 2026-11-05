// @Role: Testy ekranu i listy Rozwiązań na mockach MSW z fałszywymi timerami: sekwencja statusów, Transkrypcja z KaTeX, zatwierdzenie, Ocena i Wskazówka, błąd rozpoznawania, 409/412, limit 60 s, podgląd zdjęcia | @DependsOn: features/solutions, features/accounts, lib/auth/test-support, mocks/db, mocks/server | @UsedBy: vitest | @Task: TASK-137
import { QueryClient } from "@tanstack/react-query";
import { act, fireEvent, screen } from "@testing-library/react";
import { HttpResponse, http } from "msw";
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { StudentShell } from "@/features/accounts";
import { skonfigurujKlienta, zaloguj } from "@/lib/api/client";
import { przekierujFetchNaLocalhost, renderujZSesja, ustawSciezke } from "@/lib/auth/test-support";
import {
  resetujStan,
  utworzRozwiazanie,
  zatwierdzRozwiazanieMocka,
  znajdzRozwiazanie,
  znajdzKontoPoEmailu,
  znajdzZadaniePoWersji,
} from "@/mocks/db";
import { server } from "@/mocks/server";
import { SolutionList, SolutionView } from "./index";
import type { ScenariuszMock } from "@/lib/api/types";

vi.mock("next/navigation", async () => (await import("@/lib/auth/test-support")).nawigacjaMock);

const wersjaZadania = (n: number) => `b1000000-0000-4000-8000-00000000000${n}`;
const ID_PIERWSZEGO = "d1000000-0000-4000-8000-000000000010";

beforeAll(() => {
  server.listen({ onUnhandledRequest: "error" });
  przekierujFetchNaLocalhost();
});
beforeEach(() => {
  resetujStan();
  ustawSciezke("/solutions");
  vi.useFakeTimers({
    toFake: ["setTimeout", "clearTimeout", "setInterval", "clearInterval", "Date"],
  });
});
afterEach(() => {
  server.resetHandlers();
  vi.useRealTimers();
});
afterAll(() => {
  server.close();
  vi.unstubAllGlobals();
});

async function zalogujDemo(): Promise<void> {
  skonfigurujKlienta({ pobierzToken: () => null, odswiezSesje: () => Promise.resolve(null) });
  await zaloguj({ email: "uczen1@lemat.local", haslo: "Lemat-Demo-2026!" });
}

/** Tworzy Rozwiązanie w mocku (jak po POST /rozwiazania) i zwraca jego id. */
function przeslij(numerZadania: number, scenariusz: ScenariuszMock = "domyslny"): string {
  const konto = znajdzKontoPoEmailu("uczen1@lemat.local")!;
  const zadanie = znajdzZadaniePoWersji(wersjaZadania(numerZadania))!;
  return utworzRozwiazanie(konto, zadanie, 1, scenariusz).id;
}

async function renderuj(ui: React.ReactElement) {
  await zalogujDemo();
  return renderujZSesja(
    <StudentShell>{ui}</StudentShell>,
    new QueryClient({ defaultOptions: { queries: { retry: false } } }),
  );
}

async function uplyw(ms: number): Promise<void> {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ms);
  });
}

/** Przesuwa czas małymi krokami, aż asercja przejdzie (odpowiednik waitFor dla fałszywych timerów). */
async function doczekaj(asercja: () => void, maksMs = 15_000): Promise<void> {
  let ostatni: unknown;
  for (let t = 0; t <= maksMs; t += 100) {
    try {
      asercja();
      return;
    } catch (e) {
      ostatni = e;
    }
    await uplyw(100);
  }
  throw ostatni;
}

const tekst = (re: string | RegExp) => screen.queryByText(re);

describe("SolutionView — przebieg Zadania 2", () => {
  it("PRZESLANE → ROZPOZNAWANIE → Transkrypcja → zatwierdzenie → OCENIANIE → OCENIONE 1/2", async () => {
    const id = przeslij(2);
    await renderuj(<SolutionView rozwiazanieId={id} />);

    await doczekaj(() => expect(tekst(/Przesłano — czekamy na odczyt|Odczytujemy/)).not.toBeNull());
    await doczekaj(() => expect(tekst("Odczytujemy Twoje rozwiązanie…")).not.toBeNull());

    // Transkrypcja Zadania 2 z KaTeX; odpytywanie zatrzymane (brak kolejnych GET).
    await doczekaj(() => expect(screen.getByRole("button", { name: "Zatwierdź" })).toBeEnabled());
    expect(screen.getByText("Krok 1. Obliczam wyróżnik")).toBeInTheDocument();
    expect(document.querySelectorAll(".katex").length).toBeGreaterThanOrEqual(2);
    expect(screen.getAllByText(/mniejszą pewnością/)).toHaveLength(1); // krok 2 (0.91)

    let zapytania = 0;
    server.events.on("request:start", ({ request }) => {
      if (request.method === "GET" && request.url.endsWith(`/rozwiazania/${id}`)) zapytania += 1;
    });
    await uplyw(5000);
    expect(zapytania).toBe(0);

    fireEvent.click(screen.getByRole("button", { name: "Zatwierdź" }));
    await doczekaj(() => expect(tekst("Oceniamy…")).not.toBeNull());
    expect(screen.queryByRole("button", { name: "Zatwierdź" })).toBeNull();

    await doczekaj(() =>
      expect(screen.getByRole("heading", { name: "Ocena" })).toBeInTheDocument(),
    );
    expect(screen.getByText("1 z 2 pkt")).toBeInTheDocument();
    expect(screen.getByRole("table")).toBeInTheDocument();
    expect(screen.getAllByRole("row")).toHaveLength(3); // nagłówek + 2 Kryteria
    expect(screen.getByText("Typ błędu: błąd rachunkowy (krok 2).")).toBeInTheDocument();
    // Wyróżniony pierwszy błędny krok.
    const bladny = screen.getByText("Pierwszy błędny krok").closest("li");
    expect(bladny).toHaveAttribute("aria-current", "step");
    expect(bladny).toHaveTextContent("Krok 2. Obliczam pierwiastki");
    // Wskazówka w ramce.
    const wskazowka = screen.getByRole("complementary");
    expect(wskazowka).toHaveTextContent("Sprawdź jeszcze raz krok 2");
    // Uzasadnienie renderowane razem z KaTeX.
    expect(screen.getByText(/Pierwszy błędny krok: 2/)).toBeInTheDocument();
  });

  it("Zadanie 1 (2/2) nie pokazuje bloku Wskazówki", async () => {
    const id = przeslij(1);
    await renderuj(<SolutionView rozwiazanieId={id} />);
    await doczekaj(() => expect(screen.getByRole("button", { name: "Zatwierdź" })).toBeEnabled());
    fireEvent.click(screen.getByRole("button", { name: "Zatwierdź" }));
    await doczekaj(() =>
      expect(screen.getByRole("heading", { name: "Ocena" })).toBeInTheDocument(),
    );
    expect(screen.getByText("2 z 2 pkt")).toBeInTheDocument();
    expect(screen.queryByRole("complementary")).toBeNull();
    expect(screen.queryByText("Wskazówka")).toBeNull();
    expect(screen.queryByText("Pierwszy błędny krok")).toBeNull();
  });

  it("ładuje zdjęcie z podpisanego adresu z GET …/adres (bez Authorization na obrazie)", async () => {
    const id = przeslij(2);
    const naglowki: (string | null)[] = [];
    server.events.on("request:start", ({ request }) => {
      if (request.url.endsWith("/adres")) naglowki.push(request.headers.get("Authorization"));
    });
    await renderuj(<SolutionView rozwiazanieId={id} />);
    await doczekaj(() =>
      expect(screen.getByAltText("Przesłane zdjęcie kartki 1")).toHaveAttribute(
        "src",
        "/mock/kartka-przyklad.jpg",
      ),
    );
    expect(naglowki).toHaveLength(1);
    expect(naglowki[0]).toMatch(/^Bearer /); // adres pobiera klient API; sam <img> nie niesie nagłówka
  });
});

describe("SolutionView — błędy i limity", () => {
  it("BLAD_ROZPOZNAWANIA pokazuje komunikat i akcję „Prześlij ponownie” do Zadania", async () => {
    const id = przeslij(2, "rozpoznanie-nieczytelne");
    await renderuj(<SolutionView rozwiazanieId={id} />);
    await doczekaj(() =>
      expect(screen.getByText("Nie udało się odczytać zdjęcia")).toBeInTheDocument(),
    );
    const link = screen.getByRole("link", { name: "Prześlij ponownie" });
    expect(link).toHaveAttribute("href", "/exercises/a1000000-0000-4000-8000-000000000002");
    expect(screen.queryByRole("button", { name: "Zatwierdź" })).toBeNull();
  });

  it("ponowne zatwierdzenie (409) pokazuje komunikat i odświeża stan", async () => {
    const id = przeslij(2);
    await renderuj(<SolutionView rozwiazanieId={id} />);
    await doczekaj(() => expect(screen.getByRole("button", { name: "Zatwierdź" })).toBeEnabled());

    let pobrania = 0;
    server.use(
      http.post(`http://localhost/api/v1/rozwiazania/${id}/zatwierdzenie`, () =>
        HttpResponse.json(
          {
            type: "https://lemat.pl/bledy/rozwiazanie-nieprawidlowy-stan",
            title: "Nieprawidłowy stan rozwiązania",
            status: 409,
            detail: "x",
            instance: "/",
            kod: "ROZWIAZANIE_NIEPRAWIDLOWY_STAN",
            trace_id: "t",
            szczegoly: { status: "OCENIANIE" },
          },
          { status: 409, headers: { "Content-Type": "application/problem+json" } },
        ),
      ),
    );
    server.events.on("request:start", ({ request }) => {
      if (request.method === "GET" && request.url.endsWith(`/rozwiazania/${id}`)) pobrania += 1;
    });
    fireEvent.click(screen.getByRole("button", { name: "Zatwierdź" }));
    await doczekaj(() => expect(screen.getByRole("alert")).toHaveTextContent(/w trakcie oceny/));
    await doczekaj(() => expect(pobrania).toBeGreaterThanOrEqual(1));
  });

  it("412 KONFLIKT_WERSJI pokazuje komunikat i pobiera aktualny stan", async () => {
    const id = przeslij(2);
    await renderuj(<SolutionView rozwiazanieId={id} />);
    await doczekaj(() => expect(screen.getByRole("button", { name: "Zatwierdź" })).toBeEnabled());
    server.use(
      http.post(`http://localhost/api/v1/rozwiazania/${id}/zatwierdzenie`, () =>
        HttpResponse.json(
          {
            type: "https://lemat.pl/bledy/konflikt-wersji",
            title: "Dane zmieniły się w międzyczasie",
            status: 412,
            detail: "x",
            instance: "/",
            kod: "KONFLIKT_WERSJI",
            trace_id: "t",
            szczegoly: null,
          },
          { status: 412, headers: { "Content-Type": "application/problem+json" } },
        ),
      ),
    );
    fireEvent.click(screen.getByRole("button", { name: "Zatwierdź" }));
    await doczekaj(() =>
      expect(screen.getByRole("alert")).toHaveTextContent(/Dane zmieniły się w międzyczasie/),
    );
  });

  it("po 60 s odpytywanie się zatrzymuje, a „Odśwież” je wznawia", async () => {
    const id = przeslij(2);
    let pobrania = 0;
    server.use(
      http.get(`http://localhost/api/v1/rozwiazania/${id}`, ({ request }) => {
        pobrania += 1;
        return HttpResponse.json({
          id,
          zadanie_id: "a1000000-0000-4000-8000-000000000002",
          zadanie_wersja_id: wersjaZadania(2),
          status: "ROZPOZNAWANIE",
          element_sciezki_id: null,
          przypisanie_id: null,
          zdjecia: [],
          liczba_przeslan: 1,
          liczba_ponowien_rozpoznania: 0,
          kod_bledu_rozpoznawania: null,
          transkrypcja: null,
          ocena: null,
          weryfikacja: null,
          version: 2,
          created_at: "2026-11-02T10:00:00Z",
          updated_at: "2026-11-02T10:00:00Z",
          nastepne_odpytanie_ms: 1000,
          szkic_transkrypcji: null,
          _url: request.url,
        });
      }),
    );
    await renderuj(<SolutionView rozwiazanieId={id} />);
    await doczekaj(() => expect(tekst("Odczytujemy Twoje rozwiązanie…")).not.toBeNull());
    await uplyw(62_000);
    expect(screen.getByText("Ocena trwa dłużej niż zwykle — odśwież stronę.")).toBeInTheDocument();
    const poLimicie = pobrania;
    expect(poLimicie).toBeGreaterThan(30);
    await uplyw(10_000);
    expect(pobrania).toBe(poLimicie);

    fireEvent.click(screen.getByRole("button", { name: "Odśwież" }));
    await uplyw(3000);
    expect(pobrania).toBeGreaterThan(poLimicie + 1);
    expect(screen.queryByText("Ocena trwa dłużej niż zwykle — odśwież stronę.")).toBeNull();
  });

  it("nieistniejące Rozwiązanie (404) pokazuje stronę „Nie znaleziono”", async () => {
    await renderuj(<SolutionView rozwiazanieId="d1000000-0000-4000-8000-0000000000ff" />);
    await doczekaj(() =>
      expect(screen.getByText("Nie znaleziono rozwiązania")).toBeInTheDocument(),
    );
  });
});

describe("SolutionList", () => {
  it("stan pusty z linkiem do Zadań", async () => {
    await renderuj(<SolutionList />);
    await doczekaj(() =>
      expect(screen.getByText("Nie przesłałeś jeszcze żadnego rozwiązania.")).toBeInTheDocument(),
    );
    expect(screen.getByRole("link", { name: "Przejdź do zadań" })).toHaveAttribute(
      "href",
      "/exercises",
    );
  });

  it("pokazuje statusy po polsku, punkty i linki", async () => {
    const ocenione = przeslij(2);
    // Szybkie doprowadzenie pierwszego Rozwiązania do OCENIONE (bez przechodzenia przez UI).
    await uplyw(3100);
    zatwierdzRozwiazanieMocka(
      znajdzRozwiazanie(ocenione, znajdzKontoPoEmailu("uczen1@lemat.local")!.id)!,
    );
    await uplyw(2100);
    przeslij(3, "rozpoznanie-nieczytelne");
    await zalogujDemo();

    renderujZSesja(
      <StudentShell>
        <SolutionList />
      </StudentShell>,
      new QueryClient({ defaultOptions: { queries: { retry: false } } }),
    );
    await doczekaj(() => expect(screen.getByText("Ocenione")).toBeInTheDocument());
    expect(screen.getByText("1 z 2 pkt")).toBeInTheDocument();
    const elementy = screen.getAllByRole("listitem");
    expect(elementy).toHaveLength(2);
    expect(elementy[0]).toHaveTextContent(/Przesłane|Odczytywanie|Błąd odczytu/); // najnowsze pierwsze
    const linki = screen.getAllByRole("link", { name: "Zobacz rozwiązanie" });
    expect(linki).toHaveLength(2);
    expect(linki.map((l) => l.getAttribute("href"))).toContain(`/solutions/${ID_PIERWSZEGO}`);
  });
});
