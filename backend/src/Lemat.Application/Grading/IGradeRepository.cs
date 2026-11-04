// @Role: Port repozytorium agregatu Ocena: dodawanie, odczyt Oceny aktualnej (bez i ze śledzeniem), mapowanie numerów Kryteriów na identyfikatory wiersza kryterium_oceniania | @DependsOn: Lemat.Domain.Grading.Ocena, RozwiazanieId, ZadanieWersjaId, WariantRozwiazania | @UsedBy: GradingFacade, GradeRepositoryEf, handler OCEN_ROZWIAZANIE (planowane TASK-128) | @Task: TASK-126
using Lemat.Domain.Content;
using Lemat.Domain.Grading;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Grading;

public interface IOcenaRepozytorium
{
    void Dodaj(Ocena ocena);

    /// <summary>Ocena aktualna (status inny niż ZASTAPIONA) z Kryteriami, bez śledzenia; <c>null</c>, gdy brak.</summary>
    Task<Ocena?> PobierzAktualnaAsync(RozwiazanieId rozwiazanie, CancellationToken ct);

    /// <summary>Ocena aktualna ze śledzeniem (zastąpienie, uzupełnienie Wskazówki); <c>null</c>, gdy brak.</summary>
    Task<Ocena?> PobierzAktualnaDoZmianyAsync(RozwiazanieId rozwiazanie, CancellationToken ct);

    /// <summary>Numer Kryterium (<c>nr_kryterium</c>) → identyfikator wiersza <c>kryterium_oceniania</c> dla wariantu wersji Zadania.</summary>
    Task<IReadOnlyDictionary<int, Guid>> PobierzIdentyfikatoryKryteriowAsync(ZadanieWersjaId wersja, WariantRozwiazania wariant, CancellationToken ct);
}
