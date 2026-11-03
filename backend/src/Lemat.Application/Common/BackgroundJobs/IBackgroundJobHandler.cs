// @Role: Port handlera pracy w tle (13.4.1): wykonuje jeden typ pracy idempotentnie; wyjątek = ponowienie, a po wyczerpaniu prób MARTWA | @DependsOn: TypPracy, PracaWTle | @UsedBy: ProcesorPrac, RozpoznajPismoHandler, OcenRozwiazanieHandler | @Task: TASK-128
namespace Lemat.Application.Common.BackgroundJobs;

public interface IHandlerPracy
{
    TypPracy Typ { get; }

    /// <summary>
    /// Wykonuje pracę. Musi być idempotentny (ponowienie po awarii nie duplikuje skutków) i sprawdzać warunek wykonania
    /// (13.3): praca, której warunek już nie zachodzi, kończy się bez skutków. Wywołania modeli i opóźnienia — poza transakcją.
    /// </summary>
    Task WykonajAsync(PracaWTle praca, CancellationToken ct);
}
