// @Role: DTO ZmianaUczniaWejscie (PATCH /uczniowie/ja): JSON Merge Patch przez Opcjonalne<T> (ADR-072) | @DependsOn: Opcjonalne<T>, Lemat.Domain.Common.PoziomMatury | @UsedBy: EndpointyUcznia | @Task: TASK-124
using Lemat.Api.Contracts.Common;
using Lemat.Domain.Common;

namespace Lemat.Api.Contracts.Accounts;

public sealed record ZmianaUczniaWejscie
{
    /// <summary>null -> 422 NIE_MOZNA_WYCZYSCIC.</summary>
    public Opcjonalne<PoziomMatury?> PoziomMatury { get; init; }

    /// <summary>null -> brak terminu.</summary>
    public Opcjonalne<DateOnly?> TerminEgzaminu { get; init; }

    /// <summary>null -> wszystkie Działy.</summary>
    public Opcjonalne<int?> WybranyDzialId { get; init; }

    /// <summary>null -> 422 NIE_MOZNA_WYCZYSCIC.</summary>
    public Opcjonalne<bool?> ZgodaNaTrening { get; init; }
}
