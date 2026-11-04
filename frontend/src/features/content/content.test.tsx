// @Role: Testy listy i szczegółów Zadań na mockach MSW: kolejność z kontraktu 6.3, filtr Działu, KaTeX (także niepoprawny LaTeX), 404, błąd z ponowieniem | @DependsOn: features/content, features/accounts, lib/auth/test-support, mocks/server | @UsedBy: vitest | @Task: TASK-135
import { QueryClient } from "@tanstack/react-query";
import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { HttpResponse, http } from "msw";
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { StudentShell } from "@/features/accounts";
import { skonfigurujKlienta, zaloguj } from "@/lib/api/client";
import { przekierujFetchNaLocalhost, renderujZSesja, ustawSciezke } from "@/lib/auth/test-support";
import { resetujStan } from "@/mocks/db";
import { server } from "@/mocks/server";
import { ExerciseDetail, ExerciseList } from "./index";

vi.mock("next/navigation", async () => (await import("@/lib/auth/test-support")).nawigacjaMock);

const ZADANIE_1 = "a1000000-0000-4000-8000-000000000001";
const ZADANIE_3 = "a1000000-0000-4000-8000-000000000003";
const ZADANIE_4 = "a1000000-0000-4000-8000-000000000004";

beforeAll(() => {
  server.listen({ onUnhandledRequest: "error" });
  przekierujFetchNaLocalhost();
});
beforeEach(() => {
  resetujStan();
  ustawSciezke("/exercises");
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

function renderuj(ui: React.ReactElement) {
  return renderujZSesja(
    <StudentShell>{ui}</StudentShell>,
    new QueryClient({ defaultOptions: { queries: { retry: false } } }),
  );
}

describe("ExerciseList", () => {
  it("pokazuje 5 Zadań w kolejności z kontraktu 6.3 (trudność, potem id)", async () => {
    await zalogujDemo();
    renderuj(<ExerciseList />);
    const linki = await screen.findAllByRole("link", { name: "Zobacz zadanie" });
    expect(linki.map((l) => l.getAttribute("href"))).toEqual([
      "/exercises/a1000000-0000-4000-8000-000000000002",
      "/exercises/a1000000-0000-4000-8000-000000000005",
      "/exercises/a1000000-0000-4000-8000-000000000001",
      "/exercises/a1000000-0000-4000-8000-000000000003",
      "/exercises/a1000000-0000-4000-8000-000000000004",
    ]);
    expect(screen.getByText("Dział: Trygonometria")).toBeInTheDocument();
    expect(screen.getAllByText(/Poziom trudności: \d z 5/)).toHaveLength(5);
  });

  it("renderuje wzory KaTeX ($20\\%$, \\frac, \\operatorname{tg}) bez surowego znaku $", async () => {
    await zalogujDemo();
    const { container } = renderuj(<ExerciseList />);
    await screen.findAllByRole("link", { name: "Zobacz zadanie" });
    expect(container.querySelectorAll(".katex").length).toBeGreaterThan(5);
    expect(container.textContent).not.toContain("$");
    expect(container.querySelector(".katex-html")?.textContent).toBeTruthy();
    expect(container.innerHTML).toContain("tg");
    expect(container.innerHTML).toContain("mfrac");
  });

  it("filtr Działu zawęża listę do Zadania 1 i pozwala wrócić do wszystkich", async () => {
    await zalogujDemo();
    renderuj(<ExerciseList />);
    await screen.findAllByRole("link", { name: "Zobacz zadanie" });
    const wybor = screen.getByLabelText("Dział");
    await waitFor(() => expect(within(wybor).getAllByRole("option")).toHaveLength(14));
    await userEvent.selectOptions(wybor, "Funkcje");
    await waitFor(() =>
      expect(screen.getAllByRole("link", { name: "Zobacz zadanie" })).toHaveLength(1),
    );
    expect(screen.getByRole("link", { name: "Zobacz zadanie" })).toHaveAttribute(
      "href",
      `/exercises/${ZADANIE_1}`,
    );
    await userEvent.selectOptions(wybor, "Wszystkie działy");
    await waitFor(() =>
      expect(screen.getAllByRole("link", { name: "Zobacz zadanie" })).toHaveLength(5),
    );
  });

  it("dział bez Zadań pokazuje stan pusty", async () => {
    await zalogujDemo();
    renderuj(<ExerciseList />);
    await screen.findAllByRole("link", { name: "Zobacz zadanie" });
    const wybor = screen.getByLabelText("Dział");
    await waitFor(() => expect(within(wybor).getAllByRole("option")).toHaveLength(14));
    await userEvent.selectOptions(wybor, "Stereometria");
    expect(await screen.findByText("Brak zadań w wybranym dziale.")).toBeInTheDocument();
  });

  it("niepoprawny LaTeX nie wywala strony (throwOnError: false)", async () => {
    await zalogujDemo();
    server.use(
      http.get("*/api/v1/zadania", () =>
        HttpResponse.json({
          elementy: [
            {
              id: ZADANIE_1,
              zadanie_wersja_id: "b1000000-0000-4000-8000-000000000001",
              rodzaj: "OTWARTE_OBLICZENIOWE",
              forma_zamknieta: null,
              tresc_md: "Zepsuty wzór $\\frac{1}{$ oraz $\\nieznane{x}$.",
              zalaczniki: [],
              opcje: null,
              stwierdzenia: null,
              punkty_max: 2,
              poziom_trudnosci: 2,
              dzial: { id: 5, nazwa: "Funkcje" },
              umiejetnosci: [],
            },
          ],
          nastepny_kursor: null,
        }),
      ),
    );
    renderuj(<ExerciseList />);
    expect(await screen.findByText(/Zepsuty wzór/)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Zobacz zadanie" })).toBeInTheDocument();
  });

  it("błąd serwera pokazuje komunikat i pozwala ponowić", async () => {
    await zalogujDemo();
    let wywolania = 0;
    server.use(
      http.get("*/api/v1/zadania", () => {
        wywolania += 1;
        if (wywolania === 1) {
          return HttpResponse.json(
            {
              type: "https://lemat.pl/bledy/blad-wewnetrzny",
              title: "Błąd wewnętrzny",
              status: 500,
              detail: "Błąd.",
              instance: "/api/v1/zadania",
              kod: "BLAD_WEWNETRZNY",
              trace_id: "x",
              szczegoly: null,
            },
            { status: 500, headers: { "Content-Type": "application/problem+json" } },
          );
        }
        return HttpResponse.json({ elementy: [], nastepny_kursor: null });
      }),
    );
    renderuj(<ExerciseList />);
    expect(await screen.findByRole("alert")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Spróbuj ponownie" }));
    expect(await screen.findByText("Brak zadań w wybranym dziale.")).toBeInTheDocument();
  });
});

describe("ExerciseDetail", () => {
  it("pokazuje treść z KaTeX, punkty_max, trudność i Umiejętność", async () => {
    await zalogujDemo();
    const { container } = renderuj(<ExerciseDetail zadanieId={ZADANIE_4} />);
    expect(await screen.findByText("Punkty do zdobycia: 3", { exact: false })).toBeInTheDocument();
    expect(
      screen.getByText("Obliczanie wartości funkcji trygonometrycznych kąta ostrego", {
        exact: false,
      }),
    ).toBeInTheDocument();
    expect(screen.getByText(/główna/)).toBeInTheDocument();
    expect(screen.getByText(/Poziom trudności: 3 z 5/)).toBeInTheDocument();
    expect(container.querySelectorAll(".katex").length).toBeGreaterThan(0);
    expect(container.textContent).not.toContain("$");
  });

  it("Zadanie z procentem renderuje $20\\%$ jako wzór", async () => {
    await zalogujDemo();
    const { container } = renderuj(<ExerciseDetail zadanieId={ZADANIE_3} />);
    await screen.findByText(/Punkty do zdobycia/);
    expect(container.querySelectorAll(".katex").length).toBeGreaterThan(0);
    expect(container.textContent).not.toContain("$");
    expect(container.textContent).toContain("20");
  });

  it("przycisk „Prześlij rozwiązanie” otwiera i zamyka sekcję przesyłania (slot)", async () => {
    await zalogujDemo();
    renderuj(<ExerciseDetail zadanieId={ZADANIE_1} przeslanie={<p>Slot przesyłania</p>} />);
    const przycisk = await screen.findByRole("button", { name: "Prześlij rozwiązanie" });
    expect(przycisk).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByText("Slot przesyłania")).not.toBeInTheDocument();
    await userEvent.click(przycisk);
    expect(screen.getByText("Slot przesyłania")).toBeInTheDocument();
    expect(przycisk).toHaveAttribute("aria-expanded", "true");
    await userEvent.click(przycisk);
    expect(screen.queryByText("Slot przesyłania")).not.toBeInTheDocument();
  });

  it("nieistniejący exerciseId pokazuje stronę „Nie znaleziono zadania”", async () => {
    await zalogujDemo();
    renderuj(<ExerciseDetail zadanieId="a1000000-0000-4000-8000-0000000000ff" />);
    expect(
      await screen.findByRole("heading", { name: "Nie znaleziono zadania" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Wróć do listy zadań" })).toHaveAttribute(
      "href",
      "/exercises",
    );
  });

  it("id niebędące UUID także daje 404 (kontrakt 5.2)", async () => {
    await zalogujDemo();
    renderuj(<ExerciseDetail zadanieId="nie-uuid" />);
    expect(
      await screen.findByRole("heading", { name: "Nie znaleziono zadania" }),
    ).toBeInTheDocument();
  });
});
