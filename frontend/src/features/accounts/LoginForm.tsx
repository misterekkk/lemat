// @Role: Formularz logowania (react-hook-form + zod): walidacja po polsku, błędy serwera na polach, przekierowanie do /exercises po zalogowaniu | @DependsOn: lib/auth/SessionProvider, schemas, field-errors, lib/api/use-error-message, ui/Form, ui/Button | @UsedBy: app/(public)/login/page | @Task: TASK-133
"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useForm, type FieldError } from "react-hook-form";
import { czyBladApi } from "@/lib/api/errors";
import { useKomunikatBledu } from "@/lib/api/use-error-message";
import { useSession } from "@/lib/auth/SessionProvider";
import { Button } from "@/ui/Button";
import { Form, FormField } from "@/ui/Form";
import { przypiszBledyPol } from "./field-errors";
import { useJednaWysylka } from "./use-single-submit";
import { schematLogowania, type LogowanieDane } from "./schemas";

export function LoginForm() {
  const t = useTranslations("Konto");
  const komunikatBledu = useKomunikatBledu();
  const router = useRouter();
  const sesja = useSession();
  const [bladOgolny, ustawBladOgolny] = useState<string | null>(null);
  const jednaWysylka = useJednaWysylka();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LogowanieDane>({
    resolver: zodResolver(schematLogowania),
    defaultValues: { email: "", haslo: "" },
  });

  useEffect(() => {
    if (sesja.stan === "zalogowany") router.replace("/exercises");
  }, [sesja.stan, router]);

  const tekstBledu = (blad: FieldError | undefined): string | undefined =>
    blad?.message
      ? blad.type === "serwer"
        ? blad.message
        : t(`walidacja.${blad.message}`)
      : undefined;

  const wyslij = handleSubmit(async (dane) => {
    ustawBladOgolny(null);
    try {
      await jednaWysylka(() => sesja.zaloguj(dane));
    } catch (e) {
      if (czyBladApi(e) && e.kod === "NIEUWIERZYTELNIONY") {
        ustawBladOgolny(t("logowanie.bladDanych"));
      } else if (
        czyBladApi(e) &&
        e.kod === "WALIDACJA" &&
        przypiszBledyPol(e, ["email", "haslo"], (pole, message) =>
          setError(pole as keyof LogowanieDane, { type: "serwer", message }),
        )
      ) {
        // błędy pól ustawione
      } else {
        ustawBladOgolny(komunikatBledu(e));
      }
    }
  });

  return (
    <Form onSubmit={wyslij} aria-labelledby="logowanie-naglowek">
      <h1 id="logowanie-naglowek" className="text-2xl font-bold">
        {t("logowanie.naglowek")}
      </h1>
      {bladOgolny ? (
        <p role="alert" className="rounded-md bg-red-50 p-3 text-red-800">
          {bladOgolny}
        </p>
      ) : null}
      <FormField
        etykieta={t("logowanie.email")}
        type="email"
        autoComplete="email"
        blad={tekstBledu(errors.email)}
        {...register("email")}
      />
      <FormField
        etykieta={t("logowanie.haslo")}
        type="password"
        autoComplete="current-password"
        blad={tekstBledu(errors.haslo)}
        {...register("haslo")}
      />
      <Button type="submit" disabled={isSubmitting}>
        {isSubmitting ? t("logowanie.wTrakcie") : t("logowanie.wyslij")}
      </Button>
      <p>
        {t("logowanie.brakKonta")}{" "}
        <Link href="/register" className="font-medium text-blue-700 underline">
          {t("logowanie.linkRejestracja")}
        </Link>
      </p>
    </Form>
  );
}
