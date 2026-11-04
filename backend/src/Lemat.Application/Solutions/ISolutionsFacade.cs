// @Role: Fasada modułu Rozwiązania (6.2): odczyt własnego Rozwiązania Ucznia i jego Transkrypcji; przypadki zapisu dojdą w TASK-127 i TASK-128 | @DependsOn: Lemat.Domain.Solutions, RozwiazanieSkrot, TranskrypcjaSkrot | @UsedBy: Api (planowane TASK-127), moduły Ocenianie i Weryfikacje | @Task: TASK-126
using Lemat.Domain.Accounts;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Solutions;

public interface IRozwiazaniaFasada
{
    /// <summary>Rozwiązanie wskazanego Ucznia; cudze i nieistniejące zwracają <c>null</c> (uprawnienia obiektowe, ADR-071).</summary>
    Task<RozwiazanieSkrot?> PobierzDlaUczniaAsync(RozwiazanieId id, UczenId uczen, CancellationToken ct);

    /// <summary>Rozwiązania Ucznia (created_at malejąco, potem id), opcjonalnie z jednym statusem; strona po <paramref name="po"/> (wyłącznie).</summary>
    Task<StronaRozwiazan> ListaDlaUczniaAsync(UczenId uczen, StatusRozwiazania? status, PozycjaListyRozwiazan? po, int limit, CancellationToken ct);

    /// <summary>Najnowsza wersja Transkrypcji Rozwiązania; <c>null</c>, gdy jeszcze nie powstała.</summary>
    Task<TranskrypcjaSkrot?> PobierzNajnowszaTranskrypcjeAsync(RozwiazanieId id, CancellationToken ct);
}
