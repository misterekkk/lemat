// @Role: Port obróbki obrazów: normalizacja przesłanego pliku do JPEG z wymiarami i sha256 (POC: walidacja bez przetwarzania; U-02, U-25) | @DependsOn: ObrazZnormalizowany, ZdjecieNieprawidloweException | @UsedBy: PrzeslijRozwiazanieHandler, adapter ProstaObrobkaObrazow | @Task: TASK-127
namespace Lemat.Application.Models;

public interface IObrobkaObrazow
{
    /// <summary>Waliduje i normalizuje plik; <see cref="ZdjecieNieprawidloweException"/> z kodem błędu API, gdy plik nie nadaje się do przyjęcia.</summary>
    Task<ObrazZnormalizowany> NormalizujAsync(ReadOnlyMemory<byte> plik, CancellationToken ct);
}
