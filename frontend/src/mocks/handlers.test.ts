// @Role: Testy mocków MSW (setupServer) z fałszywymi timerami: przepływ rejestracja→ocena, dane 6.3/7.2/7.3, scenariusze, błędy 1.1, profil 4.3 | @DependsOn: mocks/server, mocks/db, lib/api/client, docs/POC_CONTRACT.md | @UsedBy: vitest | @Task: TASK-132
// @vitest-environment node
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import {
  odswiez,
  pobierzAdresZdjecia,
  pobierzDzialy,
  pobierzOcene,
  pobierzProfilUproszczony,
  pobierzRozwiazania,
  pobierzRozwiazanie,
  pobierzUcznia,
  pobierzZadania,
  pobierzZadanie,
  przeslijRozwiazanie,
  skonfigurujKlienta,
  wyloguj,
  zaloguj,
  zarejestruj,
  zatwierdzRozwiazanie,
  zmienUcznia,
} from "@/lib/api/client";
import { BladApi } from "@/lib/api/errors";
import type { RozwiazanieWyjscie, ScenariuszMock } from "@/lib/api/types";
import { resetujStan } from "./db";
import { server } from "./server";
import dzialyOczekiwane from "./__expected__/dzialy-4-2.json";
import profilOczekiwany from "./__expected__/profil-4-3.json";
import zadaniaOczekiwane from "./__expected__/zadania-6-3.json";
import ocenaPrzyklad from "./__expected__/ocena-zadanie-2-7-5.json";
import transkrypcjaPrzyklad from "./__expected__/transkrypcja-zadanie-2-7-5.json";

const BAZA = "http://localhost/api/v1";
const HASLO_DEMO = "Lemat-Demo-2026!";
const WERSJA = (n: number) => `b1000000-0000-4000-8000-00000000000${n}`;
const ZADANIE = (n: number) => `a1000000-0000-4000-8000-00000000000${n}`;

let token: string | null = null;

/** Minimalny poprawny JPEG: SOI, SOF0 (16x16), EOI. */
function jpeg(): Blob {
  const bajty = [0xff, 0xd8, 0xff, 0xc0, 0x00, 0x11, 0x08, 0x00, 0x10, 0x00, 0x10, 0x03];
  bajty.push(0x01, 0x22, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01, 0xff, 0xd9);
  return new Blob([new Uint8Array(bajty)], { type: "image/jpeg" });
}

async function zalogujJako(email = "uczen1@lemat.local"): Promise<void> {
  const sesja = await zaloguj({ email, haslo: HASLO_DEMO });
  token = sesja.access_token;
}
async function minelo(ms: number): Promise<void> {
  await vi.advanceTimersByTimeAsync(ms);
}
async function przeslij(n: number, scenariusz?: ScenariuszMock): Promise<RozwiazanieWyjscie> {
  return przeslijRozwiazanie({ zadanie_wersja_id: WERSJA(n), zdjecia: [jpeg()], scenariusz });
}
/** Przesyła, czeka na Transkrypcję, zatwierdza i czeka na Ocenę; zwraca Rozwiązanie OCENIONE. */
async function rozwiaz(n: number): Promise<RozwiazanieWyjscie> {
  const wyslane = await przeslij(n);
  await minelo(3000);
  const gotowe = await pobierzRozwiazanie(wyslane.id);
  const tr = gotowe.transkrypcja!;
  await zatwierdzRozwiazanie(
    wyslane.id,
    { transkrypcja_id: tr.id, wersja: tr.wersja },
    gotowe.version,
  );
  await minelo(2000);
  return pobierzRozwiazanie(wyslane.id);
}
async function przechwyc(obietnica: Promise<unknown>): Promise<BladApi> {
  try {
    await obietnica;
  } catch (e) {
    if (e instanceof BladApi) return e;
    throw e;
  }
  throw new Error("Oczekiwano błędu API.");
}
function zNaglowkami(naglowki: Record<string, string> = {}): RequestInit {
  return { headers: { Authorization: `Bearer ${token}`, ...naglowki } };
}

beforeAll(() => server.listen({ onUnhandledRequest: "error" }));
afterAll(() => server.close());
beforeEach(() => {
  vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout", "Date"] });
  vi.setSystemTime(new Date("2026-11-02T10:05:00Z"));
  resetujStan();
  token = null;
  skonfigurujKlienta({
    pobierzToken: () => token,
    odswiezSesje: () => Promise.resolve(null),
    bazowyAdres: BAZA,
  });
});
afterEach(() => {
  resetujStan();
  server.resetHandlers();
  vi.useRealTimers();
});

