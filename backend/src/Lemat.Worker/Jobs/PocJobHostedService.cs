// @Role: Pętla odpytywania praca_w_tle w procesie worker (U-26): co Lemat:Poc:OdpytanieMs pobiera i wykonuje prace aż do opróżnienia kolejki, okresowo odzyskuje wygasłe dzierżawy; błąd iteracji nie zatrzymuje procesu | @DependsOn: ProcesorPrac, IKolejkaPrac, LematOpcje, TimeProvider | @UsedBy: WorkerHost | @Task: TASK-128
using Lemat.Application.Common;
using Lemat.Application.Common.BackgroundJobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lemat.Worker.Jobs;

public sealed partial class PocPracaHostedService(
    ProcesorPrac procesor,
    IKolejkaPrac kolejka,
    LematOpcje opcje,
    TimeProvider czas,
    ILogger<PocPracaHostedService> dziennik) : BackgroundService
{
    private const int CoIleIteracjiOdzyskiwacDzierzawy = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var iteracja = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (iteracja++ % CoIleIteracjiOdzyskiwacDzierzawy == 0)
                {
                    var odzyskane = await kolejka.OdzyskajDzierzawyAsync(stoppingToken);
                    if (odzyskane > 0)
                    {
                        LogOdzyskane(odzyskane);
                    }
                }

                while (await procesor.PrzetworzJednaAsync(stoppingToken))
                {
                    // Opróżniamy kolejkę, zanim znowu zaśpimy.
                }

                await Task.Delay(TimeSpan.FromMilliseconds(opcje.Poc.OdpytanieMs), czas, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                // Np. chwilowy brak bazy: pętla przeżywa i próbuje ponownie.
                LogBladPetli(e);
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(opcje.Poc.OdpytanieMs), czas, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Odzyskano {Liczba} prac w tle z wygasłą dzierżawą.")]
    private partial void LogOdzyskane(int liczba);

    [LoggerMessage(Level = LogLevel.Error, Message = "Błąd iteracji pętli prac w tle; ponowna próba za chwilę.")]
    private partial void LogBladPetli(Exception wyjatek);
}
