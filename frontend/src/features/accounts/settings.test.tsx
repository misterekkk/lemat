// @Role: Testy formularza ustawień Ucznia na mockach MSW: dane z GET, merge-patch tylko ze zmienionymi polami i If-Match, null przy czyszczeniu, 412 i 422 | @DependsOn: features/accounts, lib/auth/test-support, mocks/server, mocks/db | @UsedBy: vitest | @Task: TASK-134
import { QueryClient } from "@tanstack/react-query";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { HttpResponse, http } from "msw";
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { skonfigurujKlienta, zaloguj, zmienUcznia } from "@/lib/api/client";
import { przekierujFetchNaLocalhost, renderujZSesja, ustawSciezke } from "@/lib/auth/test-support";
import { resetujStan } from "@/mocks/db";
import { server } from "@/mocks/server";
import { SettingsForm, StudentShell } from "./index";

vi.mock("next/navigation", async () => (await import("@/lib/auth/test-support")).nawigacjaMock);

const HASLO_DEMO = "Lemat-Demo-2026!";
let patche: { naglowki: Headers; cialo: Promise<string> }[] = [];

beforeAll(() => {
  server.listen({ onUnhandledRequest: "error" });
  przekierujFetchNaLocalhost();
  server.events.on("request:start", ({ request }) => {
    if (request.method === "PATCH") {
      patche.push({ naglowki: request.headers, cialo: request.clone().text() });
    }
  });
});
beforeEach(() => {
  resetujStan();
  patche = [];
  ustawSciezke("/settings");
});
afterEach(() => server.resetHandlers());
afterAll(() => {
  server.close();
  vi.unstubAllGlobals();
});

/** Loguje (cookie mocka) i opcjonalnie ustawia wartości początkowe (version 1 → 2). */
async function przygotuj(zUstawieniami: boolean): Promise<void> {
  let token: string | null = null;
  skonfigurujKlienta({ pobierzToken: () => token, odswiezSesje: () => Promise.resolve(null) });
  const sesja = await zaloguj({ email: "uczen1@lemat.local", haslo: HASLO_DEMO });
  token = sesja.access_token;
  if (zUstawieniami) {
    await zmienUcznia(
      { poziom_matury: "PODSTAWOWY", termin_egzaminu: "2027-05-05", wybrany_dzial_id: 5 },
      1,
    );
  }
  patche = [];
  skonfigurujKlienta({ pobierzToken: () => null, odswiezSesje: () => Promise.resolve(null) });
}

function renderuj() {
  return renderujZSesja(
    <StudentShell>
      <SettingsForm />
    </StudentShell>,
    new QueryClient({ defaultOptions: { queries: { retry: false } } }),
  );
}

const poziom = () => screen.findByLabelText("Poziom matury") as Promise<HTMLSelectElement>;
const termin = () => screen.getByLabelText("Termin egzaminu") as HTMLInputElement;
const dzial = () => screen.getByLabelText("Dział do nauki") as HTMLSelectElement;
const zapisz = () => screen.getByRole("button", { name: "Zapisz zmiany" });