describe("pełny przepływ (kontrakt 3, 6, 7)", () => {
  it("rejestracja → logowanie → lista Zadań → POST → TRANSKRYPCJA_GOTOWA po 3 s → zatwierdzenie → OCENIONE po 2 s", async () => {
    const rejestracja = await zarejestruj({
      email: "ola.wisniewska@example.com",
      haslo: "BardzoTajne#2026",
      imie: "Ola",
      nazwisko: "Wiśniewska",
      rola: "UCZEN",
      oswiadczenie_wieku_16: true,
      zgoda_regulamin: true,
      kod_zaproszenia: null,
    });
    expect(rejestracja.status_konta).toBe("AKTYWNE");
    expect(rejestracja.sesja?.uzytkownik).toEqual({
      id: "c1000000-0000-4000-8000-000000000003",
      rola: "UCZEN",
      imie: "Ola",
      flagi: { weryfikator_globalny: false, recenzent_tresci: false },
    });

    const sesja = await zaloguj({ email: "OLA.wisniewska@example.com", haslo: "BardzoTajne#2026" });
    token = sesja.access_token;
    expect(sesja.wygasa_at).toBe("2026-11-02T10:20:00Z");

    const lista = await pobierzZadania();
    expect(lista.elementy).toHaveLength(5);

    const wyslane = await przeslij(2);
    expect(wyslane).toMatchObject({
      status: "PRZESLANE",
      version: 1,
      nastepne_odpytanie_ms: 1000,
      transkrypcja: null,
      ocena: null,
      weryfikacja: null,
      szkic_transkrypcji: null,
      przypisanie_id: null,
      liczba_przeslan: 1,
      liczba_ponowien_rozpoznania: 0,
    });
    expect(wyslane.zdjecia).toHaveLength(1);

    await minelo(1);
    const rozpoznawane = await pobierzRozwiazanie(wyslane.id);
    expect(rozpoznawane).toMatchObject({
      status: "ROZPOZNAWANIE",
      version: 2,
      nastepne_odpytanie_ms: 1000,
    });

    await minelo(2998);
    expect((await pobierzRozwiazanie(wyslane.id)).status).toBe("ROZPOZNAWANIE");
    await minelo(1);
    const gotowe = await pobierzRozwiazanie(wyslane.id);
    expect(gotowe).toMatchObject({
      status: "TRANSKRYPCJA_GOTOWA",
      version: 3,
      nastepne_odpytanie_ms: null,
      ocena: null,
    });

    const tr = gotowe.transkrypcja!;
    const zatwierdzone = await zatwierdzRozwiazanie(
      wyslane.id,
      { transkrypcja_id: tr.id, wersja: tr.wersja },
      gotowe.version,
    );
    expect(zatwierdzone).toMatchObject({
      status: "ZATWIERDZONE",
      version: 4,
      nastepne_odpytanie_ms: 1000,
      ocena: null,
    });
    expect(zatwierdzone.transkrypcja).toEqual(tr);

    await minelo(1);
    expect((await pobierzRozwiazanie(wyslane.id)).status).toBe("OCENIANIE");
    await minelo(1998);
    expect((await pobierzRozwiazanie(wyslane.id)).status).toBe("OCENIANIE");
    await minelo(1);
    const ocenione = await pobierzRozwiazanie(wyslane.id);
    expect(ocenione).toMatchObject({ status: "OCENIONE", version: 6, nastepne_odpytanie_ms: null });
    expect(ocenione.ocena).not.toBeNull();
    expect(await pobierzOcene(wyslane.id)).toEqual(ocenione.ocena);
  });

  it("zdjęcie: adres wskazuje statyczny plik /mock/kartka-przyklad.jpg", async () => {
    await zalogujJako();
    const r = await przeslij(1);
    const adres = await pobierzAdresZdjecia(r.id, r.zdjecia[0]!.id);
    expect(adres.url).toBe("/mock/kartka-przyklad.jpg");
    expect(Date.parse(adres.wygasa_at)).toBe(Date.parse("2026-11-02T10:10:00Z"));
    const obcy = await przechwyc(pobierzAdresZdjecia(r.id, "e1000000-0000-4000-8000-0000000000ff"));
    expect(obcy.kod).toBe("NIE_ZNALEZIONO");
  });
});

