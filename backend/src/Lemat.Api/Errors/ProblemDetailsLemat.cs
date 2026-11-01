// @Role: Kształt odpowiedzi błędu RFC 9457 (type, title, status, detail, instance, kod, trace_id, szczegoly, bledy_pol) | @DependsOn: KodBledu, BladPola | @UsedBy: ProblemWriter, handlery błędów, testy | @Task: TASK-121
using System.Text.Json.Serialization;

namespace Lemat.Api.Errors;

/// <summary>Treść <c>application/problem+json</c> (kontrakt POC 1.1); <c>bledy_pol</c> występuje tylko dla 422 WALIDACJA.</summary>
public sealed record ProblemDetailsLemat(
    string Type,
    string Title,
    int Status,
    string Detail,
    string Instance,
    KodBledu Kod,
    string TraceId,
    IReadOnlyDictionary<string, object?>? Szczegoly,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BladPola>? BledyPol);
