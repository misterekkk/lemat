// @Role: Pomocniki testów E2E: otwarcie aplikacji po starcie Service Workera MSW, rejestracja i logowanie Ucznia | @DependsOn: @playwright/test, messages/pl-PL.json (teksty UI) | @UsedBy: e2e/poc-smoke.spec.ts | @Task: TASK-139
import { expect, type Page } from "@playwright/test";

export const TRYB_REAL = process.env.LEMAT_E2E_REAL === "1";
export const HASLO_DEMO = "Lemat-Demo-2026!";
export const EMAIL_DEMO = "uczen1@lemat.local";
export const KARTKA = "e2e/fixtures/kartka.jpg";

/** Otwiera adres; w trybie mock czeka na baner „Dane przykładowe” (MockProvider wstrzymuje render do startu workera). */
export async function otworz(page: Page, adres: string): Promise<void> {
  await page.goto(adres);
  if (!TRYB_REAL) {
    await expect(page.getByRole("note").filter({ hasText: "Dane przykładowe" })).toBeVisible();
  }
}

export async function zarejestruj(page: Page, email: string, haslo: string): Promise<void> {
  await otworz(page, "/register");
  await page.getByLabel("Adres e-mail").fill(email);
  await page.getByLabel("Hasło (10–128 znaków)").fill(haslo);
  await page.getByLabel("Imię").fill("Ola");
  await page.getByLabel("Nazwisko").fill("Wiśniewska");
  await page.getByLabel("Oświadczam, że ukończyłem(-am) 16 lat.").check();
  await page.getByLabel("Akceptuję regulamin serwisu.").check();
  await page.getByRole("button", { name: "Załóż konto" }).click();
  await expect(page).toHaveURL(/\/exercises$/);
}

export async function zaloguj(page: Page, email: string, haslo: string): Promise<void> {
  await expect(page.getByRole("heading", { name: "Zaloguj się" })).toBeVisible();
  await page.getByLabel("Adres e-mail").fill(email);
  await page.getByLabel("Hasło").fill(haslo);
  await page.getByRole("button", { name: "Zaloguj się" }).click();
  await expect(page).toHaveURL(/\/exercises$/);
}
