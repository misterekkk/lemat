// @Role: Polityka nazw JSON: snake_case/UPPER_SNAKE z separacją cyfr (oswiadczenie_wieku_16, liczba_wynikow_7d) | @DependsOn: Lemat.Domain.Common.KonwencjaNazw | @UsedBy: JsonApi | @Task: TASK-121
using System.Text.Json;
using Lemat.Domain.Common;

namespace Lemat.Api.Configuration;

/// <summary>Opakowuje wbudowaną politykę snake_case i dokłada separator przed liczbą (ADR-049 p. 14).</summary>
public sealed class NazwyLemat(JsonNamingPolicy baza) : JsonNamingPolicy
{
    public override string ConvertName(string name) => KonwencjaNazw.RozdzielCyfry(baza.ConvertName(name));
}
