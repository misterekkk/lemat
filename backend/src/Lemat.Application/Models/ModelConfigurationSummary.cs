// @Role: Rekord odczytu KonfiguracjaModeluSkrot (konfiguracja modelu bez szczegółów trwałości) i wejście ZapiszKonfiguracjeWejscie | @DependsOn: Lemat.Domain.Models | @UsedBy: IModeleFasada, ModeleFasada, Api (planowane TASK-071) | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Models;

namespace Lemat.Application.Models;

public sealed record KonfiguracjaModeluSkrot(
    KonfiguracjaModeluId Id,
    RolaModelu Rola,
    string Dostawca,
    string Model,
    string WersjaPromptu,
    bool Aktywna,
    ParametryModelu Parametry)
{
    public static KonfiguracjaModeluSkrot Z(KonfiguracjaModelu k) =>
        new(k.Id, k.Rola, k.Dostawca, k.Model, k.WersjaPromptu, k.Aktywna, k.Parametry);
}

public sealed record NowaKonfiguracjaModelu(
    RolaModelu Rola,
    string Dostawca,
    string Model,
    ParametryModelu Parametry,
    string WersjaPromptu,
    UzytkownikId? UtworzonaPrzez);
