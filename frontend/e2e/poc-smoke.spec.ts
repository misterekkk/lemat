// @Role: Smoke test E2E POC: rejestracja → ustawienia → Zadania → zdjęcie → Transkrypcja → Ocena → Profil → ponowne logowanie; test błędu rozpoznania | @DependsOn: playwright.config.ts, e2e/helpers.ts, e2e/fixtures/kartka.jpg | @UsedBy: npm run e2e | @Task: TASK-139
import { expect, test, type Page } from "@playwright/test";
import { EMAIL_DEMO, HASLO_DEMO, KARTKA, otworz, zaloguj, zarejestruj } from "./helpers";

const ZADANIE_2 = "Rozwiąż równanie";

async function otworzZadanieIPrzeslij(page: Page): Promise<void> {
  await page.getByRole("link", { name: "Zadania" }).first().click();
  const kafelek = page.getByRole("listitem").filter({ hasText: ZADANIE_2 });
  await kafelek.getByRole("link", { name: "Zobacz zadanie" }).click();
  await expect(page.locator(".katex").first()).toBeVisible();
  await page.getByRole("button", { name: "Prześlij rozwiązanie" }).click();
  await page.getByLabel("Dodaj zdjęcia kartki").setInputFiles(KARTKA);
  await expect(page.getByText("Dodane zdjęcia: 1 z 5")).toBeVisible();
  await page.getByRole("button", { name: "Wyślij do oceny" }).click();
  await expect(page).toHaveURL(/\/solutions\/[0-9a-f-]+$/);
}

test("smoke POC: pełny przepływ Ucznia", async ({ page }) => {
  const email = `ola.${Date.now()}@lemat.local`;
  await zarejestruj(page, email, HASLO_DEMO);

  // Ustawienia: poziom matury.
  await page.getByRole("link", { name: "Ustawienia" }).first().click();
  await page.getByLabel("Poziom matury").selectOption({ label: "Podstawowy" });
  await page.getByRole("button", { name: "Zapisz zmiany" }).click();
  await expect(page.getByText("Zapisano zmiany.")).toBeVisible();

  // Lista 5 Zadań.
  await page.getByRole("link", { name: "Zadania" }).first().click();
  await expect(page.getByRole("link", { name: "Zobacz zadanie" })).toHaveCount(5);

  await otworzZadanieIPrzeslij(page);

  // Ekran oczekiwania → Transkrypcja → zatwierdzenie → Ocena.
  await expect(page.getByText("Odczytujemy Twoje rozwiązanie…")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Odczytane rozwiązanie" })).toBeVisible({
    timeout: 20_000,
  });
  await page.getByRole("button", { name: "Zatwierdź" }).click();
  await expect(page.getByRole("heading", { name: "Ocena", exact: true })).toBeVisible({
    timeout: 20_000,
  });
  await expect(page.getByText("1 z 2 pkt").first()).toBeVisible();
  await expect(page.getByRole("heading", { name: "Wskazówka" })).toBeVisible();

  // Profil: 1 rozwiązanie, 50,0%.
  await page.getByRole("link", { name: "Profil" }).first().click();
  await expect(page.getByRole("heading", { name: "Profil ucznia" })).toBeVisible();
  await expect(page.getByText("50,0%").first()).toBeVisible();
  await expect(page.getByText("Liczba rozwiązań").locator("..")).toContainText("1");

  // Wylogowanie i ponowne logowanie kontem demo.
  await page.getByRole("button", { name: "Wyloguj" }).click();
  await expect(page).toHaveURL(/\/($|login)/);
  await otworz(page, "/login");
  await zaloguj(page, EMAIL_DEMO, HASLO_DEMO);
});

test("rozpoznanie-nieczytelne: błąd rozpoznania z przyciskiem ponownego przesłania", async ({
  page,
}) => {
  // Scenariusz mocka (ADR-065): nagłówek dodany do wysyłki Rozwiązania.
  await page.addInitScript(() => {
    const oryginalny = window.fetch.bind(window);
    window.fetch = (wejscie, opcje) => {
      const adres = wejscie instanceof Request ? wejscie.url : String(wejscie);
      const metoda = (
        opcje?.method ?? (wejscie instanceof Request ? wejscie.method : "GET")
      ).toUpperCase();
      if (metoda === "POST" && new URL(adres, location.href).pathname === "/api/v1/rozwiazania") {
        const naglowki = new Headers(opcje?.headers);
        naglowki.set("X-Lemat-Mock-Scenariusz", "rozpoznanie-nieczytelne");
        return oryginalny(wejscie, { ...opcje, headers: naglowki });
      }
      return oryginalny(wejscie, opcje);
    };
  });
  await zarejestruj(page, `blad.${Date.now()}@lemat.local`, HASLO_DEMO);
  await otworzZadanieIPrzeslij(page);
  await expect(page.getByRole("heading", { name: "Nie udało się odczytać zdjęcia" })).toBeVisible({
    timeout: 20_000,
  });
  await expect(page.getByRole("link", { name: "Prześlij ponownie" })).toBeVisible();
});
