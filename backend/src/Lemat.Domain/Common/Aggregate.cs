// @Role: Korzeń agregatu: Version (token współbieżności), kolekcja zdarzeń domenowych (3.6) | @DependsOn: Lemat.Domain.Common.IZdarzenieDomenowe | @UsedBy: agregaty wszystkich modułów domenowych, LematDbContext (TASK-015) | @Task: TASK-014, TASK-015
namespace Lemat.Domain.Common;

/// <summary>Niegeneryczny kontrakt korzenia agregatu dla warstwy trwałości (token współbieżności, zdarzenia).</summary>
public interface IKorzenAgregatu
{
    int Version { get; }

    IReadOnlyList<IZdarzenieDomenowe> Zdarzenia { get; }

    void PotwierdzZapis();
}

public abstract class Agregat<TId> : IKorzenAgregatu
    where TId : struct
{
    private readonly List<IZdarzenieDomenowe> _zdarzenia = [];

    public TId Id { get; protected init; }

    public int Version { get; private set; } = 1;

    public IReadOnlyList<IZdarzenieDomenowe> Zdarzenia => _zdarzenia;

    protected void Zglos(IZdarzenieDomenowe zdarzenie)
    {
        ArgumentNullException.ThrowIfNull(zdarzenie);
        _zdarzenia.Add(zdarzenie);
    }

    public void WyczyscZdarzenia() => _zdarzenia.Clear();

    /// <summary>
    /// Wywoływane przez warstwę trwałości (LematDbContext) przy zapisie agregatu zmienionego
    /// przez człowieka albo przejście maszyny stanów (3.6 p. 3); zwiększa token współbieżności.
    /// </summary>
    public void PotwierdzZapis() => Version++;
}
