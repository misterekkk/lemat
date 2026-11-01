// @Role: Implementacja IKalibracjaFasada nad repozytoriami Kalibratora i Progu pewności oraz IJednostkaPracy: odczyt migawek, zapis kalibratora tożsamościowego, ustawienie nowego aktywnego Progu w jednej transakcji | @DependsOn: IKalibratorPewnosciRepozytorium, IProgPewnosciRepozytorium, IJednostkaPracy, TimeProvider | @UsedBy: DI (CalibrationRegistration), moduły przez IKalibracjaFasada | @Task: TASK-018
using Lemat.Application.Common;
using Lemat.Domain.Accounts;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Application.Calibration;

public sealed class KalibracjaFasada(
    IKalibratorPewnosciRepozytorium kalibratory,
    IProgPewnosciRepozytorium progi,
    IJednostkaPracy jednostkaPracy,
    TimeProvider czas) : IKalibracjaFasada
{
    public async Task<MigawkaProgu?> PobierzAktywnyProgAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct)
    {
        var prog = await progi.PobierzAktywnyAsync(konfiguracja, rodzaj, ct);
        return prog is null ? null : MigawkaProgu.Z(prog);
    }

    public async Task<MigawkaKalibratora?> PobierzKalibratorAsync(KalibratorPewnosciId id, CancellationToken ct)
    {
        var kalibrator = await kalibratory.PobierzDoOdczytuAsync(id, ct);
        return kalibrator is null ? null : MigawkaKalibratora.Z(kalibrator);
    }

    public async Task<AktywnaKalibracja?> PobierzAktywnaKalibracjeAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct)
    {
        var prog = await PobierzAktywnyProgAsync(konfiguracja, rodzaj, ct);
        if (prog is null)
        {
            return null;
        }

        var kalibrator = await PobierzKalibratorAsync(prog.KalibratorPewnosciId, ct);
        return kalibrator is null ? null : new AktywnaKalibracja(prog, kalibrator);
    }

    public async Task<MigawkaKalibratora> ZapiszKalibratorTozsamosciowyAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct)
    {
        var kalibrator = await jednostkaPracy.WykonajAsync(
            _ =>
            {
                var nowy = KalibratorPewnosci.Tozsamosciowy(new KalibratorPewnosciId(GeneratorUuid.Nowy(czas)), konfiguracja, rodzaj, czas.GetUtcNow());
                kalibratory.Dodaj(nowy);
                return Task.FromResult(nowy);
            },
            ct);
        return MigawkaKalibratora.Z(kalibrator);
    }

    public async Task<MigawkaProgu> UstawProgAsync(
        KonfiguracjaModeluId konfiguracja,
        RodzajZadania rodzaj,
        KalibratorPewnosciId kalibrator,
        decimal wartosc,
        ZrodloProgu zrodlo,
        string uzasadnienie,
        UzytkownikId? utworzonyPrzez,
        CancellationToken ct)
    {
        var prog = await jednostkaPracy.WykonajAsync(
            async token =>
            {
                var nowy = ProgPewnosci.Utworz(
                    new ProgPewnosciId(GeneratorUuid.Nowy(czas)),
                    konfiguracja,
                    rodzaj,
                    kalibrator,
                    wartosc,
                    zrodlo,
                    uzasadnienie,
                    null,
                    null,
                    utworzonyPrzez,
                    czas.GetUtcNow());
                nowy.Aktywuj();
                await progi.DezaktywujAktywnyAsync(konfiguracja, rodzaj, token);
                progi.Dodaj(nowy);
                return nowy;
            },
            ct);
        return MigawkaProgu.Z(prog);
    }
}
