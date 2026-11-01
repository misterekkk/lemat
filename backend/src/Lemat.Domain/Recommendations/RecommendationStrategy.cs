// @Role: Strategia Rekomendacji (POLITYKA_ELO w MVP, JEV_CHOICE po MVP) | @DependsOn: — | @UsedBy: Rekomendacja, konfiguracja EF | @Task: TASK-019
namespace Lemat.Domain.Recommendations;

public enum StrategiaRekomendacji
{
    PolitykaElo,
    JevChoice,
}
