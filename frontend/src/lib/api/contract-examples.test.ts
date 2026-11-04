// @Role: Test typów: przykłady JSON wklejone z kontraktu POC (sekcje 3-7) kompilują się z typami lib/api/types | @DependsOn: lib/api/types, docs/POC_CONTRACT.md | @UsedBy: vitest, tsc | @Task: TASK-131
import { describe, expect, it } from "vitest";
import type {
  AdresZdjeciaWyjscie,
  DzialWyjscie,
  OcenaWyjscie,
  ProfilUproszczonyWyjscie,
  RejestracjaWyjscie,
  RozwiazanieWyjscie,
  SesjaWyjscie,
  Strona,
  UczenWyjscie,
  ZadanieUczniaWyjscie,
  ZatwierdzenieWejscie,
  ZmianaUczniaWejscie,
} from "./types";

const rejestracja = {
  status_konta: "AKTYWNE",
  sesja: {
    access_token: "eyJ.x.y",
    wygasa_at: "2026-11-02T10:15:00Z",
    uzytkownik: {
      id: "c1000000-0000-4000-8000-000000000003",
      rola: "UCZEN",
      imie: "Ola",
      flagi: { weryfikator_globalny: false, recenzent_tresci: false },
    },
  },
} satisfies RejestracjaWyjscie;

const sesja = {
  access_token: "eyJ.x.y",
  wygasa_at: "2026-11-02T10:15:00Z",
  uzytkownik: {
    id: "c1000000-0000-4000-8000-000000000001",
    rola: "UCZEN",
    imie: "Ala",
    flagi: { weryfikator_globalny: false, recenzent_tresci: false },
  },
} satisfies SesjaWyjscie;

const uczen = {
  id: "c1000000-0000-4000-8000-000000000001",
  email: "uczen1@lemat.local",
  imie: "Ala",
  nazwisko: "Kowalska",
  poziom_matury: null,
  termin_egzaminu: null,
  wybrany_dzial_id: null,
  zgoda_na_trening: false,
  test_diagnostyczny: "BRAK",
  klasy: [],
  version: 1,
} satisfies UczenWyjscie;

const zmiana = {
  poziom_matury: "PODSTAWOWY",
  termin_egzaminu: "2027-05-05",
  wybrany_dzial_id: 5,
} satisfies ZmianaUczniaWejscie;

const dzialy = [
  { id: 1, kod: "I", nazwa: "Liczby rzeczywiste", poziom_matury: "PODSTAWOWY" },
  {
    id: 13,
    kod: "XIII",
    nazwa: "Optymalizacja i rachunek różniczkowy",
    poziom_matury: "ROZSZERZONY",
  },
] satisfies DzialWyjscie[];

const profil = {
  uczen_id: "c1000000-0000-4000-8000-000000000001",
  liczba_rozwiazan: 4,
  liczba_ocenionych: 3,
  suma_punktow: 3,
  suma_punktow_max: 6,
  sredni_procent_punktow: 50.0,
  dzialy: [
    {
      dzial: { id: 3, nazwa: "Równania i nierówności" },
      liczba_ocenionych: 1,
      sredni_procent_punktow: 50.0,
    },
  ],
  ostatnie_oceny: [
    {
      rozwiazanie_id: "d1000000-0000-4000-8000-000000000012",
      zadanie_id: "a1000000-0000-4000-8000-000000000005",
      dzial: { id: 8, nazwa: "Planimetria" },
      punkty: 0,
      punkty_max: 2,
      oceniono_at: "2026-11-02T10:12:00Z",
    },
  ],
} satisfies ProfilUproszczonyWyjscie;

const zadanie = {
  id: "a1000000-0000-4000-8000-000000000004",
  zadanie_wersja_id: "b1000000-0000-4000-8000-000000000004",
  rodzaj: "OTWARTE_OBLICZENIOWE",
  forma_zamknieta: null,
  tresc_md: "Kąt $\\alpha$ jest ostry i $\\sin\\alpha = \\frac{3}{5}$.",
  zalaczniki: [],
  opcje: null,
  stwierdzenia: null,
  punkty_max: 3,
  poziom_trudnosci: 3,
  dzial: { id: 7, nazwa: "Trygonometria" },
  umiejetnosci: [
    { id: 701, nazwa: "Obliczanie wartości funkcji trygonometrycznych", glowna: true },
  ],
} satisfies ZadanieUczniaWyjscie;

const strona = {
  elementy: [zadanie],
  nastepny_kursor: null,
} satisfies Strona<ZadanieUczniaWyjscie>;

