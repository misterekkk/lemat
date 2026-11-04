// @Role: Test blokujący wywołania fetch do /api poza lib/api/client.ts | @DependsOn: node:fs, src/** | @UsedBy: vitest | @Task: TASK-131
import { readdirSync, readFileSync, statSync } from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

const korzenSrc = path.resolve(import.meta.dirname, "../..");

function zbierz(katalog: string): string[] {
  const wynik: string[] = [];
  for (const nazwa of readdirSync(katalog)) {
    const sciezka = path.join(katalog, nazwa);
    if (statSync(sciezka).isDirectory()) wynik.push(...zbierz(sciezka));
    else if (/\.(ts|tsx)$/.test(nazwa)) wynik.push(sciezka);
  }
  return wynik;
}

describe("strażnik fetch", () => {
  it("tylko lib/api/client.ts woła fetch / zawiera adresy /api", () => {
    const naruszenia = zbierz(korzenSrc)
      .filter((p) => !/\.test\.tsx?$/.test(p))
      .filter((p) => !p.endsWith(path.join("lib", "api", "client.ts")))
      .filter((p) => !p.includes(`${path.sep}mocks${path.sep}`))
      .filter((p) => /\bfetch\s*\(\s*[`'"]\/api/.test(readFileSync(p, "utf8")));
    expect(naruszenia).toEqual([]);
  });

  it("wykrywa wzorzec w przykładowym kodzie", () => {
    expect(/\bfetch\s*\(\s*[`'"]\/api/.test("fetch('/api/v1/x')")).toBe(true);
  });
});
