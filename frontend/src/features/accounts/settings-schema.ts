// @Role: Schemat zod i mapowanie formularza ustawień Ucznia na JSON Merge Patch (tylko zmienione pola, null dla wyczyszczenia) | @DependsOn: zod, lib/api/types | @UsedBy: features/accounts/SettingsForm | @Task: TASK-134
import { z } from "zod";
import type { UczenWyjscie, ZmianaUczniaWejscie } from "@/lib/api/types";

export const schematUstawien = z.object({
  poziom_matury: z.enum(["", "PODSTAWOWY", "ROZSZERZONY"]),
  termin_egzaminu: z
    .string()
    .refine((v) => v === "" || (/^\d{4}-\d{2}-\d{2}$/.test(v) && !Number.isNaN(Date.parse(v))), {
      message: "terminFormat",
    }),
  wybrany_dzial_id: z.string(),
});
export type UstawieniaDane = z.infer<typeof schematUstawien>;

export function ustawieniaZUcznia(uczen: UczenWyjscie): UstawieniaDane {
  return {
    poziom_matury: uczen.poziom_matury ?? "",
    termin_egzaminu: uczen.termin_egzaminu ?? "",
    wybrany_dzial_id: uczen.wybrany_dzial_id === null ? "" : String(uczen.wybrany_dzial_id),
  };
}

/** Merge-patch tylko z polami, które się zmieniły; pusty termin i „Wszystkie Działy” to `null`. */
export function zbudujZmiane(uczen: UczenWyjscie, dane: UstawieniaDane): ZmianaUczniaWejscie {
  const zmiana: ZmianaUczniaWejscie = {};
  if (dane.poziom_matury !== "" && dane.poziom_matury !== uczen.poziom_matury) {
    zmiana.poziom_matury = dane.poziom_matury;
  }
  const termin = dane.termin_egzaminu === "" ? null : dane.termin_egzaminu;
  if (termin !== uczen.termin_egzaminu) zmiana.termin_egzaminu = termin;
  const dzial = dane.wybrany_dzial_id === "" ? null : Number(dane.wybrany_dzial_id);
  if (dzial !== uczen.wybrany_dzial_id) zmiana.wybrany_dzial_id = dzial;
  return zmiana;
}
