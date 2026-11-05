// @Role: Konfiguracja Playwright: projekty Desktop Chrome i Pixel 7, serwer dev:mock (MSW) albo dev:real (LEMAT_E2E_REAL=1) | @DependsOn: @playwright/test, skrypty dev:mock/dev:real | @UsedBy: npm run e2e, e2e/*.spec.ts | @Task: TASK-139
import { defineConfig, devices } from "@playwright/test";

const real = process.env.LEMAT_E2E_REAL === "1";
const port = Number(process.env.LEMAT_E2E_PORT ?? 3100);

export default defineConfig({
  testDir: "./e2e",
  // Tryb real: czas oczekiwania liczony z opóźnień mocka serwera (3 s odczyt + 2 s ocena).
  timeout: real ? 120_000 : 60_000,
  expect: { timeout: real ? 20_000 : 10_000 },
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: `http://localhost:${port}`,
    trace: "retain-on-failure",
    locale: "pl-PL",
  },
  projects: [
    { name: "desktop-chrome", use: { ...devices["Desktop Chrome"] } },
    { name: "pixel-7", use: { ...devices["Pixel 7"] } },
  ],
  webServer: {
    command: `npm run ${real ? "dev:real" : "dev:mock"} -- -p ${port}`,
    url: `http://localhost:${port}`,
    reuseExistingServer: false,
    timeout: 120_000,
    stdout: "ignore",
    stderr: "pipe",
  },
});
