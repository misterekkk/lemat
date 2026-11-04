// @Role: Dane mocka: 5 Zadań w kolejności GET /zadania (POC_CONTRACT 6.3) | @DependsOn: lib/api/types | @UsedBy: mocks/handlers, mocks/db | @Task: TASK-132
import type { ZadanieUczniaWyjscie } from "@/lib/api/types";

/** Kolejność: poziom_trudnosci rosnąco, potem id (jak w odpowiedzi kontraktu 6.3). */
export const ZADANIA = [
  {
    id: "a1000000-0000-4000-8000-000000000002",
    zadanie_wersja_id: "b1000000-0000-4000-8000-000000000002",
    rodzaj: "OTWARTE_OBLICZENIOWE",
    forma_zamknieta: null,
    tresc_md: "Rozwiąż równanie $x^2 - 5x + 6 = 0$.",
    zalaczniki: [],
    opcje: null,
    stwierdzenia: null,
    punkty_max: 2,
    poziom_trudnosci: 1,
    dzial: {
      id: 3,
      nazwa: "Równania i nierówności",
    },
    umiejetnosci: [
      {
        id: 301,
        nazwa: "Rozwiązywanie równań kwadratowych",
        glowna: true,
      },
    ],
  },
  {
    id: "a1000000-0000-4000-8000-000000000005",
    zadanie_wersja_id: "b1000000-0000-4000-8000-000000000005",
    rodzaj: "OTWARTE_OBLICZENIOWE",
    forma_zamknieta: null,
    tresc_md:
      "W trójkącie prostokątnym przyprostokątne mają długości $6$ i $8$. Oblicz długość promienia okręgu opisanego na tym trójkącie.",
    zalaczniki: [],
    opcje: null,
    stwierdzenia: null,
    punkty_max: 2,
    poziom_trudnosci: 1,
    dzial: {
      id: 8,
      nazwa: "Planimetria",
    },
    umiejetnosci: [
      {
        id: 801,
        nazwa: "Obliczanie długości boków i promienia okręgu opisanego w trójkącie prostokątnym",
        glowna: true,
      },
    ],
  },
  {
    id: "a1000000-0000-4000-8000-000000000001",
    zadanie_wersja_id: "b1000000-0000-4000-8000-000000000001",
    rodzaj: "OTWARTE_OBLICZENIOWE",
    forma_zamknieta: null,
    tresc_md:
      "Dana jest funkcja kwadratowa $f(x) = -x^2 + 4x - 3$. Oblicz współrzędne wierzchołka paraboli będącej wykresem funkcji $f$ i podaj największą wartość funkcji $f$.",
    zalaczniki: [],
    opcje: null,
    stwierdzenia: null,
    punkty_max: 2,
    poziom_trudnosci: 2,
    dzial: {
      id: 5,
      nazwa: "Funkcje",
    },
    umiejetnosci: [
      {
        id: 502,
        nazwa: "Wyznaczanie wierzchołka i wartości największej funkcji kwadratowej",
        glowna: true,
      },
    ],
  },
  {
    id: "a1000000-0000-4000-8000-000000000003",
    zadanie_wersja_id: "b1000000-0000-4000-8000-000000000003",
    rodzaj: "OTWARTE_OBLICZENIOWE",
    forma_zamknieta: null,
    tresc_md: "Cena roweru po obniżce o $20\\%$ wynosi $640$ zł. Oblicz cenę roweru przed obniżką.",
    zalaczniki: [],
    opcje: null,
    stwierdzenia: null,
    punkty_max: 2,
    poziom_trudnosci: 2,
    dzial: {
      id: 1,
      nazwa: "Liczby rzeczywiste",
    },
    umiejetnosci: [
      {
        id: 101,
        nazwa: "Obliczanie procentów i zmian procentowych",
        glowna: true,
      },
    ],
  },
  {
    id: "a1000000-0000-4000-8000-000000000004",
    zadanie_wersja_id: "b1000000-0000-4000-8000-000000000004",
    rodzaj: "OTWARTE_OBLICZENIOWE",
    forma_zamknieta: null,
    tresc_md:
      "Kąt $\\alpha$ jest ostry i $\\sin\\alpha = \\frac{3}{5}$. Oblicz $\\cos\\alpha$ oraz $\\operatorname{tg}\\alpha$.",
    zalaczniki: [],
    opcje: null,
    stwierdzenia: null,
    punkty_max: 3,
    poziom_trudnosci: 3,
    dzial: {
      id: 7,
      nazwa: "Trygonometria",
    },
    umiejetnosci: [
      {
        id: 701,
        nazwa: "Obliczanie wartości funkcji trygonometrycznych kąta ostrego",
        glowna: true,
      },
    ],
  },
] as ZadanieUczniaWyjscie[];
