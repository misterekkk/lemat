// @Role: Widok Zadania dla Ucznia (bez Rozwiązania wzorcowego, Kryteriów i klucza odpowiedzi) zwracany przez ITresciFasada.PobierzZadanieUczniaAsync; status, zasięg i autor służą autoryzacji w warstwie API | @DependsOn: Lemat.Domain.Content | @UsedBy: moduły Rozwiązania, Rekomendacje, Lemat.Api | @Task: TASK-017
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Application.Content;

public sealed record StwierdzenieDlaUcznia(string Id, string TrescMd);

/// <summary>Opcje zamknięte bez klucza poprawnych odpowiedzi.</summary>
public sealed record OpcjeZamknieteUcznia(FormaZamknieta Forma, IReadOnlyList<OpcjaWyboru> Opcje, IReadOnlyList<StwierdzenieDlaUcznia> Stwierdzenia);

public sealed record ZadanieUcznia(
    ZadanieId ZadanieId,
    ZadanieWersjaId ZadanieWersjaId,
    int NrWersji,
    StatusZadania Status,
    ZasiegZadania Zasieg,
    UzytkownikId? AutorId,
    RodzajZadania Rodzaj,
    PoziomMatury PoziomMatury,
    DzialId DzialId,
    UmiejetnoscId UmiejetnoscGlowna,
    string TrescMd,
    int PunktyMax,
    OpcjeZamknieteUcznia? Opcje);
