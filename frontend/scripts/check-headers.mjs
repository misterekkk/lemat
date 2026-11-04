// @Role: Sprawdza nagłówki Living Documentation (@Role | @DependsOn | @UsedBy | @Task) w plikach .ts/.tsx frontendu | @DependsOn: node:fs, node:path | @UsedBy: npm run check:headers, scripts/verify-frontend.sh (TASK-007) | @Task: TASK-006
import { readdirSync, readFileSync, statSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const korzen = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const pominiete = new Set(["node_modules", ".next", "coverage", "out"]);
const pominitePliki = new Set(["next-env.d.ts", "schema.d.ts"]);
const wzorzec =
  /^\/\/ @Role: .+ \| @DependsOn: .+ \| @UsedBy: .+ \| @Task: TASK-\d{3}(, TASK-\d{3})*\s*$/;

function zbierz(katalog) {
  const wynik = [];
  for (const nazwa of readdirSync(katalog)) {
    if (pominiete.has(nazwa)) continue;
    const sciezka = path.join(katalog, nazwa);
    if (statSync(sciezka).isDirectory()) wynik.push(...zbierz(sciezka));
    else if (/\.(ts|tsx)$/.test(nazwa) && !pominitePliki.has(nazwa)) wynik.push(sciezka);
  }
  return wynik;
}

const bledy = [];
for (const plik of zbierz(korzen)) {
  const pierwsza = readFileSync(plik, "utf8").split(/\r?\n/, 1)[0] ?? "";
  if (!wzorzec.test(pierwsza)) bledy.push(path.relative(korzen, plik));
}

if (bledy.length > 0) {
  console.error("Brak poprawnego nagłówka Living Documentation w pierwszej linii:");
  for (const b of bledy) console.error(`  ${b}`);
  process.exit(1);
}
console.log("check-headers: OK");
