// @Role: Strażnik i powłoka układu Ucznia: bez sesji przekierowanie na /login, nawigacja (Zadania, Rozwiązania, Profil, Ustawienia, Wyloguj) działająca na 375 px | @DependsOn: lib/auth/SessionProvider, next/navigation, ui/Button | @UsedBy: app/(student)/layout | @Task: TASK-133
"use client";

import clsx from "clsx";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { useSession } from "@/lib/auth/SessionProvider";
import { Button } from "@/ui/Button";

const POZYCJE = [
  { href: "/exercises", klucz: "zadania" },
  { href: "/solutions", klucz: "rozwiazania" },
  { href: "/profile", klucz: "profil" },
  { href: "/settings", klucz: "ustawienia" },
] as const;

export function StudentShell({ children }: { children: ReactNode }) {
  const t = useTranslations("Konto");
  const router = useRouter();
  const sciezka = usePathname();
  const sesja = useSession();

  useEffect(() => {
    if (sesja.stan === "anonimowy") router.replace("/login");
  }, [sesja.stan, router]);

  if (sesja.stan === "ladowanie") {
    return (
      <p role="status" className="p-6">
        {t("sesjaLadowanie")}
      </p>
    );
  }
  if (sesja.stan === "anonimowy") return null;

  return (
    <div className="min-h-screen">
      <header className="border-b border-slate-200">
        <nav
          aria-label={t("nawigacja.etykieta")}
          className="mx-auto flex max-w-4xl flex-wrap items-center gap-x-4 gap-y-1 px-4 py-2"
        >
          {POZYCJE.map((pozycja) => (
            <Link
              key={pozycja.href}
              href={pozycja.href}
              aria-current={sciezka?.startsWith(pozycja.href) ? "page" : undefined}
              className={clsx(
                "inline-flex min-h-11 items-center px-1 font-medium hover:underline",
                sciezka?.startsWith(pozycja.href) && "text-blue-700 underline",
              )}
            >
              {t(`nawigacja.${pozycja.klucz}`)}
            </Link>
          ))}
          <Button
            wariant="drugorzedny"
            className="ml-auto"
            onClick={() => {
              void sesja.wyloguj();
            }}
          >
            {t("nawigacja.wyloguj")}
          </Button>
        </nav>
      </header>
      <main className="mx-auto max-w-4xl p-4">{children}</main>
    </div>
  );
}
