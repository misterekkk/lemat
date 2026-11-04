// @Role: Formularz rejestracji Ucznia (react-hook-form + zod): reguły serwera, bledy_pol na polach, EMAIL_ZAJETY, sesja i przekierowanie do /exercises | @DependsOn: lib/auth/SessionProvider, schemas, field-errors, lib/api/use-error-message, ui/Form, ui/Button | @UsedBy: app/(public)/register/page | @Task: TASK-133
"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useId, useState } from "react";
import { useForm, type FieldError } from "react-hook-form";
import { czyBladApi } from "@/lib/api/errors";
import { useKomunikatBledu } from "@/lib/api/use-error-message";
import { useSession } from "@/lib/auth/SessionProvider";
import { Button } from "@/ui/Button";
import { Form, FormField } from "@/ui/Form";
import { przypiszBledyPol } from "./field-errors";
import { useJednaWysylka } from "./use-single-submit";
import { schematRejestracji, type RejestracjaDane } from "./schemas";

const POLA = [
  "email",
  "haslo",
  "imie",
  "nazwisko",
  "oswiadczenie_wieku_16",
  "zgoda_regulamin",
] as const;

export function RegisterForm() {
  const t = useTranslations("Konto");
  const komunikatBledu = useKomunikatBledu();
  const router = useRouter();
  const sesja = useSession();
  const idOswiadczenia = useId();
  const idRegulaminu = useId();
  const [bladOgolny, ustawBladOgolny] = useState<string | null>(null);
  const jednaWysylka = useJednaWysylka();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RejestracjaDane>({
    resolver: zodResolver(schematRejestracji),
    defaultValues: {
      email: "",
      haslo: "",
      imie: "",
      nazwisko: "",
      oswiadczenie_wieku_16: false,
      zgoda_regulamin: false,
    },
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
      const wynik = await jednaWysylka(() =>
        sesja.zarejestruj({ ...dane, rola: "UCZEN", kod_zaproszenia: null }),
      );
      if (wynik && !wynik.sesja) ustawBladOgolny(t("rejestracja.bezSesji"));
    } catch (e) {
      if (
        czyBladApi(e) &&
        e.kod === "WALIDACJA" &&
        przypiszBledyPol(e, POLA, (pole, message) =>
          setError(pole as keyof RejestracjaDane, { type: "serwer", message }),
        )
      ) {
        // błędy pól ustawione
      } else {
        ustawBladOgolny(komunikatBledu(e));
      }
    }
  });

  const pole = (nazwa: "email" | "haslo" | "imie" | "nazwisko") => ({
    blad: tekstBledu(errors[nazwa]),
    ...register(nazwa),
  });

  return (
    <Form onSubmit={wyslij} aria-labelledby="rejestracja-naglowek">
      <h1 id="rejestracja-naglowek" className="text-2xl font-bold">
        {t("rejestracja.naglowek")}
      </h1>
      {bladOgolny ? (
        <p role="alert" className="rounded-md bg-red-50 p-3 text-red-800">
          {bladOgolny}
        </p>
      ) : null}
      <FormField etykieta={t("rejestracja.imie")} autoComplete="given-name" {...pole("imie")} />
      <FormField
        etykieta={t("rejestracja.nazwisko")}
        autoComplete="family-name"
        {...pole("nazwisko")}
      />
      <FormField
        etykieta={t("rejestracja.email")}
        type="email"
        autoComplete="email"
        {...pole("email")}
      />
      <FormField
        etykieta={t("rejestracja.haslo")}
        type="password"
        autoComplete="new-password"
        {...pole("haslo")}
      />
      <div className="flex flex-col gap-1">
        <label htmlFor={idOswiadczenia} className="flex min-h-11 items-center gap-2">
          <input id={idOswiadczenia} type="checkbox" {...register("oswiadczenie_wieku_16")} />
          <span>{t("rejestracja.oswiadczenieWieku")}</span>
        </label>
        {errors.oswiadczenie_wieku_16 ? (
          <p role="alert" className="text-sm text-red-700">
            {tekstBledu(errors.oswiadczenie_wieku_16)}
          </p>
        ) : null}
      </div>
      <div className="flex flex-col gap-1">
        <label htmlFor={idRegulaminu} className="flex min-h-11 items-center gap-2">
          <input id={idRegulaminu} type="checkbox" {...register("zgoda_regulamin")} />
          <span>{t("rejestracja.zgodaRegulamin")}</span>
        </label>
        {errors.zgoda_regulamin ? (
          <p role="alert" className="text-sm text-red-700">
            {tekstBledu(errors.zgoda_regulamin)}
          </p>
        ) : null}
      </div>
      <Button type="submit" disabled={isSubmitting}>
        {isSubmitting ? t("rejestracja.wTrakcie") : t("rejestracja.wyslij")}
      </Button>
      <p>
        {t("rejestracja.maszKonto")}{" "}
        <Link href="/login" className="font-medium text-blue-700 underline">
          {t("rejestracja.linkLogowanie")}
        </Link>
      </p>
    </Form>
  );
}
