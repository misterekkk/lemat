// @Role: Testy reguł rewrites /api → backend (tylko tryb real) | @DependsOn: lib/api/proxy | @UsedBy: vitest | @Task: TASK-138
import { describe, expect, it } from "vitest";
import { okreslTrybApi, regulyPrzekierowan } from "./proxy";

describe("proxy /api", () => {
  it("domyślnie (brak zmiennej) tryb real i proxy na localhost:5080", () => {
    expect(okreslTrybApi(undefined)).toBe("real");
    expect(regulyPrzekierowan("real", undefined)).toEqual([
      { source: "/api/:path*", destination: "http://localhost:5080/api/:path*" },
    ]);
  });
  it("LEMAT_API_URL nadpisuje adres, końcowy ukośnik jest ucinany", () => {
    expect(regulyPrzekierowan("real", "http://api:8080/")[0]?.destination).toBe(
      "http://api:8080/api/:path*",
    );
  });
  it("tryb mock nie ma rewrites", () => {
    expect(okreslTrybApi("mock")).toBe("mock");
    expect(regulyPrzekierowan("mock", "http://x")).toEqual([]);
  });
});
