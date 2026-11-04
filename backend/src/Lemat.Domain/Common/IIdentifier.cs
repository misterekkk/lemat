// @Role: Baza identyfikatorów silnie typowanych (readonly record struct <Korzen>Id; każdy typ w TASK modułu) | @DependsOn: — | @UsedBy: Lemat.Domain.Common.Agregat, konwertery EF (TASK-015) | @Task: TASK-014
namespace Lemat.Domain.Common;

public interface IIdentyfikator<out TWartosc>
    where TWartosc : struct
{
    TWartosc Wartosc { get; }
}
