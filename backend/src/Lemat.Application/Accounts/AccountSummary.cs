// @Role: Skrót konta udostępniany innym modułom przez IKontaFasada (rola i status; bez skrótu hasła) | @DependsOn: Lemat.Domain.Accounts | @UsedBy: IKontaFasada, moduły Klasy, Weryfikacje, Rekomendacje | @Task: TASK-016
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public sealed record KontoSkrot(UzytkownikId UzytkownikId, RolaUzytkownika Rola, StatusKonta Status, int Version);
