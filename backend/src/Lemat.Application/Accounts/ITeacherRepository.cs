// @Role: Port repozytorium agregatu Nauczyciel (klucz = identyfikator konta) | @DependsOn: Lemat.Domain.Accounts.Nauczyciel | @UsedBy: AccountsFacade, TeacherRepositoryEf, moduły Klasy i Weryfikacje przez IKontaFasada | @Task: TASK-016
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public interface INauczycielRepozytorium
{
    Task<Nauczyciel?> PobierzAsync(NauczycielId id, int? oczekiwanaWersja, CancellationToken ct);

    Task<Nauczyciel?> PobierzDoOdczytuAsync(NauczycielId id, CancellationToken ct);

    void Dodaj(Nauczyciel nauczyciel);
}
