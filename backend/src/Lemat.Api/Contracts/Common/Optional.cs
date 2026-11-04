// @Role: Opcjonalne<T> dla rekordów PATCH (JSON Merge Patch, ADR-072): Obecne = false gdy pole nieobecne, Obecne = true i Wartosc = null dla jawnego null | @DependsOn: OpcjonalneKonwerter | @UsedBy: ZmianaUczniaWejscie, kolejne rekordy PATCH | @Task: TASK-124
using System.Text.Json.Serialization;

namespace Lemat.Api.Contracts.Common;

[JsonConverter(typeof(OpcjonalneKonwerter))]
public readonly record struct Opcjonalne<T>(bool Obecne, T? Wartosc);
