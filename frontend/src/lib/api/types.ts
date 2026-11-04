// @Role: Ręcznie utrzymywane typy kontraktu POC-1.0 (enumy jako unie literałów, rekordy wejścia/wyjścia, Strona<T>) | @DependsOn: docs/POC_CONTRACT.md 1-7 | @UsedBy: lib/api/client, mocks (TASK-132), features/* | @Task: TASK-131

// --- Enumy (UPPER_SNAKE jak w DDL) ---
export type RolaUzytkownika = "UCZEN" | "NAUCZYCIEL" | "ADMINISTRATOR";
export type StatusKonta = "AKTYWNE" | "OCZEKUJE_NA_ZATWIERDZENIE" | "ZABLOKOWANE" | "USUWANE";
export type PoziomMatury = "PODSTAWOWY" | "ROZSZERZONY";
export type StatusTestuDiagnostycznego =
  "BRAK" | "W_TOKU" | "ZAKONCZONY" | "POMINIETY" | "NIEDOSTEPNY";
export type RodzajZadania = "OTWARTE_OBLICZENIOWE" | "OTWARTE_Z_DOWODEM" | "ZAMKNIETE";
export type StatusRozwiazania =
  | "PRZESLANE"
  | "ROZPOZNAWANIE"
  | "BLAD_ROZPOZNAWANIA"
  | "TRANSKRYPCJA_GOTOWA"
  | "ZATWIERDZONE"
  | "OCENIANIE"
  | "OCZEKUJE_NA_WERYFIKACJE"
  | "OCENIONE"
  | "PORZUCONE"
  | "ANULOWANE";
export type KodBleduRozpoznawania =
  "NIECZYTELNE" | "PRZEKROCZONY_CZAS" | "BLAD_DOSTAWCY" | "ZBYT_WIELE_KROKOW";
export type ZrodloTranskrypcji = "AGENT_ROZPOZNAJACY" | "POPRAWKA_UCZNIA";
export type StatusOceny = "WSTEPNA" | "OSTATECZNA" | "ZASTAPIONA";
export type ZrodloOceny = "AUTOMATYCZNA" | "NAUCZYCIEL";
// W POC kontrakt podaje tylko PELNA (luka [B]: pozostałe wartości nieopisane).
export type WidocznoscDlaUcznia = "PELNA";
export type TypBledu = "RACHUNKOWY" | "MERYTORYCZNY" | "NIEPELNE_ROZWIAZANIE";
export type ScenariuszMock = "domyslny" | "rozpoznanie-nieczytelne";

// --- Wspólne ---
export interface Strona<T> {
  elementy: T[];
  nastepny_kursor: string | null;
}

// --- Uwierzytelnianie (3) ---
export interface RejestracjaWejscie {
  email: string;
  haslo: string;
  imie: string;
  nazwisko: string;
  rola: "UCZEN" | "NAUCZYCIEL";
  oswiadczenie_wieku_16: boolean;
  zgoda_regulamin: boolean;
  kod_zaproszenia: string | null;
}
export interface LogowanieWejscie {
  email: string;
  haslo: string;
}
export interface FlagiUzytkownika {
  weryfikator_globalny: boolean;
  recenzent_tresci: boolean;
}
export interface UzytkownikSesji {
  id: string;
  rola: RolaUzytkownika;
  imie: string;
  flagi: FlagiUzytkownika;
}
export interface SesjaWyjscie {
  access_token: string;
  wygasa_at: string;
  uzytkownik: UzytkownikSesji;
}
export interface RejestracjaWyjscie {
  status_konta: StatusKonta;
  sesja: SesjaWyjscie | null;
}

// --- Uczeń, Działy, Profil (4) ---
// W POC `klasy` jest zawsze pustą tablicą (U-21).
export interface KlasaUcznia {
  id: string;
  nazwa: string;
  nauczyciel: string;
}
export interface UczenWyjscie {
  id: string;
  email: string;
  imie: string;
  nazwisko: string;
  poziom_matury: PoziomMatury | null;
  termin_egzaminu: string | null;
  wybrany_dzial_id: number | null;
  zgoda_na_trening: boolean;
  test_diagnostyczny: StatusTestuDiagnostycznego;
  klasy: KlasaUcznia[];
  version: number;
}
/** JSON Merge Patch (RFC 7396): brak pola = bez zmiany; null tylko dla termin_egzaminu i wybrany_dzial_id. */
export interface ZmianaUczniaWejscie {
  poziom_matury?: PoziomMatury;
  termin_egzaminu?: string | null;
  wybrany_dzial_id?: number | null;
  zgoda_na_trening?: boolean;
}
export interface DzialWyjscie {
  id: number;
  kod: string;
  nazwa: string;
  poziom_matury: PoziomMatury;
}
export interface DzialSkrot {
  id: number;
  nazwa: string;
}
export interface DzialUproszczony {
  dzial: DzialSkrot;
  liczba_ocenionych: number;
  sredni_procent_punktow: number;
}
export interface OstatniaOcenaSkrot {
  rozwiazanie_id: string;
  zadanie_id: string;
  dzial: DzialSkrot;
  punkty: number;
  punkty_max: number;
  oceniono_at: string;
}
export interface ProfilUproszczonyWyjscie {
  uczen_id: string;
  liczba_rozwiazan: number;
  liczba_ocenionych: number;
  suma_punktow: number;
  suma_punktow_max: number;
  sredni_procent_punktow: number | null;
  dzialy: DzialUproszczony[];
  ostatnie_oceny: OstatniaOcenaSkrot[];
}

