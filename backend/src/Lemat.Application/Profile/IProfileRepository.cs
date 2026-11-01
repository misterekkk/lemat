// @Role: Port repozytorium agregatu Profil ucznia (z Opanowaniem Umiejętności): wczytanie do zmiany z If-Match i odczyt bez śledzenia | @DependsOn: Lemat.Domain.Profile.ProfilUcznia, UczenId | @UsedBy: ProfilFasada, ProfileRepositoryEf | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Profile;

namespace Lemat.Application.Profile;

public interface IProfilRepozytorium
{
    /// <summary>Wczytuje Profil do zmiany (ze śledzeniem); gdy podano <paramref name="oczekiwanaWersja"/>, niezgodność kończy zapis konfliktem współbieżności.</summary>
    Task<ProfilUcznia?> PobierzAsync(UczenId uczen, int? oczekiwanaWersja, CancellationToken ct);

    Task<ProfilUcznia?> PobierzDoOdczytuAsync(UczenId uczen, CancellationToken ct);

    void Dodaj(ProfilUcznia profil);
}
