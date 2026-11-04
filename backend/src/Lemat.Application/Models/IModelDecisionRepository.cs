// @Role: Port repozytorium Decyzji modelu (tabela partycjonowana): wsadowy zapis i odczyt po kontekście | @DependsOn: Lemat.Domain.Models.DecyzjaModelu, KontekstDecyzji | @UsedBy: ModeleFasada, Agent oceniający (planowane TASK-022), ModelDecisionRepositoryEf | @Task: TASK-018
using Lemat.Domain.Models;

namespace Lemat.Application.Models;

public interface IDecyzjaModeluRepozytorium
{
    /// <summary>Dodaje wiersze decyzji (wsadowo w jednej jednostce pracy zapisu Oceny, Z-4).</summary>
    void DodajRange(IEnumerable<DecyzjaModelu> decyzje);

    Task<IReadOnlyList<DecyzjaModelu>> ListaDlaKontekstuAsync(KontekstDecyzji kontekst, Guid kontekstId, CancellationToken ct);
}
