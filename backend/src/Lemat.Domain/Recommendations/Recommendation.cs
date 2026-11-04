// @Role: Agregat Rekomendacja (niezmienny, FR-10, NFR-05): strategia, reguła, migawka cech, decyzja, przewidywane P sukcesu i uzasadnienie z faktami dla Ucznia | @DependsOn: Lemat.Domain.Common.Agregat, UczenId, ElementSciezkiId, StrategiaRekomendacji, RegulaRekomendacji | @UsedBy: IRekomendacjaRepozytorium, RekomendacjeFasada, Agent rekomendujący (planowane TASK-040) | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Recommendations;

public sealed class Rekomendacja : Agregat<RekomendacjaId>
{
    private Rekomendacja()
    {
    }

    public UczenId UczenId { get; private init; }

    /// <summary>Jedna Rekomendacja na element (UNIQUE).</summary>
    public ElementSciezkiId ElementSciezkiId { get; private init; }

    public StrategiaRekomendacji Strategia { get; private init; }

    public RegulaRekomendacji Regula { get; private init; }

    /// <summary>Migawka wejścia (10.1.4) jako dokument JSON (<c>snake_case</c>).</summary>
    public string CechyJson { get; private init; } = "{}";

    /// <summary>Kandydaci, wybór i <c>powrot_do_polityki</c> jako dokument JSON.</summary>
    public string DecyzjaJson { get; private init; } = "{}";

    public decimal? PrzewidywanePSukcesu { get; private init; }

    /// <summary>Tekst uzasadnienia dla Ucznia (FR-10), generowany deterministycznie z faktów (ADR-011).</summary>
    public string Uzasadnienie { get; private init; } = string.Empty;

    public string UzasadnienieFaktyJson { get; private init; } = "{}";

    public DateTimeOffset CreatedAt { get; private init; }

    public static Rekomendacja Utworz(
        RekomendacjaId id,
        UczenId uczen,
        ElementSciezkiId element,
        StrategiaRekomendacji strategia,
        RegulaRekomendacji regula,
        string cechyJson,
        string decyzjaJson,
        decimal? przewidywanePSukcesu,
        string uzasadnienie,
        string uzasadnienieFaktyJson,
        DateTimeOffset teraz)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cechyJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(decyzjaJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(uzasadnienieFaktyJson);
        if (string.IsNullOrWhiteSpace(uzasadnienie))
        {
            throw new NiezmiennikNaruszonyException("UZASADNIENIE_WYMAGANE", "Rekomendacja wymaga uzasadnienia dla Ucznia.");
        }

        if (przewidywanePSukcesu is < 0m or > 1m)
        {
            throw new NiezmiennikNaruszonyException("PRAWDOPODOBIENSTWO_POZA_ZAKRESEM", "Przewidywane prawdopodobieństwo sukcesu musi mieścić się w zakresie 0-1.");
        }

        return new Rekomendacja
        {
            Id = id,
            UczenId = uczen,
            ElementSciezkiId = element,
            Strategia = strategia,
            Regula = regula,
            CechyJson = cechyJson,
            DecyzjaJson = decyzjaJson,
            PrzewidywanePSukcesu = przewidywanePSukcesu is { } p ? ZaokraglijDoTysiecznych(p) : null,
            Uzasadnienie = uzasadnienie.Trim(),
            UzasadnienieFaktyJson = uzasadnienieFaktyJson,
            CreatedAt = teraz,
        };
    }

    private static decimal ZaokraglijDoTysiecznych(decimal p) => decimal.Round(p, 3, MidpointRounding.AwayFromZero);
}
