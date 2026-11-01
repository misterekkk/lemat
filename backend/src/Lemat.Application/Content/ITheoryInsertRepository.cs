// @Role: Port repozytorium agregatu Wkładka teoretyczna (zmiana z If-Match, odczyt, opublikowana dla Umiejętności) | @DependsOn: Lemat.Domain.Content.WkladkaTeoretyczna | @UsedBy: ContentFacade, TheoryInsertRepositoryEf, przypadki użycia Treści | @Task: TASK-017
using Lemat.Domain.Content;

namespace Lemat.Application.Content;

public interface IWkladkaTeoretycznaRepozytorium
{
    Task<WkladkaTeoretyczna?> PobierzAsync(WkladkaTeoretycznaId id, int? oczekiwanaWersja, CancellationToken ct);

    Task<WkladkaTeoretyczna?> PobierzDoOdczytuAsync(WkladkaTeoretycznaId id, CancellationToken ct);

    /// <summary>Jedyna Wkładka <c>OPUBLIKOWANA</c> Umiejętności (<c>ux_wkladka_opublikowana</c>) albo <c>null</c>.</summary>
    Task<WkladkaTeoretyczna?> PobierzOpublikowanaAsync(UmiejetnoscId umiejetnosc, CancellationToken ct);

    void Dodaj(WkladkaTeoretyczna wkladka);
}
