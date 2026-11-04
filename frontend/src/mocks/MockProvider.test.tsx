// @Role: Testy MockProvider (warunkowe uruchomienie MSW wg NEXT_PUBLIC_LEMAT_API) i trwałości sesji mocka w localStorage (jsdom) | @DependsOn: mocks/MockProvider, mocks/db, @testing-library/react | @UsedBy: vitest | @Task: TASK-132
import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { dodajKonto, odswiezSesjeMocka, resetujStan, wystawSesje } from "./db";
import { MockProvider } from "./MockProvider";

const uruchomMock = vi.fn(() => Promise.resolve());
vi.mock("./browser", () => ({ uruchomMock: () => uruchomMock() }));

beforeEach(() => {
  uruchomMock.mockClear();
  resetujStan();
});
afterEach(() => {
  vi.unstubAllEnvs();
});

describe("MockProvider", () => {
  it("w trybie real renderuje dzieci od razu i nie uruchamia MSW", () => {
    vi.stubEnv("NEXT_PUBLIC_LEMAT_API", "real");
    render(<MockProvider>treść</MockProvider>);
    expect(screen.getByText("treść")).toBeInTheDocument();
    expect(uruchomMock).not.toHaveBeenCalled();
  });

  it("w trybie mock wstrzymuje dzieci do uruchomienia workera", async () => {
    vi.stubEnv("NEXT_PUBLIC_LEMAT_API", "mock");
    render(<MockProvider>treść</MockProvider>);
    expect(screen.queryByText("treść")).not.toBeInTheDocument();
    await waitFor(() => expect(screen.getByText("treść")).toBeInTheDocument());
    expect(uruchomMock).toHaveBeenCalledTimes(1);
  });
});

describe("trwałość mocka w localStorage (tylko tryb MSW)", () => {
  it("odświeżenie działa po zapisie w localStorage, a token dostępu tam nie trafia", () => {
    const konto = dodajKonto({
      email: "x@example.com",
      haslo: "BardzoTajne#2026",
      imie: "X",
      nazwisko: "Y",
    });
    const sesja = wystawSesje(konto);
    const zapis = window.localStorage.getItem("lemat_mock_stan") ?? "";
    expect(zapis).toContain("x@example.com");
    expect(zapis).not.toContain(sesja.access_token);
    expect(odswiezSesjeMocka()?.uzytkownik.id).toBe(konto.id);
    resetujStan();
    expect(odswiezSesjeMocka()).toBeNull();
  });
});
