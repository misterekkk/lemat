// @Role: Źródło Wskazówki (typ PostgreSQL zrodlo_wskazowki): Agent wyjaśniający, szablon albo Nauczyciel | @DependsOn: — | @UsedBy: Wskazowka, konfiguracja EF, MapowanieTypowWyliczeniowych | @Task: TASK-126
namespace Lemat.Domain.Grading;

public enum ZrodloWskazowki
{
    AgentWyjasniajacy,
    Szablon,
    Nauczyciel,
}
