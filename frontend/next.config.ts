// @Role: Konfiguracja Next.js: plugin next-intl i rewrites /api/:path* do backendu tylko w trybie real (NEXT_PUBLIC_LEMAT_API) | @DependsOn: next-intl/plugin, lib/api/proxy, zmienne LEMAT_API_URL i NEXT_PUBLIC_LEMAT_API | @UsedBy: next dev/build | @Task: TASK-006, TASK-138
import createNextIntlPlugin from "next-intl/plugin";
import type { NextConfig } from "next";
import { okreslTrybApi, regulyPrzekierowan } from "./src/lib/api/proxy";

const withNextIntl = createNextIntlPlugin("./src/lib/i18n/request.ts");

const tryb = okreslTrybApi(process.env.NEXT_PUBLIC_LEMAT_API);

const konfiguracja: NextConfig = {
  output: "standalone",
  // Wartość zawsze jawna (mock|real), by dead-code elimination usunęła MSW z buildu real.
  env: { NEXT_PUBLIC_LEMAT_API: tryb },
  async rewrites() {
    return regulyPrzekierowan(tryb, process.env.LEMAT_API_URL);
  },
};

export default withNextIntl(konfiguracja);
