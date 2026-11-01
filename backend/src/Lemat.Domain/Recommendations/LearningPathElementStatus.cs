// @Role: Status Elementu Ścieżki nauki wg maszyny stanów 8.5 | @DependsOn: — | @UsedBy: ElementSciezki, SciezkaNauki, konfiguracja EF | @Task: TASK-019
namespace Lemat.Domain.Recommendations;

public enum StatusElementuSciezki
{
    Proponowany,
    Rozpoczety,
    Ukonczony,
    Pominiety,
    Wygasly,
}