describe("dane Transkrypcji (7.2) i Oceny (7.3) dla każdego Zadania", () => {
  const kroki = {
    1: [
      ["Obliczam odciętą wierzchołka", String.raw`p=\frac{-b}{2a}=\frac{-4}{2\cdot(-1)}=2`, 0.97],
      ["Obliczam wartość funkcji w wierzchołku", String.raw`f(2)=-2^2+4\cdot 2-3=1`, 0.95],
      ["Odpowiedź", String.raw`W=(2,1),\ f_{\max}=1`, 0.93],
    ],
    2: [
      ["Obliczam wyróżnik", String.raw`\Delta=(-5)^2-4\cdot 1\cdot 6=1`, 0.96],
      ["Obliczam pierwiastki", String.raw`x_1=\frac{5-1}{2}=2,\quad x_2=\frac{5+1}{2}=4`, 0.91],
    ],
    3: [
      ["Oznaczam cenę przed obniżką jako x", String.raw`0{,}8x=640`, 0.95],
      ["Obliczam x", String.raw`x=\frac{640}{0{,}8}=800`, 0.96],
      ["Odpowiedź", String.raw`800\ \text{zł}`, 0.9],
    ],
    4: [
      [
        "Korzystam z jedynki trygonometrycznej",
        String.raw`\cos^2\alpha=1-\sin^2\alpha=1-\frac{9}{25}=\frac{16}{25}`,
        0.94,
      ],
      ["Kąt jest ostry, więc cosinus jest dodatni", String.raw`\cos\alpha=\frac{4}{5}`, 0.97],
      [
        "Obliczam tangens",
        String.raw`\operatorname{tg}\alpha=\frac{\cos\alpha}{\sin\alpha}=\frac{4}{5}:\frac{3}{5}=\frac{4}{3}`,
        0.88,
      ],
    ],
    5: [
      ["Obliczam przeciwprostokątną", String.raw`c=6+8=14`, 0.95],
      ["Obliczam promień okręgu opisanego", String.raw`R=\frac{c}{2}=7`, 0.94],
    ],
  } as const;
  const oceny = {
    1: { punkty: 2, max: 2, k: [1, 1], krok: null, typ: null, wsk: null },
    2: {
      punkty: 1,
      max: 2,
      k: [1, 0],
      krok: 2,
      typ: "RACHUNKOWY",
      wsk: "Sprawdź jeszcze raz krok 2 — policz ponownie drugi pierwiastek.",
    },
    3: { punkty: 2, max: 2, k: [1, 1], krok: null, typ: null, wsk: null },
    4: {
      punkty: 2,
      max: 3,
      k: [1, 1, 0],
      krok: 3,
      typ: "MERYTORYCZNY",
      wsk: "Sprawdź jeszcze raz krok 3 — przypomnij sobie, jak definiuje się tangens kąta.",
    },
    5: {
      punkty: 0,
      max: 2,
      k: [0, 0],
      krok: 1,
      typ: "MERYTORYCZNY",
      wsk: "Sprawdź jeszcze raz krok 1 — jakie twierdzenie łączy długości boków trójkąta prostokątnego?",
    },
  } as const;

  it.each([1, 2, 3, 4, 5] as const)("Zadanie %i", async (n) => {
    await zalogujJako();
    const wyslane = await przeslij(n);
    await minelo(3000);
    const gotowe = await pobierzRozwiazanie(wyslane.id);
    const tr = gotowe.transkrypcja!;
    expect(tr).toMatchObject({
      wersja: 1,
      zrodlo: "AGENT_ROZPOZNAJACY",
      przeslanie_nr: 1,
      zgodnosc_z_zadaniem: 0.95,
    });
    expect(tr.kroki.map((k) => [k.tekst, k.latex, k.pewnosc_odczytu])).toEqual(kroki[n]);
    for (const [i, k] of tr.kroki.entries()) {
      expect(k).toMatchObject({
        nr: i + 1,
        niepewne_fragmenty: [],
        zmieniony_przez_ucznia: false,
        nr_kroku_agenta: null,
        wymaga_sprawdzenia: false,
        podejrzenie_danych_osobowych: false,
        tylko_dane_osobowe: false,
        zastapienia_danych_osobowych: [],
      });
    }

    await zatwierdzRozwiazanie(wyslane.id, { transkrypcja_id: tr.id, wersja: 1 }, gotowe.version);
    await minelo(2000);
    const ocena = (await pobierzRozwiazanie(wyslane.id)).ocena!;
    const o = oceny[n];
    expect(ocena).toMatchObject({
      wersja: 1,
      status: "OSTATECZNA",
      zrodlo: "AUTOMATYCZNA",
      widocznosc_dla_ucznia: "PELNA",
      punkty: o.punkty,
      punkty_max: o.max,
      wariant: 1,
      pierwszy_bledny_krok: o.krok,
      brakujacy_krok: false,
      typ_bledu: o.typ,
      krok_podejrzany_odczyt: null,
      mozliwy_blad_odczytu: false,
      wskazowka: o.wsk,
      wskazowka_w_przygotowaniu: false,
      pewnosc_oceny: 0.995,
      komunikat: null,
      mozliwe_zgloszenie_do: null,
    });
    expect(ocena.kryteria.map((k) => k.punkty)).toEqual(o.k);
    expect(ocena.kryteria.every((k) => k.punkty_max === 1 && k.prawdopodobienstwo === 0.995)).toBe(
      true,
    );
    expect(ocena.uzasadnienie.startsWith("Kryteria: K1 „")).toBe(true);
    expect(ocena.uzasadnienie.endsWith(`Razem ${o.punkty}/${o.max} pkt.`)).toBe(true);
  });

  it("Zadanie 2 jako drugie Rozwiązanie równa się przykładowi 7.5 (poza created_at)", async () => {
    await zalogujJako();
    await rozwiaz(1);
    const wyslane = await przeslij(2);
    expect(wyslane.id).toBe("d1000000-0000-4000-8000-000000000011");
    await minelo(3000);
    const gotowe = await pobierzRozwiazanie(wyslane.id);
    expect({ ...gotowe.transkrypcja!, created_at: "" }).toEqual({
      ...transkrypcjaPrzyklad,
      created_at: "",
    });
    await zatwierdzRozwiazanie(
      wyslane.id,
      { transkrypcja_id: gotowe.transkrypcja!.id, wersja: 1 },
      gotowe.version,
    );
    await minelo(2000);
    const ocenione = await pobierzRozwiazanie(wyslane.id);
    expect(ocenione.version).toBe(6);
    expect({ ...ocenione.ocena!, created_at: "" }).toEqual({ ...ocenaPrzyklad, created_at: "" });
    expect(ocenione.element_sciezki_id).toBe("e5000000-0000-4000-8000-000000000011");
    expect(ocenione.zdjecia).toEqual([
      {
        id: "e1000000-0000-4000-8000-000000000011",
        przeslanie_nr: 1,
        kolejnosc: 1,
        dostepne: true,
      },
    ]);
  });
});

