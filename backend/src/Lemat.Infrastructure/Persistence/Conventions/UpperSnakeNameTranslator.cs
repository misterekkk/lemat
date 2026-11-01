// @Role: Translator nazw Npgsql dla typów wyliczeniowych PostgreSQL: typ snake_case, członek UPPER_SNAKE (ADR-049, ef-core.md 3.2) | @DependsOn: Lemat.Domain.Common.KonwencjaNazw, Npgsql | @UsedBy: MapowanieTypowWyliczeniowych, UpperSnakeEnumConverter | @Task: TASK-015
using Lemat.Domain.Common;
using Npgsql;

namespace Lemat.Infrastructure.Persistence.Conventions;

public sealed class TlumaczNazwUpperSnake : INpgsqlNameTranslator
{
    public static TlumaczNazwUpperSnake Instancja { get; } = new();

    /// <summary><c>StatusRozwiazania</c> -> <c>status_rozwiazania</c>.</summary>
    public string TranslateTypeName(string clrName) => KonwencjaNazw.PascalNaSnake(clrName);

    /// <summary><c>OtwarteZDowodem</c> -> <c>OTWARTE_Z_DOWODEM</c>.</summary>
    public string TranslateMemberName(string clrName) => KonwencjaNazw.PascalNaUpperSnake(clrName);
}
