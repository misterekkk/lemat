// @Role: Katalog kodów błędów POC (HTTP, tytuł, domyślny opis, type) oraz komunikaty reguł walidacji | @DependsOn: KodBledu, Lemat.Domain.Common.KonwencjaNazw | @UsedBy: ProblemWriter, handlery błędów, mapowanie statusów | @Task: TASK-121, TASK-123
using System.Collections.Frozen;
using Lemat.Domain.Common;

namespace Lemat.Api.Errors;

/// <summary>Jedno źródło prawdy o kodach błędów API (kontrakt POC 1.2); test sprawdza kompletność względem <see cref="KodBledu"/>.</summary>
public static class ErrorCatalog
{
    public sealed record Wpis(int Status, string Tytul, string Opis);

    public const string PrefiksType = "https://lemat.pl/bledy/";

    private static readonly FrozenDictionary<KodBledu, Wpis> Wpisy = new Dictionary<KodBledu, Wpis>
    {
        [KodBledu.ZapytanieNieprawidlowe] = new(400, "Niepoprawne żądanie", "Treść żądania jest niepoprawna lub niekompletna."),
        [KodBledu.KursorNieprawidlowy] = new(400, "Niepoprawny kursor", "Kursor stronicowania jest niepoprawny. Wczytaj listę od początku."),
        [KodBledu.NaglowekNieprawidlowy] = new(400, "Niepoprawny nagłówek", "Wartość nagłówka ma niepoprawną formę."),
        [KodBledu.Nieuwierzytelniony] = new(401, "Wymagane logowanie", "Zaloguj się, aby kontynuować."),
        [KodBledu.SesjaUniewazniona] = new(401, "Sesja wygasła", "Twoja sesja wygasła. Zaloguj się ponownie."),
        [KodBledu.BrakUprawnien] = new(403, "Brak uprawnień", "Nie masz uprawnień do tej operacji."),
        [KodBledu.KontoNieaktywne] = new(403, "Konto nieaktywne", "To konto nie jest aktywne."),
        [KodBledu.NieZnaleziono] = new(404, "Nie znaleziono", "Nie znaleziono żądanego zasobu."),
        [KodBledu.MetodaNiedozwolona] = new(405, "Metoda niedozwolona", "Ta metoda HTTP nie jest dozwolona dla tego adresu."),
        [KodBledu.EmailZajety] = new(409, "Nie udało się założyć konta", "Nie udało się założyć konta na ten adres — zaloguj się albo zresetuj hasło."),
        [KodBledu.RozwiazanieNieprawidlowyStan] = new(409, "Nieprawidłowy stan rozwiązania", "Rozwiązanie jest w stanie, który nie pozwala na tę operację."),
        [KodBledu.TranskrypcjaNieaktualna] = new(409, "Transkrypcja jest nieaktualna", "Transkrypcja zmieniła się w międzyczasie. Odśwież widok."),
        [KodBledu.WersjaZadaniaNieaktualna] = new(409, "Treść zadania się zmieniła", "Treść zadania została zmieniona. Otwórz zadanie ponownie."),
        [KodBledu.KonfliktWersji] = new(412, "Dane zmieniły się w międzyczasie", "Dane zostały zmienione. Sprawdź aktualną wersję i zapisz ponownie."),
        [KodBledu.ZapytanieZaDuze] = new(413, "Żądanie zbyt duże", "Treść żądania przekracza dopuszczalny rozmiar."),
        [KodBledu.ZdjecieZaDuze] = new(413, "Zdjęcie jest zbyt duże", "Zdjęcie przekracza dopuszczalny rozmiar 10 MB."),
        [KodBledu.NieobslugiwanyFormat] = new(415, "Nieobsługiwany format", "Ten format nie jest obsługiwany."),
        [KodBledu.Walidacja] = new(422, "Błędne dane", "Popraw zaznaczone pola."),
        [KodBledu.ZaDuzoZdjec] = new(422, "Nieprawidłowa liczba zdjęć", "Prześlij od 1 do 5 zdjęć."),
        [KodBledu.ZdjecieNieprawidlowe] = new(422, "Nie można odczytać zdjęcia", "Nie można odczytać zdjęcia. Prześlij czytelny plik JPEG."),
        [KodBledu.WymaganyIfMatch] = new(428, "Wymagany nagłówek If-Match", "Operacja wymaga nagłówka If-Match z numerem wersji zasobu."),
        [KodBledu.BladWewnetrzny] = new(500, "Błąd wewnętrzny", "Wystąpił nieoczekiwany błąd. Spróbuj ponownie za chwilę."),
        [KodBledu.BazaNiedostepna] = new(503, "Usługa chwilowo niedostępna", "Usługa jest chwilowo niedostępna. Spróbuj ponownie za chwilę."),
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, string> KomunikatyRegul = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["WYMAGANE"] = "Pole jest wymagane.",
        ["ZA_KROTKIE"] = "Wartość jest zbyt krótka.",
        ["ZA_DLUGIE"] = "Wartość jest zbyt długa.",
        ["FORMAT_EMAIL"] = "Podaj poprawny adres e-mail.",
        ["WYMAGANE_OSWIADCZENIE"] = "Wymagane jest oświadczenie.",
        ["ROLA_NIEOBSLUGIWANA"] = "Ta rola nie jest obsługiwana.",
        ["POLE_NIEZNANE"] = "Nieznane pole.",
        ["TYP_NIEPRAWIDLOWY"] = "Wartość ma niepoprawny typ.",
        ["NIE_MOZNA_WYCZYSCIC"] = "Tego pola nie można wyczyścić.",
        ["DZIAL_NIEZNANY"] = "Nieznany dział.",
        ["PLIK_POZA_POLEM_ZDJECIA"] = "Pliki należy przesłać w polu zdjecia.",
        ["MOCK_SCENARIUSZ_NIEZNANY"] = "Nieznany scenariusz mocka.",
        ["WIAZANIE"] = "Brak wymaganej wartości lub wartość jest niepoprawna.",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static IReadOnlyCollection<KodBledu> WszystkieKody => Wpisy.Keys;

    public static Wpis Pobierz(KodBledu kod) => Wpisy[kod];

    /// <summary>Wartość <c>kod</c> w JSON (np. <c>ROZWIAZANIE_NIEPRAWIDLOWY_STAN</c>).</summary>
    public static string Nazwa(KodBledu kod) => KonwencjaNazw.PascalNaUpperSnake(kod.ToString());

    /// <summary>Wartość <c>type</c>: <c>https://lemat.pl/bledy/&lt;kod-kebab&gt;</c>.</summary>
    public static string Type(KodBledu kod) => PrefiksType + KonwencjaNazw.PascalNaKebab(kod.ToString());

    public static string KomunikatReguly(string kodReguly) =>
        KomunikatyRegul.TryGetValue(kodReguly, out var komunikat) ? komunikat : "Wartość jest niepoprawna.";

    /// <summary>Kod z wyjątku domenowego (UPPER_SNAKE) → <see cref="KodBledu"/>; <c>false</c>, gdy kod spoza podzbioru POC.</summary>
    public static bool TryParse(string? kodDomenowy, out KodBledu kod)
    {
        kod = default;
        return !string.IsNullOrWhiteSpace(kodDomenowy)
            && kodDomenowy.All(c => char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c) || c == '_')
            && Enum.TryParse(KonwencjaNazw.UpperSnakeNaPascal(kodDomenowy), ignoreCase: false, out kod)
            && Wpisy.ContainsKey(kod);
    }

    /// <summary>Kod błędu dla statusu HTTP bez ciała (404/405/415 z routingu, 401/403 z uwierzytelniania, ...).</summary>
    public static KodBledu DlaStatusu(int status) => status switch
    {
        400 => KodBledu.ZapytanieNieprawidlowe,
        401 => KodBledu.Nieuwierzytelniony,
        403 => KodBledu.BrakUprawnien,
        404 => KodBledu.NieZnaleziono,
        405 => KodBledu.MetodaNiedozwolona,
        412 => KodBledu.KonfliktWersji,
        413 => KodBledu.ZapytanieZaDuze,
        415 => KodBledu.NieobslugiwanyFormat,
        422 => KodBledu.Walidacja,
        428 => KodBledu.WymaganyIfMatch,
        503 => KodBledu.BazaNiedostepna,
        _ => KodBledu.BladWewnetrzny,
    };
}
