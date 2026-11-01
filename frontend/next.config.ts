// @Role: Konfiguracja Next.js: plugin next-intl i rewrites /api/:path* do backendu (dev) | @DependsOn: next-intl/plugin, zmienna LEMAT_API_URL | @UsedBy: next dev/build | @Task: TASK-006
import createNextIntlPlugin from "next-intl/plugin";
import type { NextConfig } from "next";

const withNextIntl = createNextIntlPlugin("./src/lib/i18n/request.ts");

const adresApi = process.env.LEMAT_API_URL ?? "http://localhost:5080";

const konfiguracja: NextConfig = {
  output: "standalone",
  async rewrites() {
    return [{ source: "/api/:path*", destination: `${adresApi}/api/:path*` }];
  },
};

export default withNextIntl(konfiguracja);
