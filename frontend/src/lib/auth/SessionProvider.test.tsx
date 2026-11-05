// @Role: Testy SessionProvider: token tylko w pamięci, single-flight odświeżenia, proaktywne odświeżenie 60 s przed wygaśnięciem, wylogowanie | @DependsOn: lib/auth/SessionProvider, mocks/server, mocks/db | @UsedBy: vitest | @Task: TASK-133
import { useEffect } from "react";
import { act, screen, waitFor } from "@testing-library/react";
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { resetujStan, WAZNOSC_TOKENU_MS } from "@/mocks/db";
import { server } from "@/mocks/server";
import { przekierujFetchNaLocalhost, renderujZSesja } from "./test-support";
import { ZAPAS_ODSWIEZENIA_MS, useSession } from "./SessionProvider";

let zadania: { metoda: string; sciezka: string }[] = [];
let sesja: ReturnType<typeof useSession> | null = null;

function Sonda() {
  const biezaca = useSession();
  useEffect(() => {
    sesja = biezaca;
  });
  return <p data-testid="stan">{biezaca.stan}</p>;
}
const liczbaPost = (sciezka: string) =>
  zadania.filter((z) => z.metoda === "POST" && z.sciezka === sciezka).length;

beforeAll(() => {
  server.listen({ onUnhandledRequest: "error" });
  przekierujFetchNaLocalhost();
  server.events.on("request:start", ({ request }) => {
    zadania.push({ metoda: request.method, sciezka: new URL(request.url).pathname });
  });
});
beforeEach(() => {
  resetujStan();
  zadania = [];
  sesja = null;
});
afterEach(() => {
  server.events.removeAllListeners("response:mocked");
  server.resetHandlers();
  vi.useRealTimers();
});
afterAll(() => {
  server.close();
  vi.unstubAllGlobals();
});

describe("SessionProvider", () => {
  it("bez poprzedniej sesji kończy jako anonimowy po jednym odświeżeniu", async () => {
    renderujZSesja(<Sonda />);
    expect(screen.getByTestId("stan")).toHaveTextContent("ladowanie");
    await waitFor(() => expect(screen.getByTestId("stan")).toHaveTextContent("anonimowy"));
    expect(liczbaPost("/api/v1/auth/odswiezenie")).toBe(1);
  });

  it("token dostępu nie trafia do localStorage, sessionStorage ani document.cookie", async () => {
    let token = "";
    server.events.on("response:mocked", async ({ request, response }) => {
      if (request.url.endsWith("/auth/logowanie")) {
        token = ((await response.clone().json()) as { access_token: string }).access_token;
      }
    });
    renderujZSesja(<Sonda />);
    await waitFor(() => expect(sesja?.stan).toBe("anonimowy"));
    await act(() => sesja!.zaloguj({ email: "uczen1@lemat.local", haslo: "Lemat-Demo-2026!" }));
    expect(sesja?.stan).toBe("zalogowany");
    expect(sesja?.uzytkownik?.imie).toBe("Ala");
    await waitFor(() => expect(token).not.toBe(""));
    for (const magazyn of [localStorage, sessionStorage]) {
      for (let i = 0; i < magazyn.length; i++) {
        const klucz = magazyn.key(i)!;
        expect(magazyn.getItem(klucz)).not.toContain(token);
      }
    }
    expect(document.cookie).not.toContain(token);
    expect(document.cookie).not.toContain("access_token");
  });

  it("odświeża token 60 s przed wygaśnięciem", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout", "Date"] });
    renderujZSesja(<Sonda />);
    await act(() => vi.advanceTimersByTimeAsync(200));
    expect(sesja?.stan).toBe("anonimowy");
    await act(() => sesja!.zaloguj({ email: "uczen1@lemat.local", haslo: "Lemat-Demo-2026!" }));
    const przed = liczbaPost("/api/v1/auth/odswiezenie");

    await act(() => vi.advanceTimersByTimeAsync(WAZNOSC_TOKENU_MS - ZAPAS_ODSWIEZENIA_MS - 5000));
    expect(liczbaPost("/api/v1/auth/odswiezenie")).toBe(przed);
    await act(() => vi.advanceTimersByTimeAsync(6000));
    expect(liczbaPost("/api/v1/auth/odswiezenie")).toBe(przed + 1);
    expect(sesja?.stan).toBe("zalogowany");
  });

  it("wyloguj czyści stan i wywołuje endpoint", async () => {
    renderujZSesja(<Sonda />);
    await waitFor(() => expect(sesja?.stan).toBe("anonimowy"));
    await act(() => sesja!.zaloguj({ email: "uczen1@lemat.local", haslo: "Lemat-Demo-2026!" }));
    await act(() => sesja!.wyloguj());
    expect(sesja?.stan).toBe("anonimowy");
    expect(sesja?.uzytkownik).toBeNull();
    expect(liczbaPost("/api/v1/auth/wylogowanie")).toBe(1);
  });
});
