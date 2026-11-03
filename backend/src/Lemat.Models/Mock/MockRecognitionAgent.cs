// @Role: MockAgentRozpoznajacy (ADR-065, U-26): deterministyczny odczyt pisma z danych POC 7.2 zależny od Zadania; scenariusz rozpoznanie-nieczytelne zwraca błąd NIECZYTELNE; treść zdjęcia nie ma znaczenia | @DependsOn: IAgentRozpoznajacy, MockDane | @UsedBy: RozpoznajPismoHandler (przez DI) | @Task: TASK-128
using Lemat.Application.Models;
using Lemat.Domain.Content;
using Lemat.Domain.Solutions;

namespace Lemat.Models.Mock;

public sealed class MockAgentRozpoznajacy(MockDane dane) : IAgentRozpoznajacy
{
    public const string ScenariuszNieczytelne = "rozpoznanie-nieczytelne";

    public Task<WynikRozpoznania> RozpoznajAsync(ZadanieId zadanie, string scenariusz, CancellationToken ct)
    {
        if (string.Equals(scenariusz, ScenariuszNieczytelne, StringComparison.Ordinal))
        {
            return Task.FromResult(WynikRozpoznania.Niepowodzenie(KodBleduRozpoznawania.Nieczytelne));
        }

        var mock = dane.Pobierz(zadanie);
        return Task.FromResult(new WynikRozpoznania(
            null,
            mock.ZgodnoscZZadaniem,
            [.. mock.Kroki.Select(k => new KrokOdczytany(k.Nr, k.Tekst, k.Latex, k.PewnoscOdczytu))]));
    }
}