describe("scenariusze i reguły stanu", () => {
  it("rozpoznanie-nieczytelne: po 3 s BLAD_ROZPOZNAWANIA, a ponowne przesłanie jest dozwolone", async () => {
    await zalogujJako();
    const r = await przeslij(2, "rozpoznanie-nieczytelne");
    await minelo(3000);
    const po = await pobierzRozwiazanie(r.id);
    expect(po).toMatchObject({
      status: "BLAD_ROZPOZNAWANIA",
      kod_bledu_rozpoznawania: "NIECZYTELNE",
      transkrypcja: null,
      ocena: null,
      version: 3,
      nastepne_odpytanie_ms: null,
    });
    const ponowne = await przeslij(2);
    expect(ponowne.id).not.toBe(r.id);
  });

  it("nieznany scenariusz → 422 WALIDACJA z polem X-Lemat-Mock-Scenariusz", async () => {
    await zalogujJako();
    const f = new FormData();
    f.append("zadanie_wersja_id", WERSJA(1));
    f.append("zdjecia", jpeg(), "kartka-1.jpg");
    const odp = await fetch(`${BAZA}/rozwiazania`, {
      method: "POST",
      body: f,
      ...zNaglowkami({ "X-Lemat-Mock-Scenariusz": "cos-innego" }),
    });
    const cialo = await odp.json();
    expect(odp.status).toBe(422);
    expect(cialo.kod).toBe("WALIDACJA");
    expect(cialo.bledy_pol).toEqual([
      {
        pole: "X-Lemat-Mock-Scenariusz",
        kod_reguly: "MOCK_SCENARIUSZ_NIEZNANY",
        komunikat: expect.any(String),
      },
    ]);
  });

  it("409 dla drugiego nieukończonego Rozwiązania tego Zadania, po OCENIONE znów można", async () => {
    await zalogujJako();
    const pierwsze = await przeslij(3);
    const e = await przechwyc(przeslij(3));
    expect(e).toMatchObject({ status: 409, kod: "ROZWIAZANIE_NIEPRAWIDLOWY_STAN" });
    expect(e.szczegoly).toEqual({ istniejace_rozwiazanie_id: pierwsze.id, status: "PRZESLANE" });
    // inne Zadanie nie jest blokowane
    await przeslij(5);
    await minelo(3000);
    const gotowe = await pobierzRozwiazanie(pierwsze.id);
    await zatwierdzRozwiazanie(
      pierwsze.id,
      { transkrypcja_id: gotowe.transkrypcja!.id, wersja: 1 },
      gotowe.version,
    );
    await minelo(2000);
    expect((await pobierzRozwiazanie(pierwsze.id)).status).toBe("OCENIONE");
    expect((await przeslij(3)).id).not.toBe(pierwsze.id);
  });

  it("zatwierdzenie: 428 bez If-Match, 400 zła forma, 412 stara wersja, 409 ponowne i 409 nieaktualna Transkrypcja", async () => {
    await zalogujJako();
    const r = await przeslij(1);
    await minelo(3000);
    const gotowe = await pobierzRozwiazanie(r.id);
    const tr = gotowe.transkrypcja!;
    const adres = `${BAZA}/rozwiazania/${r.id}/zatwierdzenie`;
    const cialo = JSON.stringify({ transkrypcja_id: tr.id, wersja: 1 });
    const wyslij = (naglowki: Record<string, string>, tresc = cialo) =>
      fetch(adres, {
        method: "POST",
        body: tresc,
        ...zNaglowkami({ "Content-Type": "application/json", ...naglowki }),
      });

    const brak = await wyslij({});
    expect(brak.status).toBe(428);
    expect((await brak.json()).kod).toBe("WYMAGANY_IF_MATCH");
    const zla = await wyslij({ "If-Match": "3" });
    expect(zla.status).toBe(400);
    expect((await zla.json()).kod).toBe("NAGLOWEK_NIEPRAWIDLOWY");
    const stara = await wyslij({ "If-Match": '"1"' });
    expect(stara.status).toBe(412);
    const problem412 = await stara.json();
    expect(problem412.kod).toBe("KONFLIKT_WERSJI");
    expect(problem412.szczegoly.aktualny.version).toBe(3);
    const inna = await wyslij(
      { "If-Match": '"3"' },
      JSON.stringify({ transkrypcja_id: tr.id, wersja: 2 }),
    );
    expect(inna.status).toBe(409);
    expect((await inna.json()).kod).toBe("TRANSKRYPCJA_NIEAKTUALNA");

    await zatwierdzRozwiazanie(r.id, { transkrypcja_id: tr.id, wersja: 1 }, 3);
    const ponownie = await przechwyc(
      zatwierdzRozwiazanie(r.id, { transkrypcja_id: tr.id, wersja: 1 }, 3),
    );
    expect(ponownie).toMatchObject({ status: 409, kod: "ROZWIAZANIE_NIEPRAWIDLOWY_STAN" });
    expect(ponownie.szczegoly).toEqual({ status: "ZATWIERDZONE" });
  });

  it("walidacje zdjęć 7.4: 0 plików, plik poza polem, PNG, nieczytelny JPEG, zły zadanie_wersja_id", async () => {
    await zalogujJako();
    const wyslij = (f: FormData) =>
      fetch(`${BAZA}/rozwiazania`, { method: "POST", body: f, ...zNaglowkami() });
    const forma = (mod: (f: FormData) => void) => {
      const f = new FormData();
      f.append("zadanie_wersja_id", WERSJA(1));
      mod(f);
      return f;
    };

    const zero = await wyslij(forma(() => {}));
    expect(zero.status).toBe(422);
    expect((await zero.json()).kod).toBe("ZA_DUZO_ZDJEC");

    const szesc = await wyslij(
      forma((f) => Array.from({ length: 6 }, () => f.append("zdjecia", jpeg(), "k.jpg"))),
    );
    expect((await szesc.json()).kod).toBe("ZA_DUZO_ZDJEC");

    const poza = await wyslij(forma((f) => f.append("plik", jpeg(), "k.jpg")));
    const pozaCialo = await poza.json();
    expect(poza.status).toBe(422);
    expect(pozaCialo.bledy_pol[0]).toMatchObject({
      pole: "plik",
      kod_reguly: "PLIK_POZA_POLEM_ZDJECIA",
    });

    const png = await wyslij(
      forma((f) =>
        f.append(
          "zdjecia",
          new Blob([new Uint8Array([0x89, 0x50, 0x4e, 0x47])], { type: "image/png" }),
          "k.png",
        ),
      ),
    );
    expect(png.status).toBe(415);
    expect((await png.json()).kod).toBe("NIEOBSLUGIWANY_FORMAT");

    const uciety = await wyslij(
      forma((f) =>
        f.append("zdjecia", new Blob([new Uint8Array([0xff, 0xd8, 0xff, 0xe0, 0, 2])]), "k.jpg"),
      ),
    );
    expect(uciety.status).toBe(422);
    expect((await uciety.json()).kod).toBe("ZDJECIE_NIEPRAWIDLOWE");

    const brakId = new FormData();
    brakId.append("zdjecia", jpeg(), "k.jpg");
    const bezId = await wyslij(brakId);
    expect((await bezId.json()).bledy_pol[0]).toMatchObject({
      pole: "zadanie_wersja_id",
      kod_reguly: "WYMAGANE",
    });

    const nieznane = await przechwyc(
      przeslijRozwiazanie({
        zadanie_wersja_id: "b1000000-0000-4000-8000-0000000000ff",
        zdjecia: [jpeg()],
      }),
    );
    expect(nieznane).toMatchObject({ status: 404, kod: "NIE_ZNALEZIONO" });
  });

  it("cudze Rozwiązanie, nie-UUID i Ocena przed czasem → 404 NIE_ZNALEZIONO", async () => {
    await zalogujJako("uczen1@lemat.local");
    const r = await przeslij(1);
    expect((await przechwyc(pobierzOcene(r.id))).kod).toBe("NIE_ZNALEZIONO");
    expect((await przechwyc(pobierzRozwiazanie("nie-uuid"))).status).toBe(404);
    expect((await przechwyc(pobierzZadanie("nie-uuid"))).status).toBe(404);
    await zalogujJako("uczen2@lemat.local");
    expect((await przechwyc(pobierzRozwiazanie(r.id))).status).toBe(404);
    expect((await pobierzRozwiazania()).elementy).toEqual([]);
  });

  it("lista Rozwiązań: sort created_at malejąco, filtr status, nieznany status → 422", async () => {
    await zalogujJako();
    const a = await przeslij(1);
    vi.setSystemTime(new Date("2026-11-02T10:06:00Z"));
    const b = await przeslij(2);
    await minelo(1);
    const wszystkie = await pobierzRozwiazania();
    expect(wszystkie.elementy.map((r) => r.id)).toEqual([b.id, a.id]);
    expect((await pobierzRozwiazania({ status: "ROZPOZNAWANIE" })).elementy).toHaveLength(2);
    expect((await pobierzRozwiazania({ status: "OCENIONE" })).elementy).toEqual([]);
    const e = await przechwyc(pobierzRozwiazania({ status: "ZLY" as never }));
    expect(e.bledyPol[0]).toMatchObject({ pole: "status", kod_reguly: "TYP_NIEPRAWIDLOWY" });
  });
});

