// @Role: Formatowanie liczb, procentów i dat w polskich ustawieniach regionalnych (Intl, pl-PL) | @DependsOn: Intl | @UsedBy: features/profile | @Task: TASK-134
const LOKALIZACJA = "pl-PL";

const formatLiczby = new Intl.NumberFormat(LOKALIZACJA, { maximumFractionDigits: 2 });
const formatProcentu = new Intl.NumberFormat(LOKALIZACJA, {
  style: "percent",
  minimumFractionDigits: 1,
  maximumFractionDigits: 1,
});
const formatDatyCzasu = new Intl.DateTimeFormat(LOKALIZACJA, {
  dateStyle: "medium",
  timeStyle: "short",
});

/** Liczba w formacie polskim (przecinek dziesiętny, spacja jako separator tysięcy). */
export function formatujLiczbe(wartosc: number): string {
  return formatLiczby.format(wartosc);
}

/** Wartość w punktach procentowych (50 → „50,0%”) w formacie polskim. */
export function formatujProcent(wartosc: number): string {
  return formatProcentu.format(wartosc / 100);
}

/** Data i godzina RFC 3339 w formacie polskim. */
export function formatujDateCzas(rfc3339: string): string {
  return formatDatyCzasu.format(new Date(rfc3339));
}