// --- Zadania (5) ---
export interface UmiejetnoscZadania {
  id: number;
  nazwa: string;
  glowna: boolean;
}
export interface ZadanieUczniaWyjscie {
  id: string;
  zadanie_wersja_id: string;
  rodzaj: RodzajZadania;
  forma_zamknieta: null;
  tresc_md: string;
  zalaczniki: never[];
  opcje: null;
  stwierdzenia: null;
  punkty_max: number;
  poziom_trudnosci: number;
  dzial: DzialSkrot;
  umiejetnosci: UmiejetnoscZadania[];
}
export interface ParametryZadan {
  dzial_id?: number;
  limit?: number;
  kursor?: string;
}

// --- Rozwiązania (7) ---
export interface NiepewnyFragment {
  zdjecie: number;
  ramka: [number, number, number, number];
  [pole: string]: unknown;
}
export interface ZastapienieDanychOsobowych {
  od: number;
  do: number;
  rodzaj: string;
}
export interface KrokTranskrypcjiWyjscie {
  nr: number;
  tekst: string;
  latex: string;
  pewnosc_odczytu: number | null;
  niepewne_fragmenty: NiepewnyFragment[];
  zmieniony_przez_ucznia: boolean;
  nr_kroku_agenta: number | null;
  wymaga_sprawdzenia: boolean;
  podejrzenie_danych_osobowych: boolean;
  tylko_dane_osobowe: boolean;
  zastapienia_danych_osobowych: ZastapienieDanychOsobowych[];
}
// W POC `zakres_poprawek` i `zgodnosc_poprawek` są zawsze null (U-26).
export interface TranskrypcjaWyjscie {
  id: string;
  wersja: number;
  zrodlo: ZrodloTranskrypcji;
  przeslanie_nr: number;
  zgodnosc_z_zadaniem: number | null;
  kroki: KrokTranskrypcjiWyjscie[];
  created_at: string;
  zakres_poprawek: null;
  konflikt_filtra: boolean;
  zgodnosc_poprawek: null;
}
export interface KryteriumOcenyWyjscie {
  nr_kryterium: number;
  opis: string;
  punkty: number | null;
  punkty_max: number;
  prawdopodobienstwo: number | null;
}
export interface OcenaWyjscie {
  id: string;
  wersja: number;
  status: StatusOceny;
  zrodlo: ZrodloOceny;
  widocznosc_dla_ucznia: WidocznoscDlaUcznia;
  punkty: number | null;
  punkty_max: number;
  wariant: number;
  kryteria: KryteriumOcenyWyjscie[];
  pierwszy_bledny_krok: number | null;
  brakujacy_krok: boolean;
  typ_bledu: TypBledu | null;
  krok_podejrzany_odczyt: number | null;
  mozliwy_blad_odczytu: boolean;
  wskazowka: string | null;
  wskazowka_w_przygotowaniu: boolean;
  uzasadnienie: string;
  pewnosc_oceny: number | null;
  komunikat: string | null;
  mozliwe_zgloszenie_do: string | null;
  created_at: string;
}
export interface ZdjecieRozwiazaniaWyjscie {
  id: string;
  przeslanie_nr: number;
  kolejnosc: number;
  dostepne: boolean;
}
export interface RozwiazanieWyjscie {
  id: string;
  zadanie_id: string;
  zadanie_wersja_id: string;
  status: StatusRozwiazania;
  element_sciezki_id: string | null;
  przypisanie_id: string | null;
  zdjecia: ZdjecieRozwiazaniaWyjscie[];
  liczba_przeslan: number;
  liczba_ponowien_rozpoznania: number;
  kod_bledu_rozpoznawania: KodBleduRozpoznawania | null;
  transkrypcja: TranskrypcjaWyjscie | null;
  ocena: OcenaWyjscie | null;
  /** W POC zawsze null. */
  weryfikacja: null;
  version: number;
  created_at: string;
  updated_at: string;
  nastepne_odpytanie_ms: number | null;
  /** W POC zawsze null. */
  szkic_transkrypcji: null;
}
export interface PrzeslanieRozwiazaniaWejscie {
  zadanie_wersja_id: string;
  zdjecia: Blob[];
  scenariusz?: ScenariuszMock;
}
export interface ParametryRozwiazan {
  status?: StatusRozwiazania;
  limit?: number;
  kursor?: string;
}
export interface ZatwierdzenieWejscie {
  transkrypcja_id: string;
  wersja: number;
}
export interface AdresZdjeciaWyjscie {
  url: string;
  wygasa_at: string;
}