const przeslane = {
  id: "d1000000-0000-4000-8000-000000000011",
  zadanie_id: "a1000000-0000-4000-8000-000000000002",
  zadanie_wersja_id: "b1000000-0000-4000-8000-000000000002",
  status: "PRZESLANE",
  element_sciezki_id: "e5000000-0000-4000-8000-000000000011",
  przypisanie_id: null,
  zdjecia: [
    { id: "e1000000-0000-4000-8000-000000000011", przeslanie_nr: 1, kolejnosc: 1, dostepne: true },
  ],
  liczba_przeslan: 1,
  liczba_ponowien_rozpoznania: 0,
  kod_bledu_rozpoznawania: null,
  transkrypcja: null,
  ocena: null,
  weryfikacja: null,
  version: 1,
  created_at: "2026-11-02T10:05:00Z",
  updated_at: "2026-11-02T10:05:00Z",
  nastepne_odpytanie_ms: 1000,
  szkic_transkrypcji: null,
} satisfies RozwiazanieWyjscie;

const ocena = {
  id: "f2000000-0000-4000-8000-000000000011",
  wersja: 1,
  status: "OSTATECZNA",
  zrodlo: "AUTOMATYCZNA",
  widocznosc_dla_ucznia: "PELNA",
  punkty: 1,
  punkty_max: 2,
  wariant: 1,
  kryteria: [
    {
      nr_kryterium: 1,
      opis: "Obliczenie wyróżnika trójmianu.",
      punkty: 1,
      punkty_max: 1,
      prawdopodobienstwo: 0.995,
    },
    {
      nr_kryterium: 2,
      opis: "Obliczenie pierwiastków równania.",
      punkty: 0,
      punkty_max: 1,
      prawdopodobienstwo: 0.995,
    },
  ],
  pierwszy_bledny_krok: 2,
  brakujacy_krok: false,
  typ_bledu: "RACHUNKOWY",
  krok_podejrzany_odczyt: null,
  mozliwy_blad_odczytu: false,
  wskazowka: "Sprawdź jeszcze raz krok 2 — policz ponownie drugi pierwiastek.",
  wskazowka_w_przygotowaniu: false,
  uzasadnienie: "Razem 1/2 pkt.",
  pewnosc_oceny: 0.995,
  komunikat: null,
  mozliwe_zgloszenie_do: null,
  created_at: "2026-11-02T10:08:00Z",
} satisfies OcenaWyjscie;

const oceniono = {
  ...przeslane,
  status: "OCENIONE",
  transkrypcja: {
    id: "f1000000-0000-4000-8000-000000000011",
    wersja: 1,
    zrodlo: "AGENT_ROZPOZNAJACY",
    przeslanie_nr: 1,
    zgodnosc_z_zadaniem: 0.95,
    kroki: [
      {
        nr: 1,
        tekst: "Obliczam wyróżnik",
        latex: "\\Delta=(-5)^2-4\\cdot 1\\cdot 6=1",
        pewnosc_odczytu: 0.96,
        niepewne_fragmenty: [],
        zmieniony_przez_ucznia: false,
        nr_kroku_agenta: null,
        wymaga_sprawdzenia: false,
        podejrzenie_danych_osobowych: false,
        tylko_dane_osobowe: false,
        zastapienia_danych_osobowych: [],
      },
    ],
    created_at: "2026-11-02T10:05:04Z",
    zakres_poprawek: null,
    konflikt_filtra: false,
    zgodnosc_poprawek: null,
  },
  ocena,
  version: 6,
  nastepne_odpytanie_ms: null,
} satisfies RozwiazanieWyjscie;

const zatwierdzenie = {
  transkrypcja_id: "f1000000-0000-4000-8000-000000000011",
  wersja: 1,
} satisfies ZatwierdzenieWejscie;

const adres = {
  url: "/api/v1/magazyn-lokalny/MTIz.dGFr",
  wygasa_at: "2026-11-02T10:10:00Z",
} satisfies AdresZdjeciaWyjscie;

describe("przykłady kontraktu zgodne z typami", () => {
  it("kompilują się i mają oczekiwane wartości", () => {
    expect(rejestracja.sesja.uzytkownik.rola).toBe("UCZEN");
    expect(sesja.access_token).toBeTruthy();
    expect(uczen.version).toBe(1);
    expect(zmiana.wybrany_dzial_id).toBe(5);
    expect(dzialy).toHaveLength(2);
    expect(profil.liczba_ocenionych).toBe(3);
    expect(strona.elementy[0]?.punkty_max).toBe(3);
    expect(oceniono.ocena.punkty).toBe(1);
    expect(zatwierdzenie.wersja).toBe(1);
    expect(adres.url).toContain("/magazyn-lokalny/");
  });
});
