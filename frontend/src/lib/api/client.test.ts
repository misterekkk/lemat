// @Role: Testy klienta API z wstrzykniętym fetch: Authorization, If-Match, odświeżanie po 401, ProblemDetails, BLAD_BRAMY/SIECI, multipart, merge-patch | @DependsOn: lib/api/client, lib/api/errors | @UsedBy: vitest | @Task: TASK-131
import { beforeEach, describe, expect, it, vi } from "vitest";
import { BladApi } from "./errors";
import {
  pobierzUcznia,
  pobierzZadania,
  przeslijRozwiazanie,
  skonfigurujKlienta,
  zaloguj,
  zatwierdzRozwiazanie,
  zmienUcznia,
} from "./client";

function problem(status: number, kod: string, extra: Record<string, unknown> = {}): Response {
  return new Response(
    JSON.stringify({
      type: `https://lemat.pl/bledy/${kod.toLowerCase().replaceAll("_", "-")}`,
      title: "t",
      status,
      detail: "Szczegóły po polsku.",
      instance: "/api/v1/x",
      kod,
      trace_id: "abc",
      szczegoly: null,
      ...extra,
    }),
    { status, headers: { "content-type": "application/problem+json" } },
  );
}
function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json; charset=utf-8" },
  });
}
function naglowek(mock: ReturnType<typeof vi.fn>, n: number, nazwa: string): string | null {
  const init = mock.mock.calls[n]?.[1] as RequestInit;
  return new Headers(init.headers).get(nazwa);
}

let fetchMock: ReturnType<typeof vi.fn>;
let token: string | null;
let odswiezSesje: ReturnType<typeof vi.fn>;

beforeEach(() => {
  fetchMock = vi.fn();
  token = "tok-1";
  odswiezSesje = vi.fn(async () => {
    token = "tok-2";
    return "tok-2";
  });
  skonfigurujKlienta({
    pobierzToken: () => token,
    odswiezSesje,
    fetchImpl: fetchMock as unknown as typeof fetch,
  });
});

