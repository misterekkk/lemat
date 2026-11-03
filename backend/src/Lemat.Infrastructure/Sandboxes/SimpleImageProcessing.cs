// @Role: Adapter ProstaObrobkaObrazow (U-02, U-25): przyjmuje wyłącznie JPEG (magic bytes FF D8 FF), ≤ 10 MB, odczytuje wymiary z nagłówka SOF i liczy sha256; bez przetwarzania i bez usuwania EXIF | @DependsOn: IObrobkaObrazow, ObrazZnormalizowany, ZdjecieNieprawidloweException | @UsedBy: ObjectStorageRegistration, PrzeslijRozwiazanieHandler | @Task: TASK-127
using System.Buffers.Binary;
using System.Security.Cryptography;
using Lemat.Application.Models;

namespace Lemat.Infrastructure.Sandboxes;

public sealed class ProstaObrobkaObrazow : IObrobkaObrazow
{
    public const int MaksRozmiarBajty = 10 * 1024 * 1024;

    public Task<ObrazZnormalizowany> NormalizujAsync(ReadOnlyMemory<byte> plik, CancellationToken ct)
    {
        var dane = plik.Span;
        if (dane.Length > MaksRozmiarBajty)
        {
            throw new ZdjecieNieprawidloweException(ZdjecieNieprawidloweException.KodZaDuze);
        }

        if (!dane.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
        {
            throw new ZdjecieNieprawidloweException(
                JestInnymFormatemObrazu(dane) ? ZdjecieNieprawidloweException.KodNieobslugiwanyFormat : ZdjecieNieprawidloweException.KodNieprawidlowe);
        }

        var (szerokosc, wysokosc) = OdczytajWymiary(dane)
            ?? throw new ZdjecieNieprawidloweException(ZdjecieNieprawidloweException.KodNieprawidlowe);
        return Task.FromResult(new ObrazZnormalizowany(plik.ToArray(), szerokosc, wysokosc, SHA256.HashData(dane), ZHeic: false));
    }

    /// <summary>PNG, WEBP, GIF, BMP, TIFF i HEIC/HEIF: rozpoznany obraz w formacie, którego POC nie przyjmuje (415).</summary>
    private static bool JestInnymFormatemObrazu(ReadOnlySpan<byte> dane) =>
        dane.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 })
        || dane.StartsWith("GIF8"u8)
        || dane.StartsWith("BM"u8)
        || dane.StartsWith(new byte[] { 0x49, 0x49, 0x2A, 0x00 })
        || dane.StartsWith(new byte[] { 0x4D, 0x4D, 0x00, 0x2A })
        || (dane.Length >= 12 && dane[..4].SequenceEqual("RIFF"u8) && dane.Slice(8, 4).SequenceEqual("WEBP"u8))
        || (dane.Length >= 12 && dane.Slice(4, 4).SequenceEqual("ftyp"u8));

    /// <summary>Wymiary z pierwszego segmentu SOFn (bez DHT/DAC/JPG); <c>null</c>, gdy nagłówek jest ucięty albo nie ma SOF przed SOS.</summary>
    private static (int Szerokosc, int Wysokosc)? OdczytajWymiary(ReadOnlySpan<byte> dane)
    {
        var poz = 2;
        while (poz + 4 <= dane.Length)
        {
            if (dane[poz] != 0xFF)
            {
                return null;
            }

            while (poz < dane.Length && dane[poz] == 0xFF)
            {
                poz++;
            }

            if (poz >= dane.Length)
            {
                return null;
            }

            var znacznik = dane[poz++];
            if (znacznik is 0x01 or (>= 0xD0 and <= 0xD8))
            {
                continue;
            }

            if (znacznik is 0xD9 or 0xDA || poz + 2 > dane.Length)
            {
                return null;
            }

            var dlugosc = BinaryPrimitives.ReadUInt16BigEndian(dane.Slice(poz, 2));
            if (dlugosc < 2)
            {
                return null;
            }

            if (znacznik is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
            {
                if (dlugosc < 8 || poz + 7 > dane.Length)
                {
                    return null;
                }

                int wysokosc = BinaryPrimitives.ReadUInt16BigEndian(dane.Slice(poz + 3, 2));
                int szerokosc = BinaryPrimitives.ReadUInt16BigEndian(dane.Slice(poz + 5, 2));
                return szerokosc is >= 1 and <= short.MaxValue && wysokosc is >= 1 and <= short.MaxValue ? (szerokosc, wysokosc) : null;
            }

            poz += dlugosc;
        }

        return null;
    }
}
