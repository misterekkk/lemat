// @Role: Opis reguły warstwy (6.5): projekty, zabronione zespoły/pakiety i przestrzenie nazw | @DependsOn: brak | @UsedBy: LayerRules, LayerRulesTests | @Task: TASK-002
namespace Lemat.Architecture.Tests.Layers;

/// <summary>
/// Reguła warstwy. Wzorzec kończący się znakiem * oznacza prefiks, pozostałe wzorce wymagają dokładnej nazwy zespołu lub pakietu.
/// </summary>
/// <param name="Name">Nazwa reguły (6.5).</param>
/// <param name="Projects">Projekty, których dotyczy reguła.</param>
/// <param name="ForbiddenAssemblies">Zabronione zależności na poziomie zespołów, referencji projektów i pakietów.</param>
/// <param name="ForbiddenNamespaces">Zabronione przestrzenie nazw sprawdzane NetArchTest na realnych typach.</param>
/// <param name="NamespaceScope">Opcjonalny prefiks przestrzeni nazw typów objętych sprawdzeniem typów.</param>
internal sealed record LayerRule(
    string Name,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> ForbiddenAssemblies,
    IReadOnlyList<string> ForbiddenNamespaces,
    string? NamespaceScope = null);
