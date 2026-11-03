// @Role: Źródło Transkrypcji (typ PostgreSQL zrodlo_transkrypcji): odczyt Agenta rozpoznającego albo Poprawka Ucznia | @DependsOn: — | @UsedBy: Transkrypcja, konfiguracja EF, MapowanieTypowWyliczeniowych | @Task: TASK-126
namespace Lemat.Domain.Solutions;

public enum ZrodloTranskrypcji
{
    AgentRozpoznajacy,
    PoprawkaUcznia,
}
