// @Role: Główny układ: <html lang="pl">, NextIntlClientProvider, QueryClientProvider, style KaTeX | @DependsOn: next-intl, lib/query/QueryProvider, globals.css | @UsedBy: Next.js App Router (wszystkie trasy) | @Task: TASK-006
import { NextIntlClientProvider } from "next-intl";
import { getMessages, getTranslations } from "next-intl/server";
import type { Metadata } from "next";
import type { ReactNode } from "react";
import { QueryProvider } from "@/lib/query/QueryProvider";
import "./globals.css";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("Meta");
  return { title: t("tytul"), description: t("opis") };
}

export default async function RootLayout({ children }: { children: ReactNode }) {
  const komunikaty = await getMessages();
  return (
    <html lang="pl">
      <body className="min-h-screen bg-white text-slate-900">
        <NextIntlClientProvider locale="pl-PL" messages={komunikaty}>
          <QueryProvider>{children}</QueryProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
