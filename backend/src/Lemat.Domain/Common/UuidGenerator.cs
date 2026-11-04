// @Role: Generator identyfikatorów UUIDv7 (uporządkowanych czasowo) z TimeProvider | @DependsOn: — | @UsedBy: fabryki agregatów wszystkich modułów | @Task: TASK-014
namespace Lemat.Domain.Common;

public static class GeneratorUuid
{
    public static Guid Nowy(TimeProvider czas)
    {
        ArgumentNullException.ThrowIfNull(czas);
        return Guid.CreateVersion7(czas.GetUtcNow());
    }
}
