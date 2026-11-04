// @Role: Podpisany adres obiektu magazynu (url względny lub bezwzględny) i moment wygaśnięcia | @DependsOn: — | @UsedBy: IMagazynObiektow, endpoint adresu zdjęcia | @Task: TASK-127
namespace Lemat.Application.Models;

public sealed record PodpisanyAdres(Uri Url, DateTimeOffset WygasaAt);
