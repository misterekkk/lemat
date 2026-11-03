// @Role: Port repozytorium agregatu Transkrypcja: dodawanie, odczyt po identyfikatorze i najnowszej wersji Rozwiązania | @DependsOn: Lemat.Domain.Solutions.Transkrypcja, RozwiazanieId | @UsedBy: SolutionsFacade, TranscriptionRepositoryEf, handler ROZPOZNAJ_PISMO (planowane TASK-128) | @Task: TASK-126
using Lemat.Domain.Solutions;

namespace Lemat.Application.Solutions;

public interface ITranskrypcjaRepozytorium
{
    void Dodaj(Transkrypcja transkrypcja);

    /// <summary>Transkrypcja z krokami (bez śledzenia); <c>null</c>, gdy brak.</summary>
    Task<Transkrypcja?> PobierzAsync(TranskrypcjaId id, CancellationToken ct);

    /// <summary>Wersja o najwyższym numerze dla Rozwiązania (bez śledzenia); <c>null</c>, gdy Rozwiązanie nie ma Transkrypcji.</summary>
    Task<Transkrypcja?> PobierzNajnowszaAsync(RozwiazanieId rozwiazanie, CancellationToken ct);
}
