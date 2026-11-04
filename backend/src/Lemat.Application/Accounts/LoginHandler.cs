// @Role: Przypadek użycia logowania: weryfikacja hasła argon2id (jedna odpowiedź dla złego e-maila i hasła), status konta, nowa rodzina Tokenów odświeżania | @DependsOn: IUzytkownikRepozytorium, IHasherHasel, FabrykaSesji, IJednostkaPracy, TimeProvider | @UsedBy: AccountsEndpoints | @Task: TASK-123
using Lemat.Application.Common;
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public sealed class ZalogujHandler(
    IUzytkownikRepozytorium uzytkownicy,
    IHasherHasel hasher,
    FabrykaSesji sesje,
    IJednostkaPracy jednostkaPracy,
    TimeProvider czas)
{
    // Skrót "atrapa" wyrównuje czas odpowiedzi dla nieistniejącego konta (brak wyliczania adresów e-mail).
    private static string? _skrotAtrapy;

    public async Task<SesjaRezultat> WykonajAsync(ZalogujWejscie wejscie, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(wejscie);
        var dane = await uzytkownicy.PobierzDaneLogowaniaAsync(wejscie.Email, ct);
        var haslaZgodne = dane is not null
            ? hasher.Weryfikuj(wejscie.Haslo, dane.HasloHash)
            : AtrapaWeryfikacji(wejscie.Haslo);
        if (dane is null || !haslaZgodne)
        {
            throw UwierzytelnienieNieudaneException.ZleDane();
        }

        if (dane.Status != StatusKonta.Aktywne)
        {
            throw UwierzytelnienieNieudaneException.NieaktywneKonto();
        }

        return await jednostkaPracy.WykonajAsync(
            async token =>
            {
                var konto = await uzytkownicy.PobierzAsync(dane.UzytkownikId, null, token)
                    ?? throw UwierzytelnienieNieudaneException.ZleDane();
                konto.ZarejestrujLogowanie(czas.GetUtcNow());
                return sesje.Otworz(konto.Id, konto.Rola, konto.Status, konto.Imie, sesje.NowaRodzina(), wejscie.SkrotKlientaHttp);
            },
            ct);
    }

    private bool AtrapaWeryfikacji(string haslo)
    {
        var skrot = _skrotAtrapy ??= hasher.Haszuj(Guid.NewGuid().ToString("N"));
        _ = hasher.Weryfikuj(haslo, skrot);
        return false;
    }
}
