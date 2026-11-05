// @Role: Testy strony głównej (linki, przekierowanie zalogowanego), granic błędów | @DependsOn: features/accounts, app/error, app/(student)/error, app/not-found | @UsedBy: vitest | @Task: TASK-138
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { NextIntlClientProvider } from "next-intl";
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import komunikaty from "../../../messages/pl-PL.json";
import { server } from "@/mocks/server";
import { resetujStan } from "@/mocks/db";
import {
  przekieruj,
  przekierujFetchNaLocalhost,
  renderujZSesja,
  ustawSciezke,
} from "@/lib/auth/test-support";
import Blad from "@/app/error";
import BladStrefyUcznia from "@/app/(student)/error";
import NieZnaleziono from "@/app/not-found";
import { HomeView } from "./index";

vi.mock("next/navigation", async () => (await import("@/lib/auth/test-support")).nawigacjaMock);

beforeAll(() => {
  server.listen({ onUnhandledRequest: "error" });
  przekierujFetchNaLocalhost();
});
beforeEach(() => {
  resetujStan();
  przekieruj.replace.mockClear();
  ustawSciezke("/");
});
afterEach(() => {
  server.resetHandlers();
});
afterAll(() => {
  server.close();
  vi.unstubAllGlobals();
});

const zIntl = (ui: React.ReactElement) =>
  render(
    <NextIntlClientProvider locale="pl-PL" messages={komunikaty}>
      {ui}
    </NextIntlClientProvider>,
  );

describe("HomeView", () => {
  it("anonimowy widzi linki Zaloguj się i Załóż konto", async () => {
    renderujZSesja(<HomeView />);
    expect(await screen.findByRole("link", { name: "Zaloguj się" })).toHaveAttribute(
      "href",
      "/login",
    );
    expect(screen.getByRole("link", { name: "Załóż konto" })).toHaveAttribute("href", "/register");
    expect(przekieruj.replace).not.toHaveBeenCalled();
  });

  it("zalogowany (sesja z ciasteczka) jest przekierowany na /exercises", async () => {
    const { zaloguj } = await import("@/lib/api/client");
    await zaloguj({ email: "uczen1@lemat.local", haslo: "Lemat-Demo-2026!" });
    renderujZSesja(<HomeView />);
    await waitFor(() => expect(przekieruj.replace).toHaveBeenCalledWith("/exercises"));
  });
});

describe("granice błędów i 404", () => {
  it("error.tsx pokazuje komunikat i wywołuje reset", async () => {
    const reset = vi.fn();
    zIntl(<Blad error={new Error("x")} reset={reset} />);
    expect(screen.getByRole("alert")).toHaveTextContent("Wystąpił nieoczekiwany błąd");
    await userEvent.setup().click(screen.getByRole("button", { name: "Spróbuj ponownie" }));
    expect(reset).toHaveBeenCalledTimes(1);
  });

  it("(student)/error.tsx pokazuje komunikat i wywołuje reset", async () => {
    const reset = vi.fn();
    zIntl(<BladStrefyUcznia error={new Error("x")} reset={reset} />);
    await userEvent.setup().click(screen.getByRole("button", { name: "Spróbuj ponownie" }));
    expect(reset).toHaveBeenCalledTimes(1);
  });

  it("not-found.tsx pokazuje polski komunikat i link na stronę główną", () => {
    zIntl(<NieZnaleziono />);
    expect(screen.getByText("Nie znaleziono strony")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Wróć na stronę główną" })).toHaveAttribute(
      "href",
      "/",
    );
  });
});
