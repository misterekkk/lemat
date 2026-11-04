// @Role: Testy przesłania zdjęć kartki na mockach MSW z wstrzykniętym konwerterem: 1…5 plików, limit, usuwanie, multipart, nawigacja, błędy 409/413/415/422, pojedyncze żądanie | @DependsOn: features/solutions, features/accounts, lib/auth/test-support, mocks/server | @UsedBy: vitest | @Task: TASK-136
import { QueryClient } from "@tanstack/react-query";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { File as PlikWezla } from "node:buffer";
import { HttpResponse, http } from "msw";
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { StudentShell } from "@/features/accounts";
import { skonfigurujKlienta, zaloguj } from "@/lib/api/client";
import { przekieruj, renderujZSesja, ustawSciezke } from "@/lib/auth/test-support";
import { BladZdjecia, MAKS_BAJTOW_ZDJECIA, type KonwerterJpeg } from "@/lib/photos/to-jpeg";
import { resetujStan } from "@/mocks/db";
import { server } from "@/mocks/server";
import { PhotoUpload } from "./index";

vi.mock("next/navigation", async () => (await import("@/lib/auth/test-support")).nawigacjaMock);

/** File z jsdom (pola formularza w testach); globalny File jest w tym pliku podmieniany na natywny. */
const PlikJsdom = File;

const ZADANIE_2 = "a1000000-0000-4000-8000-000000000002";
const WERSJA_2 = "b1000000-0000-4000-8000-000000000002";
const ID_PIERWSZEGO = "d1000000-0000-4000-8000-000000000010";

/** Minimalny poprawny JPEG (SOI, SOF0 16x16, EOI) — przechodzi kontrolę mocka. */
const BAJTY_JPEG = new Uint8Array([
  0xff, 0xd8, 0xff, 0xc0, 0x00, 0x11, 0x08, 0x00, 0x10, 0x00, 0x10, 0x03, 0x01, 0x22, 0x00, 0x02,
  0x11, 0x01, 0x03, 0x11, 0x01, 0xff, 0xd9,
]);

const konwerterJpeg: KonwerterJpeg = () =>
  Promise.resolve(new Blob([BAJTY_JPEG], { type: "image/jpeg" }));

/** Node fetch nie przyjmuje FormData z jsdom — przepisujemy je na natywny FormData z plikami z node:buffer. */
async function natywnyFormData(zrodlo: FormData): Promise<FormData> {
  const odpowiedz = new Response("a=1", {
    headers: { "content-type": "application/x-www-form-urlencoded" },
  });
  const NatywnyFormData = Object.getPrototypeOf(await odpowiedz.formData()).constructor as {
    new (): FormData;
  };
  const wynik = new NatywnyFormData();
  for (const [nazwa, wartosc] of zrodlo.entries()) {
    if (typeof wartosc === "string") wynik.append(nazwa, wartosc);
    else {
      const bajty = await new Promise<ArrayBuffer>((resolve, reject) => {
        const czytnik = new FileReader();
        czytnik.onload = () => resolve(czytnik.result as ArrayBuffer);
        czytnik.onerror = () => reject(new Error("odczyt"));
        czytnik.readAsArrayBuffer(wartosc);
      });
      wynik.append(
        nazwa,
        new PlikWezla([new Uint8Array(bajty)], wartosc.name, {
          type: wartosc.type,
        }) as unknown as Blob,
        wartosc.name,
      );
    }
  }
  return wynik;
}

function przekierujFetchMultipart(): void {
  const przechwycony = globalThis.fetch;
  vi.stubGlobal("fetch", async (adres: RequestInfo | URL, init?: RequestInit) => {
    const pelny =
      typeof adres === "string" && adres.startsWith("/")
        ? new URL(adres, "http://localhost")
        : adres;
    const cialo = init?.body instanceof FormData ? await natywnyFormData(init.body) : init?.body;
    return przechwycony(pelny, { ...init, body: cialo });
  });
}

interface ZarejestrowaneZadanie {
  pole: string[];
  liczbaZdjec: number;
  naglowekScenariusza: string | null;
  zadanieWersjaId: string | null;
  typCiala: string | null;
}
let zarejestrowane: ZarejestrowaneZadanie[] = [];

