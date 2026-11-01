// @Role: Dział (13 Działów matury, dane referencyjne A-04): kod rzymski, nazwa, flaga tylko rozszerzony, kolejność | @DependsOn: Lemat.Domain.Content.DzialId | @UsedBy: ITresciFasada, repozytoria Treści, moduły Profil, Rekomendacje | @Task: TASK-017
namespace Lemat.Domain.Content;

/// <summary>Agregat referencyjny (bez <c>version</c> — tabela <c>dzial</c> jest ładowana migracją danych).</summary>
public sealed class Dzial
{
    private Dzial()
    {
    }

    public DzialId Id { get; private init; }

    /// <summary>Numer rzymski, np. <c>XIII</c>.</summary>
    public string Kod { get; private init; } = string.Empty;

    public string Nazwa { get; private init; } = string.Empty;

    public bool TylkoRozszerzony { get; private init; }

    public int Kolejnosc { get; private init; }

    public static Dzial Utworz(DzialId id, string kod, string nazwa, bool tylkoRozszerzony, int kolejnosc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kod);
        ArgumentException.ThrowIfNullOrWhiteSpace(nazwa);
        return new Dzial { Id = id, Kod = kod, Nazwa = nazwa, TylkoRozszerzony = tylkoRozszerzony, Kolejnosc = kolejnosc };
    }
}
