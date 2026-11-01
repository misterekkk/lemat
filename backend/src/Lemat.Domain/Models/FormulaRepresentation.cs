// @Role: Wyliczenie Reprezentacja wzorów w stanie przekazywanym modelowi (parametry.reprezentacja_wzorow) | @DependsOn: — | @UsedBy: ParametryModelu, konwerter jsonb | @Task: TASK-018
namespace Lemat.Domain.Models;

public enum ReprezentacjaWzorow
{
    Latex,
    LiniowaSympy,
    Oba,
}
