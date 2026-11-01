// @Role: Port repozytorium agregatu Uczeń (klucz = identyfikator konta) | @DependsOn: Lemat.Domain.Accounts.Uczen | @UsedBy: AccountsFacade, moduły Profil i Klasy przez IKontaFasada, StudentRepositoryEf | @Task: TASK-016
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public interface IUczenRepozytorium
{
    Task<Uczen?> PobierzAsync(UczenId id, int? oczekiwanaWersja, CancellationToken ct);

    Task<Uczen?> PobierzDoOdczytuAsync(UczenId id, CancellationToken ct);

    void Dodaj(Uczen uczen);
}
