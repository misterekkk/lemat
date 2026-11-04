// @Role: Wyjątek warstwy HTTP niosący kod błędu API, szczegóły i błędy pól (np. 412 KONFLIKT_WERSJI, 400 KURSOR_NIEPRAWIDLOWY, 422 WALIDACJA) | @DependsOn: KodBledu, BladPola | @UsedBy: NaglowekIfMatch, KodekKursora, FiltrScenariuszaMock, ObslugaWyjatkowDomenowych | @Task: TASK-121
namespace Lemat.Api.Errors;

#pragma warning disable CA1032 // Wyjątek tworzony wyłącznie przez fabryki/konstruktor z kodem błędu.
/// <summary>Błąd zgłaszany przez warstwę HTTP; mapowany na ProblemDetails przez <see cref="ObslugaWyjatkowDomenowych"/>.</summary>
public sealed class BladApiException(
    KodBledu kod,
    string? szczegolyOpis = null,
    IReadOnlyDictionary<string, object?>? szczegoly = null,
    IReadOnlyList<BladPola>? bledyPol = null) : Exception(szczegolyOpis ?? kod.ToString())
#pragma warning restore CA1032
{
    public KodBledu Kod { get; } = kod;

    public string? Opis { get; } = szczegolyOpis;

    public IReadOnlyDictionary<string, object?>? Szczegoly { get; } = szczegoly;

    public IReadOnlyList<BladPola>? BledyPol { get; } = bledyPol;

    public static BladApiException Walidacja(string pole, string kodReguly, string komunikat) =>
        new(KodBledu.Walidacja, null, null, [new BladPola(pole, kodReguly, komunikat)]);
}
