// @Role: Wyjątek nieudanego uwierzytelnienia (NIEUWIERZYTELNIONY, SESJA_UNIEWAZNIONA, KONTO_NIEAKTYWNE) z komunikatem dla Ucznia, mapowany na ProblemDetails | @DependsOn: Lemat.Domain.Common.WyjatekDomenowy | @UsedBy: LoginHandler, RefreshSessionHandler, ObslugaWyjatkowDomenowych | @Task: TASK-123
using Lemat.Domain.Common;

namespace Lemat.Application.Accounts;

public sealed class UwierzytelnienieNieudaneException(string kodBledu, string opis) : WyjatekDomenowy(kodBledu, opis)
{
    public const string Nieuwierzytelniony = "NIEUWIERZYTELNIONY";
    public const string KontoNieaktywne = "KONTO_NIEAKTYWNE";

    /// <summary>Jeden komunikat dla złego e-maila i złego hasła (brak wyliczania kont, ADR-072).</summary>
    public static UwierzytelnienieNieudaneException ZleDane() => new(Nieuwierzytelniony, "Nieprawidłowy e-mail lub hasło.");

    public static UwierzytelnienieNieudaneException NieaktywneKonto() => new(KontoNieaktywne, "To konto nie jest aktywne.");
}
