// @Role: Dane mocka: Kryteria (6.3) i wyniki Oceny per Zadanie (7.3) oraz szablon uzasadnienia | @DependsOn: lib/api/types | @UsedBy: mocks/db | @Task: TASK-132
import type { TypBledu } from "@/lib/api/types";

export interface KryteriumDanych {
  nr: number;
  opis: string;
}
export interface OcenaDanych {
  /** Punkty Kryteriów K1, K2, (K3) — każde Kryterium ma punkty_max = 1. */
  punktyKryteriow: number[];
  pierwszyBlednyKrok: number | null;
  typBledu: TypBledu | null;
  wskazowka: string | null;
}

/** Opisy Kryteriów z tabeli 6.3 (po zdekodowaniu JSON: pojedynczy `\`). */
export const KRYTERIA_PER_ZADANIE: Record<number, KryteriumDanych[]> = {
  1: [
    { nr: 1, opis: "Obliczenie odciętej wierzchołka paraboli: $p = 2$." },
    { nr: 2, opis: "Podanie największej wartości funkcji: $f(2) = 1$." },
  ],
  2: [
    { nr: 1, opis: "Obliczenie wyróżnika trójmianu: $\\Delta = 1$." },
    { nr: 2, opis: "Obliczenie pierwiastków równania: $x_1 = 2$, $x_2 = 3$." },
  ],
  3: [
    { nr: 1, opis: "Zapisanie równania opisującego cenę przed obniżką: $0{,}8x = 640$." },
    { nr: 2, opis: "Obliczenie ceny przed obniżką: $x = 800$ zł." },
  ],
  4: [
    { nr: 1, opis: "Zastosowanie jedynki trygonometrycznej: $\\cos^2\\alpha = \\frac{16}{25}$." },
    { nr: 2, opis: "Obliczenie $\\cos\\alpha = \\frac{4}{5}$." },
    { nr: 3, opis: "Obliczenie $\\operatorname{tg}\\alpha = \\frac{3}{4}$." },
  ],
  5: [
    { nr: 1, opis: "Obliczenie długości przeciwprostokątnej: $c = 10$." },
    { nr: 2, opis: "Obliczenie promienia okręgu opisanego: $R = 5$." },
  ],
};

export const OCENY_PER_ZADANIE: Record<number, OcenaDanych> = {
  1: { punktyKryteriow: [1, 1], pierwszyBlednyKrok: null, typBledu: null, wskazowka: null },
  2: {
    punktyKryteriow: [1, 0],
    pierwszyBlednyKrok: 2,
    typBledu: "RACHUNKOWY",
    wskazowka: "Sprawdź jeszcze raz krok 2 — policz ponownie drugi pierwiastek.",
  },
  3: { punktyKryteriow: [1, 1], pierwszyBlednyKrok: null, typBledu: null, wskazowka: null },
  4: {
    punktyKryteriow: [1, 1, 0],
    pierwszyBlednyKrok: 3,
    typBledu: "MERYTORYCZNY",
    wskazowka: "Sprawdź jeszcze raz krok 3 — przypomnij sobie, jak definiuje się tangens kąta.",
  },
  5: {
    punktyKryteriow: [0, 0],
    pierwszyBlednyKrok: 1,
    typBledu: "MERYTORYCZNY",
    wskazowka:
      "Sprawdź jeszcze raz krok 1 — jakie twierdzenie łączy długości boków trójkąta prostokątnego?",
  },
};

const OPIS_BLEDU: Record<TypBledu, string> = {
  RACHUNKOWY: "błąd rachunkowy",
  MERYTORYCZNY: "błąd merytoryczny",
  NIEPELNE_ROZWIAZANIE: "niepełne rozwiązanie",
};

/** Szablon uzasadnienia z 7.3. */
export function zbudujUzasadnienie(numerZadania: number): string {
  const kryteria = KRYTERIA_PER_ZADANIE[numerZadania] ?? [];
  const dane = OCENY_PER_ZADANIE[numerZadania];
  if (!dane) return "";
  const czesci = kryteria.map((k, i) => `K${k.nr} „${k.opis}” — ${dane.punktyKryteriow[i]}/1 pkt`);
  const suma = dane.punktyKryteriow.reduce((a, b) => a + b, 0);
  const blad =
    dane.typBledu !== null
      ? ` Pierwszy błędny krok: ${dane.pierwszyBlednyKrok} (${OPIS_BLEDU[dane.typBledu]}).`
      : "";
  return `Kryteria: ${czesci.join("; ")}.${blad} Razem ${suma}/${kryteria.length} pkt.`;
}