beforeAll(() => {
  server.listen({ onUnhandledRequest: "error" });
  // Parser multipart w undici (po stronie mocka) wymaga natywnego File, a jsdom podmienia globalny.
  vi.stubGlobal("File", PlikWezla);
  przekierujFetchMultipart();
  server.events.on("request:start", async ({ request }) => {
    if (request.method !== "POST" || !new URL(request.url).pathname.endsWith("/rozwiazania")) {
      return;
    }
    const formularz = await request.clone().formData();
    zarejestrowane.push({
      pole: [...formularz.keys()],
      liczbaZdjec: formularz.getAll("zdjecia").length,
      naglowekScenariusza: request.headers.get("X-Lemat-Mock-Scenariusz"),
      zadanieWersjaId: formularz.get("zadanie_wersja_id") as string | null,
      typCiala: request.headers.get("Content-Type"),
    });
  });
});
beforeEach(() => {
  resetujStan();
  ustawSciezke("/exercises/" + ZADANIE_2);
  przekieruj.push.mockClear();
  zarejestrowane = [];
  URL.createObjectURL = vi.fn(() => "blob:miniatura");
  URL.revokeObjectURL = vi.fn();
});
afterEach(() => {
  server.resetHandlers();
  vi.unstubAllEnvs();
});
afterAll(() => {
  server.close();
  vi.unstubAllGlobals();
});

async function zalogujDemo(): Promise<void> {
  skonfigurujKlienta({ pobierzToken: () => null, odswiezSesje: () => Promise.resolve(null) });
  await zaloguj({ email: "uczen1@lemat.local", haslo: "Lemat-Demo-2026!" });
}

async function renderuj(konwerter: KonwerterJpeg = konwerterJpeg) {
  await zalogujDemo();
  const wynik = renderujZSesja(
    <StudentShell>
      <PhotoUpload zadanieId={ZADANIE_2} konwerter={konwerter} />
    </StudentShell>,
    new QueryClient({ defaultOptions: { queries: { retry: false } } }),
  );
  await screen.findByRole("heading", { name: "Zdjęcia kartki" });
  return wynik;
}

function pliki(n: number, nazwa = "foto"): File[] {
  return Array.from(
    { length: n },
    (_, i) => new PlikJsdom(["x"], `${nazwa}-${i + 1}.heic`, { type: "image/heic" }),
  );
}
const pole = () => screen.getByLabelText("Dodaj zdjęcia kartki");
const przyciskWyslij = () => screen.getByRole("button", { name: /Wyślij do oceny|Wysyłanie/ });

function problem(status: number, kod: string, szczegoly: Record<string, unknown> | null = null) {
  return HttpResponse.json(
    {
      type: "https://lemat.pl/bledy/x",
      title: "Błąd",
      status,
      detail: "Szczegół z serwera.",
      instance: "/api/v1/rozwiazania",
      kod,
      trace_id: "t",
      szczegoly,
    },
    { status, headers: { "Content-Type": "application/problem+json" } },
  );
}

