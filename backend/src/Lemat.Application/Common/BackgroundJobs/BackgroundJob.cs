// @Role: Praca w tle pobrana z kolejki do wykonania (identyfikator, typ, ładunek jsonb jako tekst, numer próby i limit prób) | @DependsOn: TypPracy | @UsedBy: IKolejkaPrac, ProcesorPrac, IHandlerPracy | @Task: TASK-128
namespace Lemat.Application.Common.BackgroundJobs;

/// <param name="Ladunek">Ładunek <c>jsonb</c> jako tekst JSON (klucze snake_case).</param>
/// <param name="Proby">Numer bieżącej próby (po pobraniu ≥ 1).</param>
public sealed record PracaWTle(long Id, TypPracy Typ, string Ladunek, int Proby, int MaksProb);
