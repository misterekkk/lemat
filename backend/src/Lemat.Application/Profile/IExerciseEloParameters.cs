// @Role: Port parametrów Zadań dla modelu Elo (Umiejętności, trudnosc_b, szansa odgadnięcia) — adapter nad danymi Treści | @DependsOn: ZadanieId, ParametryEloZadania | @UsedBy: ProfilFasada, ExerciseEloParametersEf | @Task: TASK-019
using Lemat.Domain.Content;
using Lemat.Domain.Profile;

namespace Lemat.Application.Profile;

public interface IParametryZadaniaDlaProfilu
{
    /// <summary>Parametry Elo dla wskazanych Zadań; Zadania nieistniejące są pominięte w słowniku.</summary>
    Task<IReadOnlyDictionary<ZadanieId, ParametryEloZadania>> PobierzAsync(IReadOnlyCollection<ZadanieId> zadania, CancellationToken ct);
}
