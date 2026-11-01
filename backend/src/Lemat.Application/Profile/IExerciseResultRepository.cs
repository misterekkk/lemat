// @Role: Port repozytorium Wyników zadania: dodawanie, oczekujące na Profil, unieważnione do odtworzenia, ważne chronologicznie, liczba prób w oknie 60 dni, wyszukiwanie po Ocenie | @DependsOn: Lemat.Domain.Profile.WynikZadania, UczenId, ZadanieId | @UsedBy: ProfilFasada, ExerciseResultRepositoryEf | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Content;
using Lemat.Domain.Profile;

namespace Lemat.Application.Profile;

public interface IWynikZadaniaRepozytorium
{
    void Dodaj(WynikZadania wynik);

    /// <summary>Ważne (nieunieważnione) Wyniki Ucznia niezastosowane jeszcze w Profilu, chronologicznie (<c>created_at</c>, <c>id</c>); ze śledzeniem.</summary>
    Task<IReadOnlyList<WynikZadania>> PobierzOczekujaceNaProfilAsync(UczenId uczen, CancellationToken ct);

    /// <summary>Unieważnione (zawieszone) Wyniki Ucznia, których unieważnienie nie zostało jeszcze uwzględnione w Profilu; ze śledzeniem.</summary>
    Task<IReadOnlyList<WynikZadania>> PobierzUniewaznioneNieprzetworzoneAsync(UczenId uczen, CancellationToken ct);

    /// <summary>Wszystkie ważne Wyniki Ucznia chronologicznie (<c>created_at</c>, <c>id</c>); ze śledzeniem.</summary>
    Task<IReadOnlyList<WynikZadania>> PobierzWazneAsync(UczenId uczen, CancellationToken ct);

    /// <summary>Wyniki (także unieważnione) powstałe z danej Oceny albo Odpowiedzi; ze śledzeniem.</summary>
    Task<IReadOnlyList<WynikZadania>> PobierzPoZrodleAsync(Guid? ocenaId, Guid? odpowiedzId, CancellationToken ct);

    /// <summary>Liczba ważnych Wyników Ucznia przy Zadaniu od wskazanej chwili (reguła <c>w_proba</c>, 60 dni).</summary>
    Task<int> LiczbaWaznychOdAsync(UczenId uczen, ZadanieId zadanie, DateTimeOffset od, CancellationToken ct);
}