describe("Zadania i Działy (4.2, 5, 6.3)", () => {
  it("GET /dzialy równa się 4.2", async () => {
    await zalogujJako();
    expect(await pobierzDzialy()).toEqual(dzialyOczekiwane);
  });

  it("GET /zadania równa się odpowiedzi z 6.3", async () => {
    await zalogujJako();
    expect(await pobierzZadania()).toEqual(zadaniaOczekiwane);
  });

  it("filtr dzial_id, nieistniejący Dział i paginacja kursorowa", async () => {
    await zalogujJako();
    const funkcje = await pobierzZadania({ dzial_id: 5 });
    expect(funkcje.elementy.map((z) => z.id)).toEqual([ZADANIE(1)]);
    expect(funkcje.nastepny_kursor).toBeNull();
    expect((await pobierzZadania({ dzial_id: 99 })).elementy).toEqual([]);

    const s1 = await pobierzZadania({ limit: 2 });
    expect(s1.elementy.map((z) => z.id)).toEqual([ZADANIE(2), ZADANIE(5)]);
    expect(s1.nastepny_kursor).not.toBeNull();
    const s2 = await pobierzZadania({ limit: 2, kursor: s1.nastepny_kursor! });
    expect(s2.elementy.map((z) => z.id)).toEqual([ZADANIE(1), ZADANIE(3)]);
    const s3 = await pobierzZadania({ limit: 2, kursor: s2.nastepny_kursor! });
    expect(s3.elementy.map((z) => z.id)).toEqual([ZADANIE(4)]);
    expect(s3.nastepny_kursor).toBeNull();

    const zly = await przechwyc(pobierzZadania({ kursor: "!!!" }));
    expect(zly).toMatchObject({ status: 400, kod: "KURSOR_NIEPRAWIDLOWY" });
    expect((await pobierzZadanie(ZADANIE(4))).poziom_trudnosci).toBe(3);
  });
});

