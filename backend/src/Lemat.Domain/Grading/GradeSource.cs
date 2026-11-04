// @Role: Źródło Oceny (typ PostgreSQL zrodlo_oceny): Model decyzyjny (automatyczna) albo Nauczyciel | @DependsOn: — | @UsedBy: Ocena, konfiguracja EF, MapowanieTypowWyliczeniowych | @Task: TASK-126
namespace Lemat.Domain.Grading;

public enum ZrodloOceny
{
    Automatyczna,
    Nauczyciel,
}