describe("PhotoUpload", () => {
  it("pole pliku ma accept, capture i multiple, a wysyłka jest zablokowana bez zdjęć", async () => {
    await renderuj();
    expect(pole()).toHaveAttribute("accept", "image/*");
    expect(pole()).toHaveAttribute("capture", "environment");
    expect(pole()).toHaveAttribute("multiple");
    expect(screen.getByText("Dodane zdjęcia: 0 z 5")).toBeInTheDocument();
    await waitFor(() => expect(przyciskWyslij()).toBeDisabled());
  });

  it("przyjmuje 1 … 5 plików i pokazuje licznik oraz miniatury", async () => {
    await renderuj();
    await userEvent.upload(pole(), pliki(1));
    expect(await screen.findByText("Dodane zdjęcia: 1 z 5")).toBeInTheDocument();
    await userEvent.upload(pole(), pliki(4, "kolejne"));
    expect(await screen.findByText("Dodane zdjęcia: 5 z 5")).toBeInTheDocument();
    expect(screen.getAllByRole("img")).toHaveLength(5);
    expect(screen.getByAltText("Zdjęcie kartki 5")).toBeInTheDocument();
    await waitFor(() => expect(przyciskWyslij()).toBeEnabled());
  });

  it("odrzuca 6. plik komunikatem o limicie", async () => {
    await renderuj();
    await userEvent.upload(pole(), pliki(3));
    await screen.findByText("Dodane zdjęcia: 3 z 5");
    await userEvent.upload(pole(), pliki(3, "dodatkowe"));
    expect(await screen.findByText("Dodane zdjęcia: 5 z 5")).toBeInTheDocument();
    expect(screen.getByRole("alert")).toHaveTextContent("Można dodać najwyżej 5 zdjęć kartki");
  });

  it("usuwa miniaturę i zwalnia jej adres", async () => {
    await renderuj();
    await userEvent.upload(pole(), pliki(2));
    await screen.findByText("Dodane zdjęcia: 2 z 5");
    await userEvent.click(screen.getByRole("button", { name: "Usuń zdjęcie 1" }));
    expect(screen.getByText("Dodane zdjęcia: 1 z 5")).toBeInTheDocument();
    expect(screen.getAllByRole("img")).toHaveLength(1);
    expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:miniatura");
  });

  it("nieczytelny plik (np. HEIC) daje komunikat i nie trafia na listę", async () => {
    const konwerter: KonwerterJpeg = () => Promise.reject(new BladZdjecia("NIECZYTELNE"));
    await renderuj(konwerter);
    await userEvent.upload(pole(), pliki(1, "iphone"));
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Nie udało się odczytać pliku „iphone-1.heic”",
    );
    expect(screen.getByText("Dodane zdjęcia: 0 z 5")).toBeInTheDocument();
  });

  it("odrzuca zdjęcie większe niż 10 MB po konwersji", async () => {
    const konwerter: KonwerterJpeg = () =>
      Promise.resolve(new Blob([new Uint8Array(MAKS_BAJTOW_ZDJECIA + 1)], { type: "image/jpeg" }));
    await renderuj(konwerter);
    await userEvent.upload(pole(), pliki(1));
    expect(await screen.findByRole("alert")).toHaveTextContent("jest zbyt duże nawet po kompresji");
    expect(screen.getByText("Dodane zdjęcia: 0 z 5")).toBeInTheDocument();
  });

  it("wysyła multipart (zadanie_wersja_id, zdjecia) bez nagłówka scenariusza i przechodzi na /solutions/<id>", async () => {
    await renderuj();
    await userEvent.upload(pole(), pliki(2));
    await screen.findByText("Dodane zdjęcia: 2 z 5");
    await waitFor(() => expect(przyciskWyslij()).toBeEnabled());
    await userEvent.click(przyciskWyslij());
    await waitFor(() =>
      expect(przekieruj.push).toHaveBeenCalledWith(`/solutions/${ID_PIERWSZEGO}`),
    );
    expect(zarejestrowane).toHaveLength(1);
    expect(zarejestrowane[0]).toMatchObject({
      pole: ["zadanie_wersja_id", "zdjecia", "zdjecia"],
      liczbaZdjec: 2,
      zadanieWersjaId: WERSJA_2,
      naglowekScenariusza: null,
    });
    expect(zarejestrowane[0]?.typCiala).toContain("multipart/form-data");
  });

  it("wysyła nagłówek X-Lemat-Mock-Scenariusz tylko gdy ustawiono zmienną środowiskową", async () => {
    vi.stubEnv("NEXT_PUBLIC_LEMAT_MOCK_SCENARIUSZ", "rozpoznanie-nieczytelne");
    await renderuj();
    await userEvent.upload(pole(), pliki(1));
    await screen.findByText("Dodane zdjęcia: 1 z 5");
    await waitFor(() => expect(przyciskWyslij()).toBeEnabled());
    await userEvent.click(przyciskWyslij());
    await waitFor(() => expect(przekieruj.push).toHaveBeenCalled());
    expect(zarejestrowane[0]?.naglowekScenariusza).toBe("rozpoznanie-nieczytelne");
  });

  it("podwójne kliknięcie wysyła jedno żądanie, a przycisk jest zablokowany w trakcie", async () => {
    await renderuj();
    await userEvent.upload(pole(), pliki(1));
    await screen.findByText("Dodane zdjęcia: 1 z 5");
    await waitFor(() => expect(przyciskWyslij()).toBeEnabled());
    await userEvent.dblClick(przyciskWyslij());
    await waitFor(() => expect(przekieruj.push).toHaveBeenCalledTimes(1));
    expect(zarejestrowane).toHaveLength(1);
    expect(przyciskWyslij()).toBeDisabled();
  });

  it("409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN pokazuje link do istniejącego Rozwiązania", async () => {
    const pierwsze = await renderuj();
    await userEvent.upload(pole(), pliki(1));
    await screen.findByText("Dodane zdjęcia: 1 z 5");
    await waitFor(() => expect(przyciskWyslij()).toBeEnabled());
    await userEvent.click(przyciskWyslij());
    await waitFor(() => expect(przekieruj.push).toHaveBeenCalledTimes(1));
    pierwsze.unmount();

    await renderuj();
    await userEvent.upload(pole(), pliki(1));
    await screen.findByText("Dodane zdjęcia: 1 z 5");
    await waitFor(() => expect(przyciskWyslij()).toBeEnabled());
    await userEvent.click(przyciskWyslij());
    const link = await screen.findByRole("link", { name: "Przejdź do rozwiązania" });
    expect(link).toHaveAttribute("href", `/solutions/${ID_PIERWSZEGO}`);
    expect(przekieruj.push).toHaveBeenCalledTimes(1);
    expect(przyciskWyslij()).toBeEnabled();
  });

  it("409 WERSJA_ZADANIA_NIEAKTUALNA odświeża Zadanie i pokazuje komunikat", async () => {
    let odczytyZadania = 0;
    server.use(
      http.get("*/api/v1/zadania/:id", () => {
        odczytyZadania += 1;
        return undefined;
      }),
      http.post("*/api/v1/rozwiazania", () =>
        problem(409, "WERSJA_ZADANIA_NIEAKTUALNA", { aktualna_zadanie_wersja_id: "b2" }),
      ),
    );
    await renderuj();
    await userEvent.upload(pole(), pliki(1));
    await screen.findByText("Dodane zdjęcia: 1 z 5");
    await waitFor(() => expect(przyciskWyslij()).toBeEnabled());
    const przed = odczytyZadania;
    await userEvent.click(przyciskWyslij());
    expect(await screen.findByRole("alert")).toHaveTextContent("Treść zadania się zmieniła");
    await waitFor(() => expect(odczytyZadania).toBeGreaterThan(przed));
  });

  it.each([
    [415, "NIEOBSLUGIWANY_FORMAT", "Nie udało się przyjąć zdjęcia"],
    [422, "ZDJECIE_NIEPRAWIDLOWE", "Nie udało się przyjąć zdjęcia"],
    [413, "ZDJECIE_ZA_DUZE", "Zdjęcie jest zbyt duże"],
    [413, "ZAPYTANIE_ZA_DUZE", "Wysyłane dane są zbyt duże"],
    [422, "ZA_DUZO_ZDJEC", "Dodaj od 1 do 5 zdjęć kartki"],
  ])("%i %s pokazuje polski komunikat i pozwala spróbować ponownie", async (status, kod, tekst) => {
    server.use(http.post("*/api/v1/rozwiazania", () => problem(status, kod)));
    await renderuj();
    await userEvent.upload(pole(), pliki(1));
    await screen.findByText("Dodane zdjęcia: 1 z 5");
    await waitFor(() => expect(przyciskWyslij()).toBeEnabled());
    await userEvent.click(przyciskWyslij());
    expect(await screen.findByRole("alert")).toHaveTextContent(tekst);
    expect(przekieruj.push).not.toHaveBeenCalled();
    expect(przyciskWyslij()).toBeEnabled();
  });
});
