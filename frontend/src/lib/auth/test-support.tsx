// @Role: Pomocnicy testów komponentów sesji: mock next/navigation, render z next-intl/QueryClient/SessionProvider, przejście fetch na pełny adres dla MSW | @DependsOn: next-intl, @tanstack/react-query, lib/auth/SessionProvider, messages/pl-PL.json | @UsedBy: testy lib/auth i features/accounts | @Task: TASK-133
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import type { ReactElement } from "react";
import { vi } from "vitest";
import komunikaty from "../../../messages/pl-PL.json";
import { SessionProvider } from "./SessionProvider";

export const przekieruj = { replace: vi.fn(), push: vi.fn() };
export let biezacaSciezka = "/";
export function ustawSciezke(sciezka: string): void {
  biezacaSciezka = sciezka;
}

/** Fabryka dla vi.mock("next/navigation", ...). */
export const nawigacjaMock = {
  useRouter: () => ({ replace: przekieruj.replace, push: przekieruj.push, back: vi.fn() }),
  usePathname: () => biezacaSciezka,
};

/** Po `server.listen()` — klient woła ścieżki względne, a Node fetch wymaga pełnego adresu. */
export function przekierujFetchNaLocalhost(): void {
  const przechwycony = globalThis.fetch;
  vi.stubGlobal("fetch", (adres: RequestInfo | URL, init?: RequestInit) =>
    przechwycony(
      typeof adres === "string" && adres.startsWith("/")
        ? new URL(adres, "http://localhost")
        : adres,
      init,
    ),
  );
}

export function renderujZSesja(ui: ReactElement, klient = new QueryClient()) {
  return render(
    <NextIntlClientProvider locale="pl-PL" messages={komunikaty}>
      <QueryClientProvider client={klient}>
        <SessionProvider>{ui}</SessionProvider>
      </QueryClientProvider>
    </NextIntlClientProvider>,
  );
}
