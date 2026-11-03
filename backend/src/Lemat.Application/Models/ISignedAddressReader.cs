// @Role: Port odczytu obiektu magazynu po podpisanym tokenie adresu (magazyn lokalny, U-03): zwraca zawartość albo null dla tokenu zmienionego, wygasłego lub wskazującego brak obiektu | @DependsOn: — | @UsedBy: EndpointyRozwiazan (GET /magazyn-lokalny/{token}), adapter MagazynObiektowLokalny | @Task: TASK-127
namespace Lemat.Application.Models;

public interface IOdczytPodpisanegoAdresu
{
    /// <summary>Zawartość obiektu wskazanego tokenem z <c>IMagazynObiektow.PodpisanyUrl</c>; <c>null</c> dla każdego tokenu nieprawidłowego (endpoint zwraca wtedy 404).</summary>
    Task<byte[]?> OtworzPodpisanyAsync(string token, CancellationToken ct);
}
