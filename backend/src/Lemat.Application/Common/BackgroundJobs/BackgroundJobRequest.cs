// @Role: Zlecenie pracy w tle: typ, ładunek (serializowany do jsonb w snake_case), klucz deduplikacji, opcjonalny termin dostępności i priorytet | @DependsOn: TypPracy | @UsedBy: IZlecaniePrac, PrzeslijRozwiazanieHandler | @Task: TASK-127
namespace Lemat.Application.Common.BackgroundJobs;

public sealed record ZleceniePracy(
    TypPracy Typ,
    object Ladunek,
    string? KluczDeduplikacji,
    DateTimeOffset? DostepnaOd = null,
    short? Priorytet = null);