describe("Uczeń: PATCH z If-Match (4.1)", () => {
  it("merge-patch zwiększa version i zwraca nowe wartości; null czyści termin i Dział", async () => {
    await zalogujJako();
    const przed = await pobierzUcznia();
    expect(przed).toMatchObject({
      imie: "Ala",
      nazwisko: "Kowalska",
      poziom_matury: null,
      version: 1,
      klasy: [],
      test_diagnostyczny: "BRAK",
    });
    const po = await zmienUcznia(
      { poziom_matury: "PODSTAWOWY", termin_egzaminu: "2027-05-05", wybrany_dzial_id: 5 },
      przed.version,
    );
    expect(po).toMatchObject({
      poziom_matury: "PODSTAWOWY",
      termin_egzaminu: "2027-05-05",
      wybrany_dzial_id: 5,
      version: 2,
    });
    const czyste = await zmienUcznia({ termin_egzaminu: null, wybrany_dzial_id: null }, 2);
    expect(czyste).toMatchObject({
      poziom_matury: "PODSTAWOWY",
      termin_egzaminu: null,
      wybrany_dzial_id: null,
      version: 3,
    });
  });

  it("412 niesie szczegoly.aktualny, 428 bez nagłówka, 422 na polach", async () => {
    await zalogujJako();
    const e412 = await przechwyc(zmienUcznia({ zgoda_na_trening: true }, 7));
    expect(e412).toMatchObject({ status: 412, kod: "KONFLIKT_WERSJI" });
    expect(e412.aktualny).toMatchObject({ version: 1 });

    const bezNaglowka = await fetch(`${BAZA}/uczniowie/ja`, {
      method: "PATCH",
      body: "{}",
      ...zNaglowkami({ "Content-Type": "application/merge-patch+json" }),
    });
    expect(bezNaglowka.status).toBe(428);
    const zlyTyp = await fetch(`${BAZA}/uczniowie/ja`, {
      method: "PATCH",
      body: "{}",
      ...zNaglowkami({ "Content-Type": "application/json", "If-Match": '"1"' }),
    });
    expect(zlyTyp.status).toBe(415);

    const e422 = await przechwyc(
      zmienUcznia(
        {
          poziom_matury: null as never,
          wybrany_dzial_id: 99,
          termin_egzaminu: "jutro",
          nieznane: 1,
        } as never,
        1,
      ),
    );
    expect(e422.status).toBe(422);
    expect(e422.bledyPol.map((b) => [b.pole, b.kod_reguly]).sort()).toEqual(
      [
        ["nieznane", "POLE_NIEZNANE"],
        ["poziom_matury", "NIE_MOZNA_WYCZYSCIC"],
        ["termin_egzaminu", "TYP_NIEPRAWIDLOWY"],
        ["wybrany_dzial_id", "DZIAL_NIEZNANY"],
      ].sort(),
    );
  });
});

