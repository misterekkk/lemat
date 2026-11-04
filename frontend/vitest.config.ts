// @Role: Konfiguracja Vitest (jsdom, Testing Library, alias @/) | @DependsOn: vitest | @UsedBy: npm run test | @Task: TASK-006
import path from "node:path";
import { defineConfig } from "vitest/config";

export default defineConfig({
  resolve: { alias: { "@": path.resolve(import.meta.dirname, "src") } },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./vitest.setup.ts"],
    include: ["src/**/*.test.{ts,tsx}"],
  },
});
