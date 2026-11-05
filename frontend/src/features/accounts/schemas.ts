// @Role: Schematy zod formularzy logowania i rejestracji odwzorowujące reguły serwera (kontrakt 3.1); komunikaty jako klucze Konto.walidacja.* | @DependsOn: zod | @UsedBy: features/accounts/LoginForm, features/accounts/RegisterForm | @Task: TASK-133
import { z } from "zod";

const email = z
  .string()
  .trim()
  .min(1, "emailWymagany")
  .max(254, "emailZaDlugi")
  .pipe(z.email("emailFormat"));

export const schematLogowania = z.object({
  email,
  haslo: z.string().min(1, "hasloWymagane"),
});
export type LogowanieDane = z.infer<typeof schematLogowania>;

export const schematRejestracji = z.object({
  email,
  haslo: z.string().min(10, "hasloZaKrotkie").max(128, "hasloZaDlugie"),
  imie: z.string().trim().min(1, "imieWymagane").max(60, "imieZaDlugie"),
  nazwisko: z.string().trim().min(1, "nazwiskoWymagane").max(80, "nazwiskoZaDlugie"),
  oswiadczenie_wieku_16: z.boolean().refine((v) => v, "oswiadczenieWymagane"),
  zgoda_regulamin: z.boolean().refine((v) => v, "regulaminWymagany"),
});
export type RejestracjaDane = z.infer<typeof schematRejestracji>;