describe("klient API", () => {
  it("dodaje Authorization, cache no-store i adres /api/v1", async () => {
    fetchMock.mockResolvedValueOnce(json({ version: 1 }));
    await pobierzUcznia();
    expect(fetchMock.mock.calls[0]?.[0]).toBe("/api/v1/uczniowie/ja");
    expect(naglowek(fetchMock, 0, "Authorization")).toBe("Bearer tok-1");
    expect((fetchMock.mock.calls[0]?.[1] as RequestInit).cache).toBe("no-store");
  });

  it("endpointy anonimowe nie wysyłają Authorization", async () => {
    fetchMock.mockResolvedValueOnce(json({ access_token: "x" }));
    await zaloguj({ email: "a@b.pl", haslo: "x" });
    expect(naglowek(fetchMock, 0, "Authorization")).toBeNull();
    expect(naglowek(fetchMock, 0, "Content-Type")).toBe("application/json");
  });

  it("PATCH używa merge-patch i If-Match w cudzysłowach", async () => {
    fetchMock.mockResolvedValueOnce(json({ version: 2 }));
    await zmienUcznia({ wybrany_dzial_id: null }, 1);
    const init = fetchMock.mock.calls[0]?.[1] as RequestInit;
    expect(init.method).toBe("PATCH");
    expect(naglowek(fetchMock, 0, "If-Match")).toBe('"1"');
    expect(naglowek(fetchMock, 0, "Content-Type")).toBe("application/merge-patch+json");
    expect(init.body).toBe('{"wybrany_dzial_id":null}');
  });

  it("zatwierdzenie wysyła If-Match z version", async () => {
    fetchMock.mockResolvedValueOnce(json({ version: 4 }, 202));
    await zatwierdzRozwiazanie("r1", { transkrypcja_id: "t1", wersja: 1 }, 3);
    expect(fetchMock.mock.calls[0]?.[0]).toBe("/api/v1/rozwiazania/r1/zatwierdzenie");
    expect(naglowek(fetchMock, 0, "If-Match")).toBe('"3"');
  });

  it("buduje zapytanie i pomija niezdefiniowane parametry", async () => {
    fetchMock.mockResolvedValueOnce(json({ elementy: [], nastepny_kursor: null }));
    await pobierzZadania({ dzial_id: 5, kursor: undefined });
    expect(fetchMock.mock.calls[0]?.[0]).toBe("/api/v1/zadania?dzial_id=5");
  });

  it("po 401 odświeża raz i ponawia raz z nowym tokenem", async () => {
    fetchMock
      .mockResolvedValueOnce(problem(401, "NIEUWIERZYTELNIONY"))
      .mockResolvedValueOnce(json({ version: 1 }));
    await expect(pobierzUcznia()).resolves.toEqual({ version: 1 });
    expect(odswiezSesje).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(naglowek(fetchMock, 1, "Authorization")).toBe("Bearer tok-2");
  });

  it("drugi 401 kończy się błędem, bez pętli", async () => {
    fetchMock.mockImplementation(async () => problem(401, "NIEUWIERZYTELNIONY"));
    await expect(pobierzUcznia()).rejects.toMatchObject({ kod: "NIEUWIERZYTELNIONY", status: 401 });
    expect(odswiezSesje).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("401 na logowaniu nie wywołuje odświeżania", async () => {
    fetchMock.mockResolvedValueOnce(problem(401, "NIEUWIERZYTELNIONY"));
    await expect(zaloguj({ email: "a@b.pl", haslo: "x" })).rejects.toBeInstanceOf(BladApi);
    expect(odswiezSesje).not.toHaveBeenCalled();
  });

  it("gdy odświeżenie zwróci null, zwraca oryginalny błąd 401", async () => {
    odswiezSesje.mockResolvedValueOnce(null);
    fetchMock.mockResolvedValueOnce(problem(401, "NIEUWIERZYTELNIONY"));
    await expect(pobierzUcznia()).rejects.toMatchObject({ kod: "NIEUWIERZYTELNIONY" });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("propaguje SESJA_UNIEWAZNIONA z odświeżania", async () => {
    odswiezSesje.mockRejectedValueOnce(new BladApi({ status: 401, kod: "SESJA_UNIEWAZNIONA" }));
    fetchMock.mockResolvedValueOnce(problem(401, "NIEUWIERZYTELNIONY"));
    await expect(pobierzUcznia()).rejects.toMatchObject({ kod: "SESJA_UNIEWAZNIONA" });
  });

  it("ProblemDetails -> BladApi z kodem, problemem i bledyPol", async () => {
    const bledy_pol = [{ pole: "haslo", kod_reguly: "ZA_KROTKIE", komunikat: "Za krótkie." }];
    fetchMock.mockResolvedValueOnce(problem(422, "WALIDACJA", { bledy_pol }));
    const blad = await zaloguj({ email: "a", haslo: "b" }).catch((e: unknown) => e);
    expect(blad).toBeInstanceOf(BladApi);
    const b = blad as BladApi;
    expect(b.status).toBe(422);
    expect(b.kod).toBe("WALIDACJA");
    expect(b.bledyPol).toEqual(bledy_pol);
    expect(b.problem?.trace_id).toBe("abc");
  });

  it("412 niesie szczegoly.aktualny", async () => {
    fetchMock.mockResolvedValueOnce(
      problem(412, "KONFLIKT_WERSJI", { szczegoly: { aktualny: { version: 5 } } }),
    );
    const blad = (await zmienUcznia({ zgoda_na_trening: true }, 1).catch(
      (e: unknown) => e,
    )) as BladApi;
    expect(blad.kod).toBe("KONFLIKT_WERSJI");
    expect(blad.aktualny).toEqual({ version: 5 });
  });

  it("odpowiedź HTML 502 -> BLAD_BRAMY", async () => {
    fetchMock.mockResolvedValueOnce(
      new Response("<html>Bad Gateway</html>", {
        status: 502,
        headers: { "content-type": "text/html" },
      }),
    );
    await expect(pobierzUcznia()).rejects.toMatchObject({ kod: "BLAD_BRAMY", status: 502 });
  });

  it("sukces bez JSON -> BLAD_BRAMY", async () => {
    fetchMock.mockResolvedValueOnce(
      new Response("<html></html>", { status: 200, headers: { "content-type": "text/html" } }),
    );
    await expect(pobierzUcznia()).rejects.toMatchObject({ kod: "BLAD_BRAMY" });
  });

  it("błąd sieci -> BLAD_SIECI", async () => {
    fetchMock.mockRejectedValueOnce(new TypeError("Failed to fetch"));
    await expect(pobierzUcznia()).rejects.toMatchObject({ kod: "BLAD_SIECI", status: 0 });
  });

  it("AbortError -> PRZERWANO", async () => {
    fetchMock.mockRejectedValueOnce(new DOMException("aborted", "AbortError"));
    await expect(pobierzUcznia()).rejects.toMatchObject({ kod: "PRZERWANO" });
  });

  it("multipart: FormData bez ręcznego Content-Type, pole zdjecia, nagłówek scenariusza", async () => {
    fetchMock.mockResolvedValueOnce(json({ id: "r1" }, 202));
    const plik = new Blob([new Uint8Array([0xff, 0xd8, 0xff])], { type: "image/jpeg" });
    await przeslijRozwiazanie({
      zadanie_wersja_id: "b1",
      zdjecia: [plik, plik],
      scenariusz: "rozpoznanie-nieczytelne",
    });
    const init = fetchMock.mock.calls[0]?.[1] as RequestInit;
    expect(init.body).toBeInstanceOf(FormData);
    const dane = init.body as FormData;
    expect(dane.get("zadanie_wersja_id")).toBe("b1");
    expect(dane.getAll("zdjecia")).toHaveLength(2);
    expect(naglowek(fetchMock, 0, "Content-Type")).toBeNull();
    expect(naglowek(fetchMock, 0, "X-Lemat-Mock-Scenariusz")).toBe("rozpoznanie-nieczytelne");
  });

  it("204 zwraca undefined", async () => {
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 204 }));
    const { wyloguj } = await import("./client");
    await expect(wyloguj()).resolves.toBeUndefined();
  });
});
