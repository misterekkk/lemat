// @Role: Konwertery identyfikatorów silnie typowanych (readonly record struct XxxId : IIdentyfikator<Guid|int>) rejestrowane konwencją modelu (ef-core.md 3.1) | @DependsOn: Lemat.Domain.Common.IIdentyfikator, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext, testy | @Task: TASK-015
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Lemat.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lemat.Infrastructure.Persistence.Conventions;

/// <summary>Konwerter <c>XxxId</c> &lt;-&gt; <c>Guid</c>/<c>int</c>; konstruktor kompilowany raz, bez refleksji na wywołanie.</summary>
public sealed class IdentyfikatorConverter<TId, TWartosc>()
    : ValueConverter<TId, TWartosc>(
        id => id.Wartosc,
        wartosc => FabrykaIdentyfikatora<TId, TWartosc>.Utworz(wartosc))
    where TId : struct, IIdentyfikator<TWartosc>
    where TWartosc : struct;

internal static class FabrykaIdentyfikatora<TId, TWartosc>
    where TId : struct, IIdentyfikator<TWartosc>
    where TWartosc : struct
{
    public static readonly Func<TWartosc, TId> Utworz = Zbuduj();

    private static Func<TWartosc, TId> Zbuduj()
    {
        var konstruktor = typeof(TId).GetConstructor([typeof(TWartosc)])
            ?? throw new InvalidOperationException($"Identyfikator {typeof(TId).Name} nie ma konstruktora ({typeof(TWartosc).Name}).");
        var parametr = Expression.Parameter(typeof(TWartosc), "wartosc");
        return Expression.Lambda<Func<TWartosc, TId>>(Expression.New(konstruktor, parametr), parametr).Compile();
    }
}

public static class KonwersjeIdentyfikatorow
{
    private static readonly ConcurrentDictionary<Assembly, byte> DodatkoweZespoly = new();

    /// <summary>Dodaje zespół z identyfikatorami spoza <c>Lemat.Domain</c> (np. testy).</summary>
    public static void DodajZespol(Assembly zespol)
    {
        ArgumentNullException.ThrowIfNull(zespol);
        DodatkoweZespoly.TryAdd(zespol, 0);
    }

    /// <summary>Typy <c>*Id</c> (value type implementujący <see cref="IIdentyfikator{TWartosc}"/>) z domeny i zespołów dodatkowych.</summary>
    public static IReadOnlyList<(Type TypId, Type TypWartosci)> ZnajdzIdentyfikatory() =>
        [.. new[] { typeof(IIdentyfikator<>).Assembly }
            .Concat(DodatkoweZespoly.Keys)
            .Distinct()
            .SelectMany(z => z.GetTypes())
            .Where(t => t is { IsValueType: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Select(t => (TypId: t, Interfejs: t.GetInterfaces().FirstOrDefault(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIdentyfikator<>))))
            .Where(p => p.Interfejs is not null)
            .Select(p => (p.TypId, p.Interfejs!.GetGenericArguments()[0]))
            .OrderBy(p => p.TypId.FullName, StringComparer.Ordinal)];

    public static void Zarejestruj(ModelConfigurationBuilder konwencje)
    {
        ArgumentNullException.ThrowIfNull(konwencje);
        foreach (var (typId, typWartosci) in ZnajdzIdentyfikatory())
        {
            var konwerter = typeof(IdentyfikatorConverter<,>).MakeGenericType(typId, typWartosci);
            konwencje.Properties(typId).HaveConversion(konwerter);
        }
    }
}
