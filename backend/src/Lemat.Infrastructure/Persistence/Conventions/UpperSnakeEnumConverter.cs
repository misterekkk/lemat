// @Role: Konwerter enuma C# na kolumnę text z listą dozwolonych wartości UPPER_SNAKE (CHECK tekstowy; ef-core.md 3.2) | @DependsOn: Lemat.Domain.Common.KonwencjaNazw, Microsoft.EntityFrameworkCore | @UsedBy: konfiguracje encji (kod_bledu_rozpoznawania, pula, typ, metoda, sesja) | @Task: TASK-015
using Lemat.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lemat.Infrastructure.Persistence.Conventions;

public sealed class UpperSnakeEnumConverter<TEnum>()
    : ValueConverter<TEnum, string>(
        wartosc => KonwencjaNazw.PascalNaUpperSnake(wartosc.ToString()),
        tekst => Parsuj(tekst))
    where TEnum : struct, Enum
{
    private static TEnum Parsuj(string tekst) =>
        Enum.Parse<TEnum>(KonwencjaNazw.UpperSnakeNaPascal(tekst), ignoreCase: false);
}
