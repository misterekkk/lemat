// @Role: Nazwy kolekcji xUnit serializujących testy modyfikujące wspólne dane startowe POC (Zadania, wersje) | @DependsOn: — | @UsedBy: Content/ExercisesEndpointsTests, Solutions/SolutionsEndpointsTests | @Task: TASK-127
namespace Lemat.Api.Tests.Support;

public static class KolekcjeTestowe
{
    /// <summary>Testy zmieniające dane Zadań z seed-poc (status, wersja aktualna) nie mogą działać równolegle z testami listy Zadań.</summary>
    public const string DanePoc = "Dane POC";
}
