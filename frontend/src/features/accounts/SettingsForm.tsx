// @Role: Formularz danych Ucznia (poziom matury, termin egzaminu, wybrany Dział): PATCH merge-patch z If-Match, obsługa 412 i 422 | @DependsOn: lib/api/client, lib/api/queries, settings-schema, field-errors, ui/Select, ui/Form, ui/Button | @UsedBy: app/(student)/settings/page | @Task: TASK-134
"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { useForm, type FieldError } from "react-hook-form";
import { zmienUcznia } from "@/lib/api/client";
import { czyBladApi } from "@/lib/api/errors";
import { kluczeZapytan, useDzialy, useUczen } from "@/lib/api/queries";
import type { UczenWyjscie } from "@/lib/api/types";
import { useKomunikatBledu } from "@/lib/api/use-error-message";
import { Button } from "@/ui/Button";
import { Form, FormField } from "@/ui/Form";
import { Select } from "@/ui/Select";
import { przypiszBledyPol } from "./field-errors";
import { useWysylkaPojedyncza } from "./use-single-submit";
import {
  schematUstawien,
  ustawieniaZUcznia,
  zbudujZmiane,
  type UstawieniaDane,
} from "./settings-schema";

const POLA = ["poziom_matury", "termin_egzaminu", "wybrany_dzial_id"] as const;

function czyUczen(wartosc: unknown): wartosc is UczenWyjscie {
  return (
    typeof wartosc === "object" &&
    wartosc !== null &&
    "version" in wartosc &&
    typeof wartosc.version === "number"
  );
}

export function SettingsForm() {
  const t = useTranslations("Ustawienia");
  const komunikatBledu = useKomunikatBledu();
  const klient = useQueryClient();
  const uczenQuery = useUczen();
  const dzialyQuery = useDzialy();
  const uczen = uczenQuery.data;
  const dzialy = dzialyQuery.data;
  const [komunikat, ustawKomunikat] = useState<{ rodzaj: "ok" | "blad"; tekst: string } | null>(
    null,
  );
  const pojedynczaWysylka = useWysylkaPojedyncza();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<UstawieniaDane>({
    resolver: zodResolver(schematUstawien),
    values: uczen ? ustawieniaZUcznia(uczen) : undefined,
  });

  if (uczenQuery.isError || dzialyQuery.isError) {
    return (
      <div className="flex flex-col gap-3">
        <h1 className="text-2xl font-bold">{t("naglowek")}</h1>
        <p role="alert" className="rounded-md bg-red-50 p-3 text-red-800">
          {komunikatBledu(uczenQuery.error ?? dzialyQuery.error)}
        </p>
        <Button
          wariant="drugorzedny"
          onClick={() => {
            void uczenQuery.refetch();
            void dzialyQuery.refetch();
          }}
        >
          {t("ponow")}
        </Button>
      </div>
    );
  }
  if (!uczen || !dzialy) {
    return (
      <p role="status" className="p-2">
        {t("ladowanie")}
      </p>
    );
  }

  const tekstBledu = (blad: FieldError | undefined): string | undefined =>
    blad?.message
      ? blad.type === "serwer"
        ? blad.message
        : t(`walidacja.${blad.message}`)
      : undefined;

  const wyslij = handleSubmit(async (dane) => {
    const zmiana = zbudujZmiane(uczen, dane);
    if (Object.keys(zmiana).length === 0) return;
    ustawKomunikat(null);
    try {
      const wynik = await pojedynczaWysylka(() => zmienUcznia(zmiana, uczen.version));
      if (!wynik) return;
      klient.setQueryData(kluczeZapytan.uczen, wynik);
      ustawKomunikat({ rodzaj: "ok", tekst: t("zapisano") });
    } catch (e) {
      if (czyBladApi(e) && e.kod === "KONFLIKT_WERSJI") {
        if (czyUczen(e.aktualny)) klient.setQueryData(kluczeZapytan.uczen, e.aktualny);
        else await klient.invalidateQueries({ queryKey: kluczeZapytan.uczen });
        ustawKomunikat({ rodzaj: "blad", tekst: t("konflikt") });
      } else if (
        czyBladApi(e) &&
        e.kod === "WALIDACJA" &&
        przypiszBledyPol(e, POLA, (pole, message) =>
          setError(pole as keyof UstawieniaDane, { type: "serwer", message }),
        )
      ) {
        // błędy pól ustawione
      } else {
        ustawKomunikat({ rodzaj: "blad", tekst: komunikatBledu(e) });
      }
    }
  });

  return (
    <Form onSubmit={wyslij} aria-labelledby="ustawienia-naglowek">
      <h1 id="ustawienia-naglowek" className="text-2xl font-bold">
        {t("naglowek")}
      </h1>
      {komunikat ? (
        <p
          role={komunikat.rodzaj === "blad" ? "alert" : "status"}
          className={
            komunikat.rodzaj === "blad"
              ? "rounded-md bg-red-50 p-3 text-red-800"
              : "rounded-md bg-green-50 p-3 text-green-800"
          }
        >
          {komunikat.tekst}
        </p>
      ) : null}
      <Select
        etykieta={t("poziomMatury")}
        blad={tekstBledu(errors.poziom_matury)}
        {...register("poziom_matury")}
      >
        {uczen.poziom_matury === null ? <option value="">{t("poziomNieWybrano")}</option> : null}
        <option value="PODSTAWOWY">{t("poziom.PODSTAWOWY")}</option>
        <option value="ROZSZERZONY">{t("poziom.ROZSZERZONY")}</option>
      </Select>
      <FormField
        etykieta={t("terminEgzaminu")}
        type="date"
        blad={tekstBledu(errors.termin_egzaminu)}
        {...register("termin_egzaminu")}
      />
      <Select
        etykieta={t("dzial")}
        blad={tekstBledu(errors.wybrany_dzial_id)}
        {...register("wybrany_dzial_id")}
      >
        <option value="">{t("wszystkieDzialy")}</option>
        {dzialy.map((d) => (
          <option key={d.id} value={String(d.id)}>
            {t("dzialOpcja", { kod: d.kod, nazwa: d.nazwa })}
          </option>
        ))}
      </Select>
      <Button type="submit" disabled={!isDirty || isSubmitting}>
        {isSubmitting ? t("wTrakcie") : t("wyslij")}
      </Button>
    </Form>
  );
}
