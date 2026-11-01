// @Role: Syntetyczny typ naruszający regułę domeny (zależność od System.Net.Http) — wyłącznie materiał testu negatywnego | @DependsOn: System.Net.Http | @UsedBy: LayerRulesTests | @Task: TASK-002
namespace Lemat.Architecture.Tests.Layers.Synthetic;

public sealed class SyntheticDomainType
{
    public HttpClient? Client { get; set; }
}
