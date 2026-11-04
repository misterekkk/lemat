// @Role: Wersja Zadania (encja niezmienna po utworzeniu, ADR-012): treść, opcje zamknięte, Rozwiązanie wzorcowe, Wynik końcowy wzorcowy, punkty, Kryteria oceniania; zastępowana nową wersją (zastapiona_at) | @DependsOn: Lemat.Domain.Content (OpcjeZamkniete, RozwiazanieWzorcowe, WynikKoncowyWzorcowy, KryteriumOceniania, ZasadyKryteriow), RodzajZadania | @UsedBy: Lemat.Domain.Content.Zadanie, konfiguracje EF, ITresciFasada | @Task: TASK-017
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

/// <summary>Treść nowej wersji Zadania (wejście <see cref="Zadanie.Utworz"/> i <see cref="Zadanie.Popraw"/>).</summary>
public sealed record TrescWersji(
    string TrescMd,
    OpcjeZamkniete? OpcjeZamkniete,
    RozwiazanieWzorcowe RozwiazanieWzorcowe,
    WynikKoncowyWzorcowy? WynikKoncowyWzorcowy,
    int PunktyMax,
    IReadOnlyList<KryteriumOceniania> Kryteria);

public sealed class ZadanieWersja
{
    public const int MaksDlugoscTresci = 8000;
    public const int MaksPunkty = 6;

    private readonly List<KryteriumOceniania> _kryteria = [];

    private ZadanieWersja()
    {
    }

    public ZadanieWersjaId Id { get; private init; }

    /// <summary>&gt;= 1, unikalny w Zadaniu.</summary>
    public int NrWersji { get; private init; }

    public string TrescMd { get; private init; } = string.Empty;

    /// <summary>Różne od <c>null</c> dla Zadania zamkniętego (7.5.1).</summary>
    public OpcjeZamkniete? OpcjeZamkniete { get; private init; }

    public RozwiazanieWzorcowe RozwiazanieWzorcowe { get; private init; } = null!;

    /// <summary><c>null</c> wyłącznie dla dowodu i Zadania zamkniętego (7.5.3).</summary>
    public WynikKoncowyWzorcowy? WynikKoncowyWzorcowy { get; private init; }

    public int PunktyMax { get; private init; }

    public UzytkownikId? UtworzonaPrzez { get; private init; }

    /// <summary>Chwila, w której wersja przestała być aktualną.</summary>
    public DateTimeOffset? ZastapionaAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public IReadOnlyList<KryteriumOceniania> KryteriaOceniania => _kryteria;

    internal static ZadanieWersja Utworz(
        ZadanieWersjaId id,
        int nr,
        RodzajZadania rodzaj,
        FormaZamknieta? forma,
        TrescWersji tresc,
        UzytkownikId? autor,
        DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(tresc);
        if (nr < 1)
        {
            throw new NiezmiennikNaruszonyException("WERSJA_NR_NIEPRAWIDLOWY", "Numer wersji musi być dodatni.");
        }

        if (string.IsNullOrWhiteSpace(tresc.TrescMd) || tresc.TrescMd.Length > MaksDlugoscTresci)
        {
            throw new NiezmiennikNaruszonyException("TRESC_ZADANIA_NIEPRAWIDLOWA", "Treść Zadania ma mieć 1-8000 znaków.");
        }

        if (tresc.PunktyMax is < 1 or > MaksPunkty)
        {
            throw new NiezmiennikNaruszonyException("PUNKTY_ZADANIA_POZA_ZAKRESEM", "Zadanie ma 1-6 punktów.");
        }

        SprawdzSpojnoscRodzaju(rodzaj, forma, tresc);
        ZasadyKryteriow.Sprawdz(rodzaj, tresc.PunktyMax, [.. tresc.Kryteria]);

        var wersja = new ZadanieWersja
        {
            Id = id,
            NrWersji = nr,
            TrescMd = tresc.TrescMd,
            OpcjeZamkniete = tresc.OpcjeZamkniete,
            RozwiazanieWzorcowe = tresc.RozwiazanieWzorcowe,
            WynikKoncowyWzorcowy = tresc.WynikKoncowyWzorcowy,
            PunktyMax = tresc.PunktyMax,
            UtworzonaPrzez = autor,
            CreatedAt = teraz,
        };
        wersja._kryteria.AddRange(tresc.Kryteria);
        return wersja;
    }

    internal void Zastap(DateTimeOffset teraz) => ZastapionaAt ??= teraz;

    private static void SprawdzSpojnoscRodzaju(RodzajZadania rodzaj, FormaZamknieta? forma, TrescWersji tresc)
    {
        switch (rodzaj)
        {
            case RodzajZadania.Zamkniete:
                if (tresc.OpcjeZamkniete is null || tresc.OpcjeZamkniete.Forma != forma)
                {
                    throw new NiezmiennikNaruszonyException("OPCJE_ZAMKNIETE_WYMAGANE", "Zadanie zamknięte wymaga opcji zgodnych z jego formą.");
                }

                if (tresc.PunktyMax != OpcjeZamkniete.PunktyZadaniaZamknietego)
                {
                    throw new NiezmiennikNaruszonyException("PUNKTY_ZADANIA_POZA_ZAKRESEM", "Zadanie zamknięte jest warte jeden punkt.");
                }

                if (tresc.WynikKoncowyWzorcowy is not null)
                {
                    throw new NiezmiennikNaruszonyException("WYNIK_WZORCOWY_NIEDOZWOLONY", "Zadanie zamknięte nie ma wyniku końcowego wzorcowego.");
                }

                break;
            case RodzajZadania.OtwarteObliczeniowe:
                if (tresc.OpcjeZamkniete is not null)
                {
                    throw new NiezmiennikNaruszonyException("OPCJE_ZAMKNIETE_NIEDOZWOLONE", "Tylko Zadanie zamknięte ma opcje zamknięte.");
                }

                if (tresc.WynikKoncowyWzorcowy is null)
                {
                    throw new NiezmiennikNaruszonyException("WYNIK_WZORCOWY_WYMAGANY", "Zadanie obliczeniowe wymaga wyniku końcowego wzorcowego.");
                }

                break;
            case RodzajZadania.OtwarteZDowodem:
                if (tresc.OpcjeZamkniete is not null)
                {
                    throw new NiezmiennikNaruszonyException("OPCJE_ZAMKNIETE_NIEDOZWOLONE", "Tylko Zadanie zamknięte ma opcje zamknięte.");
                }

                if (tresc.WynikKoncowyWzorcowy is not null)
                {
                    throw new NiezmiennikNaruszonyException("WYNIK_WZORCOWY_NIEDOZWOLONY", "Dowód nie ma wyniku końcowego wzorcowego.");
                }

                break;
            default:
                throw new NiezmiennikNaruszonyException("RODZAJ_ZADANIA_NIEZNANY", "Nieznany rodzaj Zadania.");
        }
    }
}
