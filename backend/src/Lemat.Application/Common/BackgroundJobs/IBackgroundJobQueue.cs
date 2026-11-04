// @Role: Port kolejki prac w tle (13.4, uproszczony U-26): atomowe pobranie z dzierżawą (FOR UPDATE SKIP LOCKED), zakończenie, ponowienie z limitem prób (MARTWA) i odzyskiwanie wygasłych dzierżaw | @DependsOn: PracaWTle, TypPracy, StatusPracy | @UsedBy: ProcesorPrac, KolejkaPracSql | @Task: TASK-128
namespace Lemat.Application.Common.BackgroundJobs;

public interface IKolejkaPrac
{
    /// <summary>Pobiera najpilniejszą dostępną pracę jednego z typów i przyznaje dzierżawę; <c>null</c>, gdy brak.</summary>
    Task<PracaWTle?> PobierzNastepnaAsync(IReadOnlyCollection<TypPracy> typy, string wykonawca, TimeSpan dzierzawa, CancellationToken ct);

    /// <summary>OCZEKUJE→W_TOKU→ZAKONCZONA; <c>false</c>, gdy dzierżawa została utracona.</summary>
    Task<bool> ZakonczAsync(long pracaId, string wykonawca, CancellationToken ct);

    /// <summary>Porażka próby: powrót do OCZEKUJE po <paramref name="odstep"/> albo MARTWA po wyczerpaniu prób; <c>null</c> przy utraconej dzierżawie.</summary>
    Task<StatusPracy?> PonowAsync(long pracaId, string wykonawca, string blad, TimeSpan odstep, CancellationToken ct);

    /// <summary>Zwraca do kolejki (lub oznacza MARTWE) prace z wygasłą dzierżawą; zwraca liczbę odzyskanych.</summary>
    Task<int> OdzyskajDzierzawyAsync(CancellationToken ct);
}
