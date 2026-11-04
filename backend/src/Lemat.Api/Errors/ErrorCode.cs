// @Role: Podzbiór katalogu kodów błędów (12.2) używany w POC; wartość JSON = UPPER_SNAKE nazwy członka | @DependsOn: — | @UsedBy: ErrorCatalog, ProblemDetailsLemat, BladApiException, handlery błędów | @Task: TASK-121
namespace Lemat.Api.Errors;

/// <summary>Kody błędów API (kontrakt POC 1.2). Kolejne zadania dopisują członki wraz z wpisem w <see cref="ErrorCatalog"/>.</summary>
public enum KodBledu
{
    ZapytanieNieprawidlowe,
    KursorNieprawidlowy,
    NaglowekNieprawidlowy,
    Nieuwierzytelniony,
    SesjaUniewazniona,
    BrakUprawnien,
    KontoNieaktywne,
    NieZnaleziono,
    MetodaNiedozwolona,
    EmailZajety,
    RozwiazanieNieprawidlowyStan,
    TranskrypcjaNieaktualna,
    WersjaZadaniaNieaktualna,
    KonfliktWersji,
    ZapytanieZaDuze,
    ZdjecieZaDuze,
    NieobslugiwanyFormat,
    Walidacja,
    ZaDuzoZdjec,
    ZdjecieNieprawidlowe,
    WymaganyIfMatch,
    BladWewnetrzny,
    BazaNiedostepna,
}
