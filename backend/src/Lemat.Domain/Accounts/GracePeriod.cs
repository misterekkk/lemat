// @Role: Obiekt wartości Okres karencji (ADR-040): koniec 30-dniowej karencji usunięcia konta i status odtwarzany przy przywróceniu | @DependsOn: Lemat.Domain.Accounts.StatusKonta, Lemat.Domain.Common.NiezmiennikNaruszonyException | @UsedBy: Lemat.Domain.Accounts.Uzytkownik, konfiguracja EF uzytkownik | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public sealed record OkresKarencji
{
    public const int LiczbaDni = 30;

    public OkresKarencji(DateTimeOffset usuniecieZaplanowaneAt, StatusKonta statusPrzedWnioskiem)
    {
        if (statusPrzedWnioskiem == StatusKonta.Usuwane)
        {
            throw new NiezmiennikNaruszonyException("STATUS_PRZED_WNIOSKIEM", "Status sprzed wniosku nie może być USUWANE.");
        }

        UsuniecieZaplanowaneAt = usuniecieZaplanowaneAt;
        StatusPrzedWnioskiem = statusPrzedWnioskiem;
    }

    public DateTimeOffset UsuniecieZaplanowaneAt { get; }

    public StatusKonta StatusPrzedWnioskiem { get; }

    public static OkresKarencji Rozpocznij(DateTimeOffset teraz, StatusKonta statusPrzedWnioskiem) =>
        new(teraz.AddDays(LiczbaDni), statusPrzedWnioskiem);
}
