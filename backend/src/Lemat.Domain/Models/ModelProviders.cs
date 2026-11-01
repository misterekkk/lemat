// @Role: Klucze adapterów dostawców modeli zapisywane w konfiguracja_modelu.dostawca (rejestr adapterów 13.1; anthropic jako dodatek U-13) | @DependsOn: — | @UsedBy: KonfiguracjaModelu, walidacja Konfiguracji modelu, RejestrAdapterow (planowane TASK-054) | @Task: TASK-018
namespace Lemat.Domain.Models;

public static class DostawcyModeli
{
    public const string TypesafeJev = "typesafe-jev";
    public const string ZapasowyLlm = "zapasowy-llm";
    public const string AzureOpenAi = "azure-openai";
    public const string OpenAiZgodny = "openai-zgodny";
    public const string Anthropic = "anthropic";
    public const string Mock = "mock";

    public static readonly IReadOnlyList<string> Wszyscy = [TypesafeJev, ZapasowyLlm, AzureOpenAi, OpenAiZgodny, Anthropic, Mock];
}
