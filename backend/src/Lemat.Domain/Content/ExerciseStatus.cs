// @Role: Wyliczenie Status zadania (typ PostgreSQL status_zadania; maszyna stanów 8.4) | @DependsOn: — | @UsedBy: Lemat.Domain.Content.Zadanie, konfiguracje EF | @Task: TASK-017
namespace Lemat.Domain.Content;

public enum StatusZadania
{
    Walidacja,
    OdrzuconeWWalidacji,
    DoPrzegladu,
    Opublikowane,
    Odrzucone,
    Wycofane,
}
