// @Role: Parametry stronicowania (?limit=1..100, domyślnie 50; ?kursor) z walidacją do 422 WALIDACJA | @DependsOn: BladApiException | @UsedBy: endpointy list (TASK-125, TASK-127) | @Task: TASK-121
using Lemat.Api.Errors;

namespace Lemat.Api.Pagination;

/// <summary>Zweryfikowane parametry listy.</summary>
public sealed record ZapytanieStrony(int Limit, string? Kursor)
{
    public const int DomyslnyLimit = 50;
    public const int MaksymalnyLimit = 100;

    public static ZapytanieStrony Z(int? limit, string? kursor)
    {
        if (limit is < 1 or > MaksymalnyLimit)
        {
            throw BladApiException.Walidacja("limit", "TYP_NIEPRAWIDLOWY", $"Limit musi mieścić się w zakresie 1…{MaksymalnyLimit}.");
        }

        return new ZapytanieStrony(limit ?? DomyslnyLimit, string.IsNullOrEmpty(kursor) ? null : kursor);
    }
}
