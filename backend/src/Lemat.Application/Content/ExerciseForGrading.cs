// @Role: Migawka wersji Zadania do oceny (z Kryteriami oceniania, Rozwiązaniem wzorcowym, Wynikiem końcowym wzorcowym i opcjami zamkniętymi) zwracana przez ITresciFasada.PobierzZadanieDoOcenyAsync | @DependsOn: Lemat.Domain.Content | @UsedBy: moduły Ocenianie, Weryfikacje, Agenci (przez fasadę) | @Task: TASK-017
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Application.Content;

public sealed record ZadanieDoOceny(
    ZadanieId ZadanieId,
    ZadanieWersjaId ZadanieWersjaId,
    int NrWersji,
    RodzajZadania Rodzaj,
    FormaZamknieta? FormaZamknieta,
    PoziomMatury PoziomMatury,
    DzialId DzialId,
    UmiejetnoscId UmiejetnoscGlowna,
    string TrescMd,
    int PunktyMax,
    OpcjeZamkniete? OpcjeZamkniete,
    RozwiazanieWzorcowe RozwiazanieWzorcowe,
    WynikKoncowyWzorcowy? WynikKoncowyWzorcowy,
    IReadOnlyList<KryteriumOceniania> KryteriaOceniania);
