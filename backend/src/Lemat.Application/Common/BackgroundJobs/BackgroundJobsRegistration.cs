// @Role: Rejestracja w DI potoku prac w tle POC (tylko proces worker): procesor, handlery ROZPOZNAJ_PISMO i OCEN_ROZWIAZANIE; wymaga portów IKolejkaPrac, IAgentRozpoznajacy, IAgentOceniajacy oraz modułów Rozwiązania, Ocenianie, Treści, Modele, Kalibracja, Profil, Rekomendacje | @DependsOn: ProcesorPrac, RozpoznajPismoHandler, OcenRozwiazanieHandler | @UsedBy: Lemat.Worker (WorkerHost), testy | @Task: TASK-128
using Lemat.Application.Grading;
using Lemat.Application.Solutions;
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Application.Common.BackgroundJobs;

public static class BackgroundJobsRegistration
{
    public static IServiceCollection DodajPrzetwarzaniePrac(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddSingleton<ProcesorPrac>();
        uslugi.AddScoped<IHandlerPracy, RozpoznajPismoHandler>();
        uslugi.AddScoped<IHandlerPracy, OcenRozwiazanieHandler>();
        return uslugi;
    }
}
