// @Role: Wynik normalizacji obrazu: bajty JPEG, wymiary, skrót sha256 i znacznik pochodzenia z HEIC | @DependsOn: — | @UsedBy: IObrobkaObrazow, PrzeslijRozwiazanieHandler | @Task: TASK-127
namespace Lemat.Application.Models;

#pragma warning disable CA1819 // Rekord wartości niesie surowe bajty obrazu i skrótu.
public sealed record ObrazZnormalizowany(byte[] Jpeg, int Szerokosc, int Wysokosc, byte[] Sha256, bool ZHeic);
#pragma warning restore CA1819
