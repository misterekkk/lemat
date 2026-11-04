// @Role: Kandydat do doboru Zadania (ZadanieDoDoboru) i filtr kandydatów (10.1.2 krok 7: OPUBLIKOWANE, GLOBALNY, bez wstrzymania w doborze, Umiejętność główna, poziom matury, rodzaje) | @DependsOn: Lemat.Domain.Content | @UsedBy: ITresciFasada, adapter IDaneDoboru (Rekomendacje, TASK-019/040) | @Task: TASK-017
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Application.Content;

public sealed record FiltrKandydatow(PoziomMatury Poziom, UmiejetnoscId UmiejetnoscGlowna, IReadOnlySet<RodzajZadania> Rodzaje);

public sealed record ZadanieDoDoboru(
    ZadanieId Id,
    ZadanieWersjaId AktualnaWersjaId,
    RodzajZadania Rodzaj,
    FormaZamknieta? Forma,
    int LiczbaStwierdzen,
    UmiejetnoscId UmiejetnoscGlowna,
    IReadOnlyList<UmiejetnoscId> Dodatkowe,
    PoziomTrudnosci PoziomTrudnosci,
    decimal TrudnoscB,
    int LiczbaWynikowKalibracji,
    bool CzyBazowe);
