// @Role: Przypadek użycia rejestracji Ucznia: konto, Uczeń, Profil ucznia, Ścieżka nauki i pierwsza sesja w jednej transakcji (FR-01, U-21) | @DependsOn: IKontaFasada, IProfilFasada, IRekomendacjeFasada, IHasherHasel, FabrykaSesji, IJednostkaPracy | @UsedBy: AccountsEndpoints | @Task: TASK-123
using Lemat.Application.Common;
using Lemat.Application.Profile;
using Lemat.Application.Recommendations;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Application.Accounts;

public sealed class ZarejestrujUzytkownikaHandler(
    IKontaFasada konta,
    IUzytkownikRepozytorium uzytkownicy,
    IProfilFasada profil,
    IRekomendacjeFasada rekomendacje,
    IHasherHasel hasher,
    FabrykaSesji sesje,
    IJednostkaPracy jednostkaPracy)
{
    public async Task<SesjaRezultat> WykonajAsync(ZarejestrujUzytkownikaWejscie wejscie, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(wejscie);
        if (wejscie.Rola != RolaUzytkownika.Uczen)
        {
            // Rola NAUCZYCIEL i ADMINISTRATOR jest odrzucana wcześniej walidacją wejścia (U-21); to tylko zabezpieczenie.
            throw new NiezmiennikNaruszonyException("ROLA_NIEOBSLUGIWANA", "Ta rola nie jest obsługiwana.");
        }

        if (await uzytkownicy.PobierzPoEmailuAsync(wejscie.Email, ct) is not null)
        {
            throw new NiezmiennikNaruszonyException("EMAIL_ZAJETY");
        }

        // Haszowanie argon2id (CPU) poza transakcją.
        var skrotHasla = hasher.Haszuj(wejscie.Haslo);
        var imie = wejscie.Imie.Trim();

        return await jednostkaPracy.WykonajAsync(
            async token =>
            {
                var konto = await konta.ZarejestrujAsync(
                    new RejestracjaKontaWejscie(wejscie.Rola, wejscie.Email, skrotHasla, wejscie.Imie, wejscie.Nazwisko, wejscie.OswiadczenieWieku16),
                    token);
                var uczen = UczenId.Z(konto.UzytkownikId);
                await profil.ZalozProfilAsync(uczen, token);
                await rekomendacje.ZalozSciezkeAsync(uczen, token);
                return sesje.Otworz(konto.UzytkownikId, konto.Rola, konto.Status, imie, sesje.NowaRodzina(), wejscie.SkrotKlientaHttp);
            },
            ct);
    }
}