describe("konta i sesje: błędy w kształcie 1.1", () => {
  const poprawna = {
    email: "nowa@example.com",
    haslo: "BardzoTajne#2026",
    imie: "Ola",
    nazwisko: "W",
    rola: "UCZEN" as const,
    oswiadczenie_wieku_16: true,
    zgoda_regulamin: true,
    kod_zaproszenia: null,
  };

  it("EMAIL_ZAJETY (bez względu na wielkość liter) i WALIDACJA z bledy_pol", async () => {
    const zajety = await przechwyc(zarejestruj({ ...poprawna, email: "UCZEN1@lemat.local" }));
    expect(zajety).toMatchObject({ status: 409, kod: "EMAIL_ZAJETY" });
    expect(zajety.problem).toMatchObject({
      type: "https://lemat.pl/bledy/email-zajety",
      title: "Nie udało się założyć konta",
      instance: "/api/v1/auth/rejestracja",
      szczegoly: null,
    });
    expect(zajety.problem?.trace_id).toMatch(/^[0-9a-f]{32}$/);

    const zle = await przechwyc(
      zarejestruj({
        ...poprawna,
        haslo: "krotkie",
        oswiadczenie_wieku_16: false,
        rola: "NAUCZYCIEL",
      }),
    );
    expect(zle).toMatchObject({ status: 422, kod: "WALIDACJA" });
    expect(zle.problem?.szczegoly).toBeNull();
    expect(zle.bledyPol).toEqual([
      {
        pole: "haslo",
        kod_reguly: "ZA_KROTKIE",
        komunikat: "Hasło musi mieć co najmniej 10 znaków.",
      },
      { pole: "rola", kod_reguly: "ROLA_NIEOBSLUGIWANA", komunikat: expect.any(String) },
      {
        pole: "oswiadczenie_wieku_16",
        kod_reguly: "WYMAGANE_OSWIADCZENIE",
        komunikat: "Wymagane oświadczenie o ukończeniu 16 lat.",
      },
    ]);
  });

  it("logowanie: zły e-mail i złe hasło dają ten sam komunikat; brak tokenu i zły token → 401", async () => {
    const a = await przechwyc(zaloguj({ email: "nikt@lemat.local", haslo: HASLO_DEMO }));
    const b = await przechwyc(zaloguj({ email: "uczen1@lemat.local", haslo: "zle" }));
    expect(a).toMatchObject({ status: 401, kod: "NIEUWIERZYTELNIONY" });
    expect(b.message).toBe(a.message);
    expect(a.message).toBe("Nieprawidłowy e-mail lub hasło.");
    expect((await przechwyc(pobierzUcznia())).kod).toBe("NIEUWIERZYTELNIONY");
    token = "wymyslony";
    expect((await przechwyc(pobierzUcznia())).kod).toBe("NIEUWIERZYTELNIONY");
  });

  it("token dostępu wygasa po 15 minutach", async () => {
    await zalogujJako();
    await pobierzUcznia();
    vi.setSystemTime(new Date("2026-11-02T10:20:01Z"));
    expect((await przechwyc(pobierzUcznia())).status).toBe(401);
  });

  it("odświeżenie rotuje sesję, a po wylogowaniu zwraca SESJA_UNIEWAZNIONA", async () => {
    expect((await przechwyc(odswiez())).kod).toBe("SESJA_UNIEWAZNIONA");
    await zalogujJako();
    const nowa = await odswiez();
    expect(nowa.uzytkownik.id).toBe("c1000000-0000-4000-8000-000000000001");
    token = nowa.access_token;
    await wyloguj();
    expect((await przechwyc(odswiez())).kod).toBe("SESJA_UNIEWAZNIONA");
    expect((await przechwyc(pobierzUcznia())).status).toBe(401);
  });

  it("metoda niedozwolona → 405, nieznana ścieżka → 404, zdrowie bez tokenu", async () => {
    await zalogujJako();
    const z405 = await fetch(`${BAZA}/dzialy`, { method: "DELETE", ...zNaglowkami() });
    expect(z405.status).toBe(405);
    expect((await z405.json()).kod).toBe("METODA_NIEDOZWOLONA");
    const z404 = await fetch(`${BAZA}/nie-ma-takiego`, zNaglowkami());
    expect(z404.status).toBe(404);
    expect(z404.headers.get("content-type")).toContain("application/problem+json");
    const zdrowie = await fetch(`${BAZA}/zdrowie`);
    expect(await zdrowie.json()).toEqual({ status: "OK" });
  });
});

