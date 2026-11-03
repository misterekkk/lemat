// @Role: Wyjątek odrzucenia zdjęcia przez obróbkę obrazów; kod to kod błędu API (ZDJECIE_ZA_DUZE, NIEOBSLUGIWANY_FORMAT, ZDJECIE_NIEPRAWIDLOWE) | @DependsOn: — | @UsedBy: IObrobkaObrazow, ProstaObrobkaObrazow, PrzeslijRozwiazanieHandler | @Task: TASK-127
namespace Lemat.Application.Models;

#pragma warning disable CA1032 // Wyjątek tworzony wyłącznie z kodem błędu.
public sealed class ZdjecieNieprawidloweException(string kod) : Exception(kod)
#pragma warning restore CA1032
{
    public const string KodZaDuze = "ZDJECIE_ZA_DUZE";
    public const string KodNieobslugiwanyFormat = "NIEOBSLUGIWANY_FORMAT";
    public const string KodNieprawidlowe = "ZDJECIE_NIEPRAWIDLOWE";

    public string Kod { get; } = kod;
}
