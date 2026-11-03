// @Role: DTO Zdjęcia rozwiązania w odpowiedzi API (ZdjecieRozwiazaniaWyjscie, kontrakt POC 7.4); bez klucza obiektu | @DependsOn: — | @UsedBy: RozwiazanieWyjscie, EndpointyRozwiazan | @Task: TASK-127
namespace Lemat.Api.Contracts.Solutions;

public sealed record ZdjecieRozwiazaniaWyjscie(Guid Id, short PrzeslanieNr, short Kolejnosc, bool Dostepne);
