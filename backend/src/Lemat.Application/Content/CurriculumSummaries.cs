// @Role: Skróty danych referencyjnych (Dział, Umiejętność) oraz opublikowanej Wkładki do doboru zwracane przez ITresciFasada | @DependsOn: Lemat.Domain.Content | @UsedBy: ITresciFasada, moduły Profil, Rekomendacje, Lemat.Api | @Task: TASK-017
using Lemat.Domain.Content;

namespace Lemat.Application.Content;

public sealed record DzialSkrot(DzialId Id, string Kod, string Nazwa, bool TylkoRozszerzony, int Kolejnosc);

public sealed record UmiejetnoscSkrot(
    UmiejetnoscId Id,
    DzialId DzialId,
    string Kod,
    string Nazwa,
    bool TylkoRozszerzony,
    decimal WagaEgzaminacyjna,
    IReadOnlyList<UmiejetnoscId> Wymagane);

public sealed record WkladkaDoDoboru(WkladkaTeoretycznaId Id, UmiejetnoscId UmiejetnoscId, int SzacowanyCzasMin);
