// @Role: Wspólne opcje (de)serializacji ładunku pracy w tle (jsonb, klucze snake_case wg nazw ze słownika) | @DependsOn: System.Text.Json | @UsedBy: RozpoznajPismoHandler, OcenRozwiazanieHandler, ZatwierdzRozwiazanieHandler | @Task: TASK-128
using System.Text.Json;

namespace Lemat.Application.Common.BackgroundJobs;

public static class LadunekJson
{
    public static readonly JsonSerializerOptions Opcje = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}