describe("profil uproszczony (4.3)", () => {
  it("nowy Uczeń: zera, null i puste listy", async () => {
    await zalogujJako();
    expect(await pobierzProfilUproszczony()).toEqual({
      uczen_id: "c1000000-0000-4000-8000-000000000001",
      liczba_rozwiazan: 0,
      liczba_ocenionych: 0,
      suma_punktow: 0,
      suma_punktow_max: 0,
      sredni_procent_punktow: null,
      dzialy: [],
      ostatnie_oceny: [],
    });
  });

  it("po sekwencji Zadań 1 (2/2), 2 (1/2), 5 (0/2) i Zadaniu 3 w toku równa się przykładowi", async () => {
    await zalogujJako();
    // Oceny powstają dokładnie o 10:03:00, 10:08:00 i 10:12:00 (5 s od przesłania do Oceny).
    for (const [n, ocenaO] of [
      [1, "2026-11-02T10:03:00Z"],
      [2, "2026-11-02T10:08:00Z"],
      [5, "2026-11-02T10:12:00Z"],
    ] as const) {
      vi.setSystemTime(new Date(Date.parse(ocenaO) - 5000));
      await rozwiaz(n);
    }
    await przeslij(3);
    await minelo(10);
    expect(await pobierzProfilUproszczony()).toEqual(profilOczekiwany);
  });

  it("dane innego Ucznia nie wpływają na wynik", async () => {
    await zalogujJako("uczen2@lemat.local");
    await rozwiaz(1);
    await zalogujJako("uczen1@lemat.local");
    const profil = await pobierzProfilUproszczony();
    expect(profil.liczba_rozwiazan).toBe(0);
    expect(profil.dzialy).toEqual([]);
  });
});
