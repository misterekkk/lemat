// @Role: Testy formatowania liczb, procentów i dat w formacie polskim | @DependsOn: lib/i18n/format | @UsedBy: vitest | @Task: TASK-134
import { describe, expect, it } from "vitest";
import { formatujDateCzas, formatujLiczbe, formatujProcent } from "./format";

describe("format pl-PL", () => {
  it("procent z przecinkiem dziesiętnym i jednym miejscem", () => {
    expect(formatujProcent(50)).toMatch(/^50,0\s?%$/);
    expect(formatujProcent(66.7)).toMatch(/^66,7\s?%$/);
  });
  it("liczby z przecinkiem dziesiętnym i spacją tysięcy", () => {
    expect(formatujLiczbe(1.5)).toBe("1,5");
    expect(formatujLiczbe(12345.5)).toMatch(/^12\s345,5$/);
  });
  it("data i godzina w formacie polskim", () => {
    expect(formatujDateCzas("2026-11-02T10:12:00Z")).toMatch(/2026/);
    expect(formatujDateCzas("2026-11-02T10:12:00Z")).toMatch(/lis/);
  });
});
