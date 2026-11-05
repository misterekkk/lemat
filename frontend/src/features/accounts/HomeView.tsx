// @Role: Strona główna: opis platformy i linki „Zaloguj się”/„Załóż konto”; zalogowany Uczeń jest przekierowany na /exercises | @DependsOn: lib/auth/SessionProvider, next/navigation, next-intl, ui/Button | @UsedBy: app/(public)/page | @Task: TASK-138
"use client";

import { useTranslations } from "next-intl";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useSession } from "@/lib/auth/SessionProvider";

const klasaLinku =
  "inline-flex min-h-11 items-center justify-center rounded-md px-4 py-2 font-medium focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600";

export function HomeView() {
  const t = useTranslations("Strona");
  const router = useRouter();
  const sesja = useSession();

  useEffect(() => {
    if (sesja.stan === "zalogowany") router.replace("/exercises");
  }, [sesja.stan, router]);

  return (
    <main className="mx-auto max-w-2xl p-6">
      <h1 className="text-3xl font-bold">{t("naglowek")}</h1>
      <p className="mt-4 text-lg">{t("opis")}</p>
      {sesja.stan === "anonimowy" && (
        <div className="mt-6 flex flex-col gap-3 sm:flex-row">
          <Link href="/login" className={`${klasaLinku} bg-blue-700 text-white hover:bg-blue-800`}>
            {t("zaloguj")}
          </Link>
          <Link
            href="/register"
            className={`${klasaLinku} border border-slate-300 bg-white text-slate-900 hover:bg-slate-50`}
          >
            {t("zalozKonto")}
          </Link>
        </div>
      )}
    </main>
  );
}
