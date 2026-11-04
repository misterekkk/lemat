// @Role: Dane mocka: kroki Transkrypcji per Zadanie (POC_CONTRACT 7.2) | @DependsOn: lib/api/types | @UsedBy: mocks/db | @Task: TASK-132
import type { KrokTranskrypcjiWyjscie } from "@/lib/api/types";

interface KrokDanych {
  tekst: string;
  latex: string;
  pewnosc_odczytu: number;
}

/** Klucz: numer Zadania (1..5) z końcówki identyfikatora a1000000-…-00000000000N. */
export const KROKI_PER_ZADANIE: Record<number, KrokDanych[]> = {
  1: [
    {
      tekst: "Obliczam odciętą wierzchołka",
      latex: String.raw`p=\frac{-b}{2a}=\frac{-4}{2\cdot(-1)}=2`,
      pewnosc_odczytu: 0.97,
    },
    {
      tekst: "Obliczam wartość funkcji w wierzchołku",
      latex: String.raw`f(2)=-2^2+4\cdot 2-3=1`,
      pewnosc_odczytu: 0.95,
    },
    { tekst: "Odpowiedź", latex: String.raw`W=(2,1),\ f_{\max}=1`, pewnosc_odczytu: 0.93 },
  ],
  2: [
    {
      tekst: "Obliczam wyróżnik",
      latex: String.raw`\Delta=(-5)^2-4\cdot 1\cdot 6=1`,
      pewnosc_odczytu: 0.96,
    },
    {
      tekst: "Obliczam pierwiastki",
      latex: String.raw`x_1=\frac{5-1}{2}=2,\quad x_2=\frac{5+1}{2}=4`,
      pewnosc_odczytu: 0.91,
    },
  ],
  3: [
    {
      tekst: "Oznaczam cenę przed obniżką jako x",
      latex: String.raw`0{,}8x=640`,
      pewnosc_odczytu: 0.95,
    },
    { tekst: "Obliczam x", latex: String.raw`x=\frac{640}{0{,}8}=800`, pewnosc_odczytu: 0.96 },
    { tekst: "Odpowiedź", latex: String.raw`800\ \text{zł}`, pewnosc_odczytu: 0.9 },
  ],
  4: [
    {
      tekst: "Korzystam z jedynki trygonometrycznej",
      latex: String.raw`\cos^2\alpha=1-\sin^2\alpha=1-\frac{9}{25}=\frac{16}{25}`,
      pewnosc_odczytu: 0.94,
    },
    {
      tekst: "Kąt jest ostry, więc cosinus jest dodatni",
      latex: String.raw`\cos\alpha=\frac{4}{5}`,
      pewnosc_odczytu: 0.97,
    },
    {
      tekst: "Obliczam tangens",
      latex: String.raw`\operatorname{tg}\alpha=\frac{\cos\alpha}{\sin\alpha}=\frac{4}{5}:\frac{3}{5}=\frac{4}{3}`,
      pewnosc_odczytu: 0.88,
    },
  ],
  5: [
    { tekst: "Obliczam przeciwprostokątną", latex: String.raw`c=6+8=14`, pewnosc_odczytu: 0.95 },
    {
      tekst: "Obliczam promień okręgu opisanego",
      latex: String.raw`R=\frac{c}{2}=7`,
      pewnosc_odczytu: 0.94,
    },
  ],
};

/** Pełny krok kontraktu: pola stałe wg 7.2. */
export function zbudujKrok(nr: number, dane: KrokDanych): KrokTranskrypcjiWyjscie {
  return {
    nr,
    tekst: dane.tekst,
    latex: dane.latex,
    pewnosc_odczytu: dane.pewnosc_odczytu,
    niepewne_fragmenty: [],
    zmieniony_przez_ucznia: false,
    nr_kroku_agenta: null,
    wymaga_sprawdzenia: false,
    podejrzenie_danych_osobowych: false,
    tylko_dane_osobowe: false,
    zastapienia_danych_osobowych: [],
  };
}
