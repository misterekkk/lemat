// @Role: Reguły rewrites /api/:path* → backend dla next.config.ts: aktywne tylko w trybie real (w trybie mock żądania przechwytuje MSW) | @DependsOn: (brak) | @UsedBy: next.config.ts, lib/api/proxy.test.ts | @Task: TASK-138
export type TrybApi = "mock" | "real";

export const DOMYSLNY_ADRES_API = "http://localhost:5080";

/** Tryb z NEXT_PUBLIC_LEMAT_API; wszystko poza `mock` (także brak zmiennej) oznacza `real`. */
export function okreslTrybApi(wartosc: string | undefined): TrybApi {
  return wartosc === "mock" ? "mock" : "real";
}

export function regulyPrzekierowan(
  tryb: TrybApi,
  adresApi: string | undefined,
): { source: string; destination: string }[] {
  if (tryb !== "real") return [];
  const adres = (adresApi?.trim() || DOMYSLNY_ADRES_API).replace(/\/+$/, "");
  return [{ source: "/api/:path*", destination: `${adres}/api/:path*` }];
}
