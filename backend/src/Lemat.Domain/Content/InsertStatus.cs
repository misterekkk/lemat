// @Role: Wyliczenie Status wkładki (typ PostgreSQL status_wkladki) | @DependsOn: — | @UsedBy: Lemat.Domain.Content.WkladkaTeoretyczna, konfiguracje EF | @Task: TASK-017
namespace Lemat.Domain.Content;

public enum StatusWkladki
{
    Szkic,
    DoPrzegladu,
    Opublikowana,
    Wycofana,
}
