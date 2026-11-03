// @Role: Port zlecania prac w tle (ADR-056 p. 2): INSERT do praca_w_tle w transakcji bieżącej jednostki pracy; wymaga otwartej transakcji | @DependsOn: ZleceniePracy, RezultatZlecenia | @UsedBy: PrzeslijRozwiazanieHandler, ZlecaniePracSql | @Task: TASK-127
namespace Lemat.Application.Common.BackgroundJobs;

public interface IZlecaniePrac
{
    /// <summary>Zleca pracę w transakcji bieżącej <c>IJednostkaPracy</c> (poza transakcją: <see cref="InvalidOperationException"/>).</summary>
    Task<RezultatZlecenia> ZlecAsync(ZleceniePracy zlecenie, CancellationToken ct);
}
