// @Role: Porty repozytoriów danych referencyjnych: Działy i Umiejętności (odczyt list, dodanie wiersza przez loader danych TASK-025) | @DependsOn: Lemat.Domain.Content.Dzial, Lemat.Domain.Content.Umiejetnosc | @UsedBy: ContentFacade, CurriculumRepositoryEf, loader danych startowych | @Task: TASK-017
using Lemat.Domain.Content;

namespace Lemat.Application.Content;

public interface IDzialRepozytorium
{
    /// <summary>Wszystkie Działy w kolejności <c>kolejnosc</c>.</summary>
    Task<IReadOnlyList<Dzial>> ListaAsync(CancellationToken ct);

    Task<Dzial?> PobierzAsync(DzialId id, CancellationToken ct);

    void Dodaj(Dzial dzial);
}

public interface IUmiejetnoscRepozytorium
{
    /// <summary>Umiejętności (z wymaganymi) w kolejności Działu i kodu; <paramref name="dzial"/> = <c>null</c> -> wszystkie.</summary>
    Task<IReadOnlyList<Umiejetnosc>> ListaAsync(DzialId? dzial, CancellationToken ct);

    Task<Umiejetnosc?> PobierzAsync(UmiejetnoscId id, CancellationToken ct);

    void Dodaj(Umiejetnosc umiejetnosc);
}
