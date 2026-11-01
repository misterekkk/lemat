// @Role: Fasada modułu Konta (jedyne wejście z innych modułów): odczyt roli, statusu i Grupy eksperymentu, rejestracja konta bez HTTP | @DependsOn: Lemat.Domain.Accounts, KontoSkrot, RejestracjaKontaWejscie | @UsedBy: moduły Klasy, Weryfikacje, Rekomendacje (IRekomendacjaSynchroniczna), Orkiestracja, Api | @Task: TASK-016
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public interface IKontaFasada
{
    Task<KontoSkrot?> PobierzKontoAsync(UzytkownikId id, CancellationToken ct);

    /// <summary>Grupa eksperymentu Ucznia (<c>null</c>, gdy konto nie jest kontem Ucznia).</summary>
    Task<GrupaEksperymentu?> PobierzGrupeEksperymentuAsync(UczenId id, CancellationToken ct);

    /// <summary>
    /// Zakłada konto (oraz Ucznia/Nauczyciela) w jednej transakcji. Nauczyciel startuje jako OCZEKUJE_NA_ZATWIERDZENIE,
    /// chyba że wywołujący zażądał autozatwierdzenia (U-04, tylko Development). Duplikat e-mail -> <c>EMAIL_ZAJETY</c>.
    /// </summary>
    Task<KontoSkrot> ZarejestrujAsync(RejestracjaKontaWejscie wejscie, CancellationToken ct);
}
