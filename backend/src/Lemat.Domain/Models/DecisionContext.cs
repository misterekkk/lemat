// @Role: Wyliczenie Kontekst decyzji (typ PostgreSQL kontekst_decyzji): do czego służy Decyzja modelu | @DependsOn: — | @UsedBy: DecyzjaModelu, IModelDecyzyjny (planowane TASK-054) | @Task: TASK-018
namespace Lemat.Domain.Models;

public enum KontekstDecyzji
{
    Ocena,
    Rekomendacja,
    WalidacjaZadania,
}
