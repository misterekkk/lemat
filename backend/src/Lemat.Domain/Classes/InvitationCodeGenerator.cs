// @Role: Generator losowych Kodów zaproszenia Klasy (kryptograficzny RNG, alfabet bez mylących znaków, ważność 14 dni) | @DependsOn: KodZaproszenia, System.Security.Cryptography | @UsedBy: KlasyFasada, testy | @Task: TASK-018
using System.Security.Cryptography;

namespace Lemat.Domain.Classes;

public static class GeneratorKoduZaproszenia
{
    public static KodZaproszenia Nowy(DateTimeOffset teraz) =>
        new(RandomNumberGenerator.GetString(KodZaproszenia.Alfabet, KodZaproszenia.Dlugosc), teraz + KodZaproszenia.CzasWaznosci);
}
