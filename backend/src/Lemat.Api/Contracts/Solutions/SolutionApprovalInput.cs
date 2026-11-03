// @Role: DTO wejścia POST /rozwiazania/{id}/zatwierdzenie (kontrakt POC 7.7): zatwierdzana Transkrypcja i jej wersja | @DependsOn: — | @UsedBy: EndpointyRozwiazan, ZatwierdzenieRozwiazaniaWejscieWalidator | @Task: TASK-128
namespace Lemat.Api.Contracts.Solutions;

/// <param name="TranskrypcjaId">Musi być równe <c>najnowsza_transkrypcja_id</c> Rozwiązania.</param>
/// <param name="Wersja">Wersja Transkrypcji (≥ 1), musi być najnowsza.</param>
public sealed record ZatwierdzenieRozwiazaniaWejscie(Guid? TranskrypcjaId, int? Wersja);
