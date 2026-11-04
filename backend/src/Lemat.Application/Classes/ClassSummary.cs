// @Role: Rekordy odczytu modułu Klasy: KlasaSkrot (widok Klasy; Kod zaproszenia tylko dla prowadzącego Nauczyciela) i KodZaproszeniaSkrot | @DependsOn: Lemat.Domain.Classes, PoziomMatury, NauczycielId | @UsedBy: IKlasyFasada, KlasyFasada, Api (planowane TASK-071) | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Classes;
using Lemat.Domain.Common;

namespace Lemat.Application.Classes;

public sealed record KodZaproszeniaSkrot(string Wartosc, DateTimeOffset WaznyDo);

/// <param name="KodZaproszenia">Wypełniony wyłącznie w widoku prowadzącego Nauczyciela (uprawnienia obiektowe, ADR-071).</param>
/// <param name="Version">Źródło <c>If-Match</c> (ADR-069).</param>
public sealed record KlasaSkrot(
    KlasaId Id,
    string Nazwa,
    PoziomMatury PoziomMatury,
    NauczycielId? NauczycielId,
    bool Zarchiwizowana,
    int LiczbaUczniow,
    KodZaproszeniaSkrot? KodZaproszenia,
    int Version);
