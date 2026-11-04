// @Role: Źródło Wyniku zadania (ODPOWIEDZ dla Zadań zamkniętych i Testu diagnostycznego, OCENA dla otwartych) | @DependsOn: — | @UsedBy: Lemat.Domain.Profile.WynikZadania, konfiguracja EF | @Task: TASK-019
namespace Lemat.Domain.Profile;

public enum ZrodloWyniku
{
    Odpowiedz,
    Ocena,
}
