// @Role: Wyliczenie Rola modelu (typ PostgreSQL rola_modelu, 8 ról): które zadanie pełni skonfigurowany model | @DependsOn: — | @UsedBy: KonfiguracjaModelu, IModeleFasada, rejestr adapterów (planowane TASK-054) | @Task: TASK-018
namespace Lemat.Domain.Models;

public enum RolaModelu
{
    DecyzyjnyGlowny,
    DecyzyjnyZapasowy,
    RozpoznawaniePisma,
    GeneratywnyZadania,
    GeneratywnyRozwiazujacyNiezaleznie,
    GeneratywnyWskazowki,
    GeneratywnyWkladki,
    GeneratywnyOpiniaPomocnicza,
}
