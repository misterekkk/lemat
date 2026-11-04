// @Role: Wyliczenie Pochodzenie zadania (typ PostgreSQL pochodzenie_zadania) | @DependsOn: — | @UsedBy: Lemat.Domain.Content.Zadanie, konfiguracje EF | @Task: TASK-017
namespace Lemat.Domain.Content;

public enum PochodzenieZadania
{
    BazoweArkuszCke,
    BazoweNauczyciel,
    Wygenerowane,
}
