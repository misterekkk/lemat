// @Role: Encja wewnętrzna Członkostwo klasy: Uczeń w Klasie od chwili dołączenia (klucz klasa_id + uczen_id = unikalność członkostwa) | @DependsOn: Lemat.Domain.Accounts.UczenId | @UsedBy: Lemat.Domain.Classes.Klasa, konfiguracja EF | @Task: TASK-018
using Lemat.Domain.Accounts;

namespace Lemat.Domain.Classes;

public sealed record CzlonkostwoKlasy(UczenId UczenId, DateTimeOffset DolaczonoAt);
