// @Role: Wyjątek aplikacyjny konfliktu współbieżności (token version/xmin, zakleszczenie, konflikt serializacji; 15.2) mapowany przez Api na 412/409 albo przez worker na rezultat przestarzały | @DependsOn: — | @UsedBy: Lemat.Infrastructure.Persistence.JednostkaPracyEf, Lemat.Api (TASK-071), handlery prac w tle | @Task: TASK-015
namespace Lemat.Application.Common;

public sealed class KonfliktWspolbieznosciException : Exception
{
    public const string Kod = "KONFLIKT_WSPOLBIEZNOSCI";

    public KonfliktWspolbieznosciException()
        : base(Kod)
    {
    }

    public KonfliktWspolbieznosciException(Exception przyczyna)
        : base(Kod, przyczyna)
    {
    }

    public KonfliktWspolbieznosciException(string komunikat, Exception przyczyna)
        : base(komunikat, przyczyna)
    {
    }
}
