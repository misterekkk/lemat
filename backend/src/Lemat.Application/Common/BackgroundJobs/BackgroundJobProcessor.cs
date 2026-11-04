// @Role: Procesor prac w tle (U-26): pobiera jedną pracę z kolejki, wykonuje ją handlerem w osobnym zakresie DI, kończy albo ponawia (limit prób → MARTWA); błąd jednej pracy nie zatrzymuje pętli | @DependsOn: IKolejkaPrac, IHandlerPracy, LematOpcje, IServiceScopeFactory | @UsedBy: PocPracaHostedService (Lemat.Worker), testy | @Task: TASK-128
using Lemat.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lemat.Application.Common.BackgroundJobs;

public sealed partial class ProcesorPrac(
    IKolejkaPrac kolejka,
    IServiceScopeFactory zakresy,
    LematOpcje opcje,
    ILogger<ProcesorPrac> dziennik)
{
    private const int MaksDlugoscBledu = 500;

    private readonly string _wykonawca = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

    private IReadOnlyCollection<TypPracy>? _typy;

    public string Wykonawca => _wykonawca;

    /// <summary>Pobiera i wykonuje jedną pracę; <c>false</c>, gdy kolejka jest pusta.</summary>
    public async Task<bool> PrzetworzJednaAsync(CancellationToken ct)
    {
        var praca = await kolejka.PobierzNastepnaAsync(TypyObslugiwane(), _wykonawca, TimeSpan.FromSeconds(opcje.Poc.DzierzawaSekund), ct);
        if (praca is null)
        {
            return false;
        }

        try
        {
            using var zakres = zakresy.CreateScope();
            var handler = zakres.ServiceProvider.GetServices<IHandlerPracy>().Single(h => h.Typ == praca.Typ);
            await handler.WykonajAsync(praca, ct);
            if (!await kolejka.ZakonczAsync(praca.Id, _wykonawca, CancellationToken.None))
            {
                LogUtrataDzierzawy(praca.Id, KonwencjaNazw.PascalNaUpperSnake(praca.Typ.ToString()));
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Zatrzymanie procesu: dzierżawa wygaśnie i praca wróci do kolejki (odzyskiwanie dzierżaw).
            throw;
        }
        catch (Exception e)
        {
            var blad = $"{e.GetType().Name}: {e.Message}";
            var status = await kolejka.PonowAsync(
                praca.Id,
                _wykonawca,
                blad.Length > MaksDlugoscBledu ? blad[..MaksDlugoscBledu] : blad,
                TimeSpan.FromMilliseconds(opcje.Poc.OdstepPonowieniaMs),
                CancellationToken.None);
            LogBladPracy(e, praca.Id, KonwencjaNazw.PascalNaUpperSnake(praca.Typ.ToString()), praca.Proby, status?.ToString() ?? "UTRACONA_DZIERZAWA");
        }

        return true;
    }

    private IReadOnlyCollection<TypPracy> TypyObslugiwane()
    {
        if (_typy is null)
        {
            using var zakres = zakresy.CreateScope();
            _typy = [.. zakres.ServiceProvider.GetServices<IHandlerPracy>().Select(h => h.Typ).Distinct()];
        }

        return _typy;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Praca w tle {PracaId} ({Typ}) utraciła dzierżawę przed zakończeniem.")]
    private partial void LogUtrataDzierzawy(long pracaId, string typ);

    [LoggerMessage(Level = LogLevel.Error, Message = "Błąd pracy w tle {PracaId} ({Typ}), próba {Proba}; status po porażce: {Status}.")]
    private partial void LogBladPracy(Exception wyjatek, long pracaId, string typ, int proba, string status);
}
