// @Role: DTO adresu zdjęcia (AdresZdjeciaWyjscie, kontrakt POC 7.9): podpisany url i moment wygaśnięcia | @DependsOn: — | @UsedBy: EndpointyRozwiazan | @Task: TASK-127
namespace Lemat.Api.Contracts.Solutions;

public sealed record AdresZdjeciaWyjscie(string Url, DateTimeOffset WygasaAt);
