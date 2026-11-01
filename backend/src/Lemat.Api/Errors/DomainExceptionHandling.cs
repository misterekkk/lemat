// @Role: Mapuje wyjątki domenowe/aplikacyjne/bazy na ProblemDetails (WyjatekDomenowy, KonfliktWspolbieznosci, BladApi, baza niedostępna) i 500 bez szczegółów technicznych | @DependsOn: ProblemWriter, ErrorCatalog, TlumaczBledowBazy, WyjatekDomenowy | @UsedBy: ApiHost (AddExceptionHandler) | @Task: TASK-121, TASK-123
using Lemat.Application.Accounts;
using Lemat.Application.Common;
using Lemat.Domain.Common;
using Lemat.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Npgsql;

namespace Lemat.Api.Errors;

/// <summary>Ostatni handler łańcucha: każdy nieobsłużony wyjątek kończy się ProblemDetails (ADR-060).</summary>
public sealed partial class ObslugaWyjatkowDomenowych(ILogger<ObslugaWyjatkowDomenowych> dziennik) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var wyjatek = TlumaczBledowBazy.Przetlumacz(exception) ?? exception;
        var (kod, opis, szczegoly, bledyPol) = Mapuj(wyjatek);
        if (kod == KodBledu.BladWewnetrzny)
        {
            LogNieobsluzony(exception, httpContext.Request.Path.Value);
        }

        return await ProblemWriter.ZapiszAsync(httpContext, kod, opis, szczegoly, bledyPol, token: cancellationToken);
    }

    private (KodBledu Kod, string? Opis, IReadOnlyDictionary<string, object?>? Szczegoly, IReadOnlyList<BladPola>? BledyPol) Mapuj(Exception wyjatek) =>
        wyjatek switch
        {
            BladApiException api => (api.Kod, api.Opis, api.Szczegoly, api.BledyPol),
            UwierzytelnienieNieudaneException uwierzytelnienie when ErrorCatalog.TryParse(uwierzytelnienie.KodBledu, out var kodUwierzytelnienia) => (kodUwierzytelnienia, uwierzytelnienie.Message, null, null),
            WyjatekDomenowy domenowy when ErrorCatalog.TryParse(domenowy.KodBledu, out var kod) => (kod, null, null, null),
            WyjatekDomenowy domenowy => NieznanyKod(domenowy.KodBledu),
            KonfliktWspolbieznosciException => (KodBledu.KonfliktWersji, null, null, null),
            _ when BazaNiedostepna(wyjatek) => (KodBledu.BazaNiedostepna, null, null, null),
            _ => (KodBledu.BladWewnetrzny, null, null, null),
        };

    private (KodBledu, string?, IReadOnlyDictionary<string, object?>?, IReadOnlyList<BladPola>?) NieznanyKod(string kodDomenowy)
    {
        LogNieznanyKod(kodDomenowy);
        return (KodBledu.BladWewnetrzny, null, null, null);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Nieobsłużony wyjątek w {Sciezka}")]
    private partial void LogNieobsluzony(Exception wyjatek, string? sciezka);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Kod błędu domeny {Kod} nie należy do katalogu API; zwrócono BLAD_WEWNETRZNY.")]
    private partial void LogNieznanyKod(string kod);

    private static bool BazaNiedostepna(Exception wyjatek)
    {
        for (var biezacy = wyjatek; biezacy is not null; biezacy = biezacy.InnerException)
        {
            switch (biezacy)
            {
                case PostgresException pg when pg.SqlState.StartsWith("08", StringComparison.Ordinal) || pg.SqlState is "57P01" or "57P02" or "57P03" or "53300":
                case NpgsqlException { IsTransient: true }:
                case System.Net.Sockets.SocketException:
                    return true;
            }
        }

        return false;
    }
}
