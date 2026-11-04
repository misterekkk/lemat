// @Role: Mapowanie bledy_pol z odpowiedzi 422 WALIDACJA na pola formularza react-hook-form | @DependsOn: lib/api/errors | @UsedBy: features/accounts/LoginForm, features/accounts/RegisterForm | @Task: TASK-133
import type { BladApi } from "@/lib/api/errors";

/** Przypisuje komunikaty serwera do znanych pól; zwraca true, gdy przypisano choć jedno. */
export function przypiszBledyPol(
  blad: BladApi,
  znanePola: readonly string[],
  ustawBlad: (pole: string, komunikat: string) => void,
): boolean {
  let przypisano = false;
  for (const b of blad.bledyPol) {
    if (znanePola.includes(b.pole)) {
      ustawBlad(b.pole, b.komunikat);
      przypisano = true;
    }
  }
  return przypisano;
}
