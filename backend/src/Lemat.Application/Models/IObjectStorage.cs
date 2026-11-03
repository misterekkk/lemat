// @Role: Port magazynu obiektów (zdjęcia kartek): zapis, odczyt, usunięcie (idempotentne) i podpisany adres o ograniczonej ważności | @DependsOn: PodpisanyAdres | @UsedBy: PrzeslijRozwiazanieHandler, adapter MagazynObiektowLokalny, endpoint adresu zdjęcia | @Task: TASK-127
namespace Lemat.Application.Models;

public interface IMagazynObiektow
{
    Task ZapiszAsync(string klucz, ReadOnlyMemory<byte> dane, string typMime, CancellationToken ct);

    Task<byte[]> PobierzAsync(string klucz, CancellationToken ct);

    /// <summary>Idempotentne: brak obiektu nie jest błędem.</summary>
    Task UsunAsync(string klucz, CancellationToken ct);

    PodpisanyAdres PodpisanyUrl(string klucz, TimeSpan waznosc);
}
