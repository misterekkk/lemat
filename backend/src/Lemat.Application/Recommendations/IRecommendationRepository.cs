// @Role: Port repozytorium niezmiennej Rekomendacji: dodawanie i odczyt po elemencie Ścieżki | @DependsOn: Lemat.Domain.Recommendations.Rekomendacja, ElementSciezkiId | @UsedBy: RekomendacjeFasada, RecommendationRepositoryEf | @Task: TASK-019
using Lemat.Domain.Recommendations;

namespace Lemat.Application.Recommendations;

public interface IRekomendacjaRepozytorium
{
    void Dodaj(Rekomendacja rekomendacja);

    Task<Rekomendacja?> PobierzPoElemencieAsync(ElementSciezkiId element, CancellationToken ct);
}
