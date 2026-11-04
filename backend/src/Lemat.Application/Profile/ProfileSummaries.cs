// @Role: Migawki i wejścia fasady Profilu: ProfilSkrot, OpanowanieSkrot, NowyWynikZadania, WynikSkrot, TestSkrot, PozycjaTestuSkrot, OdpowiedzNaPozycje | @DependsOn: Lemat.Domain.Profile, UczenId, ZadanieId | @UsedBy: IProfilFasada, ProfilFasada, Api (planowane TASK-073), Rekomendacje | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Profile;

namespace Lemat.Application.Profile;

public sealed record OpanowanieSkrot(
    UmiejetnoscId UmiejetnoscId,
    decimal Theta,
    decimal PoziomOpanowania,
    int LiczbaWynikow,
    DateTimeOffset? OstatniWynikAt,
    int LicznikPowtorek,
    DateTimeOffset? NastepnaPowtorkaAt,
    InicjalizacjaOpanowania Inicjalizacja);

public sealed record ProfilSkrot(UczenId UczenId, int Version, IReadOnlyList<OpanowanieSkrot> Opanowanie);

/// <summary>Dane nowego Wyniku zadania; <c>OdpowiedzId</c> dla źródła ODPOWIEDZ, <c>OcenaId</c> dla źródła OCENA.</summary>
public sealed record NowyWynikZadania(
    UczenId UczenId,
    ZadanieId ZadanieId,
    ZadanieWersjaId ZadanieWersjaId,
    ZrodloWyniku Zrodlo,
    Guid? OdpowiedzId,
    Guid? OcenaId,
    decimal Ulamek,
    bool OcenaWstepna,
    bool MozliwyBladOdczytu);

public sealed record WynikSkrot(WynikZadaniaId Id, decimal Ulamek, decimal Waga, bool LiczyDoTrudnosci);

public sealed record PozycjaTestuSkrot(PozycjaTestuDiagnostycznegoId Id, int Kolejnosc, ZadanieWersjaId ZadanieWersjaId);

public sealed record TestSkrot(
    TestDiagnostycznyId Id,
    PoziomMatury PoziomMatury,
    StatusTestu Status,
    IReadOnlyList<int> PominieteDzialy,
    DateTimeOffset? ZakonczonoAt,
    IReadOnlyList<PozycjaTestuSkrot> Pozycje);

/// <summary>Odpowiedź Ucznia na pozycję Testu diagnostycznego (Odpowiedź z modułu Rozwiązania) z policzonym ułamkiem punktów.</summary>
public sealed record OdpowiedzNaPozycje(PozycjaTestuDiagnostycznegoId PozycjaId, Guid OdpowiedzId, ZadanieId ZadanieId, decimal Ulamek);
