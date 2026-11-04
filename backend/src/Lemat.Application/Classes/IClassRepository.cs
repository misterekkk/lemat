// @Role: Port repozytorium agregatu Klasa (z Członkostwami): wczytanie do zmiany ze śledzeniem i If-Match, odczyty bez śledzenia, wyszukanie po Kodzie zaproszenia | @DependsOn: Lemat.Domain.Classes.Klasa, NauczycielId, UczenId | @UsedBy: KlasyFasada, Weryfikacje (przydział 11.4.1, planowane), ClassRepositoryEf | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Classes;

namespace Lemat.Application.Classes;

public interface IKlasaRepozytorium
{
    /// <summary>Wczytuje Klasę do zmiany (ze śledzeniem); gdy podano <paramref name="oczekiwanaWersja"/>, niezgodność kończy zapis konfliktem współbieżności.</summary>
    Task<Klasa?> PobierzAsync(KlasaId id, int? oczekiwanaWersja, CancellationToken ct);

    Task<Klasa?> PobierzDoOdczytuAsync(KlasaId id, CancellationToken ct);

    /// <summary>Klasa o danym (znormalizowanym) Kodzie zaproszenia, ze śledzeniem, albo <c>null</c>.</summary>
    Task<Klasa?> PobierzPoKodzieAsync(string kod, CancellationToken ct);

    Task<bool> KodZajetyAsync(string kod, CancellationToken ct);

    Task<IReadOnlyList<Klasa>> ListaNauczycielaAsync(NauczycielId nauczyciel, CancellationToken ct);

    /// <summary>Klasy Ucznia (także zarchiwizowane), od najpóźniej dołączonej.</summary>
    Task<IReadOnlyList<Klasa>> ListaUczniaAsync(UczenId uczen, CancellationToken ct);

    void Dodaj(Klasa klasa);
}
