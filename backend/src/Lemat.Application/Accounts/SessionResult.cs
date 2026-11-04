// @Role: Rezultat otwarcia sesji: token dostępu, dane Użytkownika do odpowiedzi oraz surowy refresh token do ciasteczka | @DependsOn: WystawionyTokenDostepu, Lemat.Domain.Accounts | @UsedBy: RegisterUserHandler, LoginHandler, RefreshSessionHandler, AccountsEndpoints | @Task: TASK-123
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

/// <summary>Rezultat operacji sesyjnej. <see cref="TokenOdswiezania"/> jest wartością jawną tylko w tym rezultacie (w bazie jest wyłącznie skrót SHA-256).</summary>
public sealed record SesjaRezultat(
    WystawionyTokenDostepu TokenDostepu,
    UzytkownikId UzytkownikId,
    RolaUzytkownika Rola,
    StatusKonta Status,
    string Imie,
    string TokenOdswiezania,
    DateTimeOffset OdswiezanieWygasaAt);
