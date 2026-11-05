// @Role: Setup testów Vitest: matchery jest-dom, sprzątanie DOM i zapasowy Web Storage dla Node >= 25 (gdzie globalny localStorage przesłania jsdom) | @DependsOn: @testing-library/jest-dom | @UsedBy: vitest.config.ts | @Task: TASK-006, TASK-132
import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

// Node >= 25 definiuje własny (niedziałający bez flagi) globalny localStorage, który
// przesłania implementację jsdom — w środowisku jsdom podstawiamy prosty magazyn w pamięci.
class MagazynPamieciowy implements Storage {
  private dane = new Map<string, string>();
  get length(): number {
    return this.dane.size;
  }
  clear(): void {
    this.dane.clear();
  }
  getItem(klucz: string): string | null {
    return this.dane.get(klucz) ?? null;
  }
  key(indeks: number): string | null {
    return [...this.dane.keys()][indeks] ?? null;
  }
  removeItem(klucz: string): void {
    this.dane.delete(klucz);
  }
  setItem(klucz: string, wartosc: string): void {
    this.dane.set(klucz, String(wartosc));
  }
}

if (typeof window !== "undefined") {
  for (const nazwa of ["localStorage", "sessionStorage"] as const) {
    let dziala = false;
    try {
      dziala = typeof window[nazwa]?.getItem === "function";
    } catch {
      dziala = false;
    }
    if (!dziala) {
      Object.defineProperty(window, nazwa, { value: new MagazynPamieciowy(), configurable: true });
    }
  }
}

afterEach(() => cleanup());