describe("SettingsForm", () => {
  it("wypełnia się danymi z GET /uczniowie/ja i listą Działów", async () => {
    await przygotuj(true);
    renderuj();
    expect((await poziom()).value).toBe("PODSTAWOWY");
    expect(termin().value).toBe("2027-05-05");
    await waitFor(() => expect(dzial().value).toBe("5"));
    expect(screen.getByRole("option", { name: "Wszystkie Działy" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "V. Funkcje" })).toBeInTheDocument();
    expect(zapisz()).toBeDisabled();
  });

  it("zapis wysyła merge-patch tylko ze zmienionymi polami i If-Match z version", async () => {
    await przygotuj(true);
    const u = userEvent.setup();
    renderuj();
    await poziom();
    await waitFor(() => expect(dzial().value).toBe("5"));
    await u.selectOptions(dzial(), "3");
    await u.click(zapisz());
    expect(await screen.findByText("Zapisano zmiany.")).toBeInTheDocument();
    expect(patche).toHaveLength(1);
    expect(patche[0]!.naglowki.get("If-Match")).toBe('"2"');
    expect(patche[0]!.naglowki.get("Content-Type")).toContain("application/merge-patch+json");
    expect(JSON.parse(await patche[0]!.cialo)).toEqual({ wybrany_dzial_id: 3 });
    expect(dzial().value).toBe("3");
  });

  it("pierwszy zapis nowego Ucznia ustawia poziom, termin i Dział", async () => {
    await przygotuj(false);
    const u = userEvent.setup();
    renderuj();
    expect((await poziom()).value).toBe("");
    await u.selectOptions(await poziom(), "ROZSZERZONY");
    fireEvent.change(termin(), { target: { value: "2027-05-05" } });
    await u.selectOptions(dzial(), "7");
    await u.click(zapisz());
    expect(await screen.findByText("Zapisano zmiany.")).toBeInTheDocument();
    expect(JSON.parse(await patche[0]!.cialo)).toEqual({
      poziom_matury: "ROZSZERZONY",
      termin_egzaminu: "2027-05-05",
      wybrany_dzial_id: 7,
    });
    expect(patche[0]!.naglowki.get("If-Match")).toBe('"1"');
  });

  it("wyczyszczenie terminu i Działu wysyła null", async () => {
    await przygotuj(true);
    const u = userEvent.setup();
    renderuj();
    await poziom();
    await waitFor(() => expect(dzial().value).toBe("5"));
    fireEvent.change(termin(), { target: { value: "" } });
    await u.selectOptions(dzial(), "");
    await u.click(zapisz());
    expect(await screen.findByText("Zapisano zmiany.")).toBeInTheDocument();
    expect(JSON.parse(await patche[0]!.cialo)).toEqual({
      termin_egzaminu: null,
      wybrany_dzial_id: null,
    });
  });

  it("podwójne kliknięcie wysyła jedno żądanie", async () => {
    await przygotuj(true);
    const u = userEvent.setup();
    renderuj();
    await poziom();
    await waitFor(() => expect(dzial().value).toBe("5"));
    await u.selectOptions(dzial(), "2");
    await u.dblClick(zapisz());
    await screen.findByText("Zapisano zmiany.");
    expect(patche).toHaveLength(1);
  });

  it("412 KONFLIKT_WERSJI pokazuje komunikat i odświeża dane z szczegoly.aktualny", async () => {
    await przygotuj(true);
    server.use(
      http.patch(
        "*/api/v1/uczniowie/ja",
        () =>
          HttpResponse.json(
            {
              type: "https://lemat.pl/bledy/konflikt-wersji",
              title: "Konflikt wersji",
              status: 412,
              detail: "Dane zmieniły się.",
              instance: "/api/v1/uczniowie/ja",
              kod: "KONFLIKT_WERSJI",
              trace_id: "x",
              szczegoly: {
                aktualny: {
                  id: "c1000000-0000-4000-8000-000000000001",
                  email: "uczen1@lemat.local",
                  imie: "Ala",
                  nazwisko: "Kowalska",
                  poziom_matury: "ROZSZERZONY",
                  termin_egzaminu: "2027-06-01",
                  wybrany_dzial_id: 8,
                  zgoda_na_trening: false,
                  test_diagnostyczny: "BRAK",
                  klasy: [],
                  version: 7,
                },
              },
            },
            { status: 412, headers: { "Content-Type": "application/problem+json" } },
          ),
        { once: true },
      ),
    );
    const u = userEvent.setup();
    renderuj();
    await poziom();
    await waitFor(() => expect(dzial().value).toBe("5"));
    await u.selectOptions(dzial(), "3");
    await u.click(zapisz());
    expect(await screen.findByText(/Dane zmieniły się w międzyczasie/)).toBeInTheDocument();
    await waitFor(() => expect(dzial().value).toBe("8"));
    expect(termin().value).toBe("2027-06-01");
    expect((await poziom()).value).toBe("ROZSZERZONY");
  });

  it("422 WALIDACJA oznacza pola komunikatem z serwera", async () => {
    await przygotuj(true);
    server.use(
      http.patch("*/api/v1/uczniowie/ja", () =>
        HttpResponse.json(
          {
            type: "https://lemat.pl/bledy/walidacja",
            title: "Błędne dane",
            status: 422,
            detail: "Popraw zaznaczone pola.",
            instance: "/api/v1/uczniowie/ja",
            kod: "WALIDACJA",
            trace_id: "x",
            szczegoly: null,
            bledy_pol: [
              {
                pole: "wybrany_dzial_id",
                kod_reguly: "DZIAL_NIEZNANY",
                komunikat: "Nieznany Dział (serwer).",
              },
            ],
          },
          { status: 422, headers: { "Content-Type": "application/problem+json" } },
        ),
      ),
    );
    const u = userEvent.setup();
    renderuj();
    await poziom();
    await waitFor(() => expect(dzial().value).toBe("5"));
    await u.selectOptions(dzial(), "3");
    await u.click(zapisz());
    expect(await screen.findByText("Nieznany Dział (serwer).")).toBeInTheDocument();
    expect(dzial()).toHaveAttribute("aria-invalid", "true");
  });

  it("błąd wczytania pokazuje komunikat i pozwala ponowić", async () => {
    await przygotuj(true);
    server.use(
      http.get(
        "*/api/v1/dzialy",
        () =>
          HttpResponse.json(
            {
              type: "https://lemat.pl/bledy/blad-wewnetrzny",
              title: "Błąd",
              status: 500,
              detail: "Błąd serwera.",
              instance: "/api/v1/dzialy",
              kod: "BLAD_WEWNETRZNY",
              trace_id: "x",
              szczegoly: null,
            },
            { status: 500, headers: { "Content-Type": "application/problem+json" } },
          ),
        { once: true },
      ),
    );
    const u = userEvent.setup();
    renderuj();
    await u.click(await screen.findByRole("button", { name: "Spróbuj ponownie" }));
    expect(await poziom()).toBeInTheDocument();
  });
});
