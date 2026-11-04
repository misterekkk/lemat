// @Role: Testy widoku uproszczonego Profilu ucznia na mockach MSW: stan pusty, przykład 4.3, format polski, linki do Rozwiązań | @DependsOn: features/profile, features/accounts, lib/auth/test-support, mocks/server | @UsedBy: vitest | @Task: TASK-134
import { QueryClient } from "@tanstack/react-query";
import { screen, within } from "@testing-library/react";
import { HttpResponse, http } from "msw";
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { StudentShell } from "@/features/accounts";
import { skonfigurujKlienta, zaloguj } from "@/lib/api/client";
import { przekierujFetchNaLocalhost, renderujZSesja, ustawSciezke } from "@/lib/auth/test-support";
import { resetujStan } from "@/mocks/db";
import profil43 from "@/mocks/__expected__/profil-4-3.json";
import { server } from "@/mocks/server";
import { ProfileView } from "./index";

vi.mock("next/navigation", async () => (await import("@/lib/auth/test-support")).nawigacjaMock);

beforeAll(() => {
  server.listen({ onUnhandledRequest: "error" });
  przekierujFetchNaLocalhost();
});
beforeEach(() => {
  resetujStan();
  ustawSciezke("/profile");
});
afterEach(() => server.resetHandlers());
afterAll(() => {
  server.close();
  vi.unstubAllGlobals();
});

async function zalogujDemo(): Promise<void> {
  skonfigurujKlienta({ pobierzToken: () => null, odswiezSesje: () => Promise.resolve(null) });
  await zaloguj({ email: "uczen1@lemat.local", haslo: "Lemat-Demo-2026!" });
}

function renderuj() {
  return renderujZSesja(
    <StudentShell>
      <ProfileView />
    </StudentShell>,
    new QueryClient({ defaultOptions: { queries: { retry: false } } }),
  );
}

describe("ProfileView", () => {
  it("Uczeń bez Rozwiązań widzi stan pusty z linkiem do Zadań", async () => {
    await zalogujDemo();
    renderuj();
    expect(await screen.findByText("Nie masz jeszcze rozwiązań")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Przejdź do zadań" })).toHaveAttribute(
      "href",
      "/exercises",
    );
    expect(screen.queryByText("Średni wynik")).not.toBeInTheDocument();
  });

  it("dla przykładu 4.3 pokazuje 50,0%, trzy Działy i ostatnie Oceny z linkami", async () => {
    await zalogujDemo();
    server.use(
      http.get("*/api/v1/uczniowie/ja/profil-uproszczony", () => HttpResponse.json(profil43)),
    );
    renderuj();
    expect(await screen.findByText(/^50,0\s?%$/)).toBeInTheDocument();
    expect(screen.getByText("Liczba rozwiązań").nextElementSibling).toHaveTextContent("4");
    expect(screen.getByText("Ocenione rozwiązania").nextElementSibling).toHaveTextContent("3");
    expect(screen.getByText("3 z 6 pkt")).toBeInTheDocument();

    const dzialy = within(screen.getByRole("region", { name: "Wyniki według Działów" }));
    expect(dzialy.getAllByRole("listitem")).toHaveLength(3);
    expect(dzialy.getByText("Równania i nierówności")).toBeInTheDocument();
    expect(dzialy.getByText("Funkcje")).toBeInTheDocument();
    expect(dzialy.getByText("Planimetria")).toBeInTheDocument();
    expect(dzialy.getByText(/^100,0\s?% · Ocenione: 1$/)).toBeInTheDocument();
    expect(dzialy.getByText(/^0,0\s?% · Ocenione: 1$/)).toBeInTheDocument();

    const ostatnie = within(screen.getByRole("region", { name: "Ostatnie oceny" }));
    const linki = ostatnie.getAllByRole("link", { name: "Zobacz rozwiązanie" });
    expect(linki.map((l) => l.getAttribute("href"))).toEqual([
      "/solutions/d1000000-0000-4000-8000-000000000012",
      "/solutions/d1000000-0000-4000-8000-000000000011",
      "/solutions/d1000000-0000-4000-8000-000000000010",
    ]);
    expect(ostatnie.getByText("Planimetria · 0 z 2 pkt")).toBeInTheDocument();
  });

  it("błąd wczytania pokazuje komunikat z ponowieniem", async () => {
    await zalogujDemo();
    server.use(
      http.get(
        "*/api/v1/uczniowie/ja/profil-uproszczony",
        () =>
          HttpResponse.json(
            {
              type: "https://lemat.pl/bledy/blad-wewnetrzny",
              title: "Błąd",
              status: 500,
              detail: "Błąd serwera.",
              instance: "/api/v1/uczniowie/ja/profil-uproszczony",
              kod: "BLAD_WEWNETRZNY",
              trace_id: "x",
              szczegoly: null,
            },
            { status: 500, headers: { "Content-Type": "application/problem+json" } },
          ),
        { once: true },
      ),
    );
    renderuj();
    expect(await screen.findByRole("button", { name: "Spróbuj ponownie" })).toBeInTheDocument();
  });
});
