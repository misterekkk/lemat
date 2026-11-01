// @Role: Fasada modułu Klasy (jedyne wejście z innych modułów, 6.2): zakładanie Klasy, Kod zaproszenia, dołączanie i opuszczanie, archiwizacja, Przekazanie Klasy, odczyty z uprawnieniami obiektowymi | @DependsOn: Lemat.Domain.Classes, KlasaSkrot, NauczycielId, UczenId | @UsedBy: moduły Weryfikacje (przydział kolejki), Przypisania, Api (planowane TASK-071) | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Classes;
using Lemat.Domain.Common;

namespace Lemat.Application.Classes;

public interface IKlasyFasada
{
    /// <summary>Zakłada Klasę z nowym Kodem zaproszenia (ważny 14 dni).</summary>
    Task<KlasaSkrot> ZalozAsync(NauczycielId nauczyciel, string nazwa, PoziomMatury poziom, CancellationToken ct);

    /// <summary>Widok Klasy bez Kodu zaproszenia albo <c>null</c>.</summary>
    Task<KlasaSkrot?> PobierzAsync(KlasaId id, CancellationToken ct);

    /// <summary>Widok prowadzącego Nauczyciela (z Kodem); cudza Klasa kończy się <c>KLASA_NIE_ZNALEZIONA</c> (ADR-071).</summary>
    Task<KlasaSkrot> PobierzDlaNauczycielaAsync(NauczycielId nauczyciel, KlasaId id, CancellationToken ct);

    Task<IReadOnlyList<KlasaSkrot>> ListaKlasNauczycielaAsync(NauczycielId nauczyciel, CancellationToken ct);

    Task<IReadOnlyList<KlasaSkrot>> ListaKlasUczniaAsync(UczenId uczen, CancellationToken ct);

    /// <summary>Klasa, do której Uczeń dołączył najpóźniej i która nie jest zarchiwizowana (przydział Weryfikacji, 11.4.1).</summary>
    Task<KlasaSkrot?> PobierzNajpozniejDolaczonaKlaseAsync(UczenId uczen, CancellationToken ct);

    /// <summary>Dołącza kodem; nieznany lub nieważny kod daje <c>KOD_ZAPROSZENIA_NIEWAZNY</c>. Idempotentne dla członka.</summary>
    Task<KlasaSkrot> DolaczAsync(UczenId uczen, string kod, CancellationToken ct);

    Task OpuscAsync(UczenId uczen, KlasaId id, CancellationToken ct);

    Task<KodZaproszeniaSkrot> OdnowKodAsync(NauczycielId nauczyciel, KlasaId id, int? oczekiwanaWersja, CancellationToken ct);

    Task UniewaznKodAsync(NauczycielId nauczyciel, KlasaId id, int? oczekiwanaWersja, CancellationToken ct);

    Task ArchiwizujAsync(NauczycielId nauczyciel, KlasaId id, int? oczekiwanaWersja, CancellationToken ct);

    /// <summary>Przekazanie Klasy (ADR-044; operacja Administratora).</summary>
    Task<KlasaSkrot> PrzekazAsync(KlasaId id, NauczycielId nowy, int? oczekiwanaWersja, CancellationToken ct);
}
