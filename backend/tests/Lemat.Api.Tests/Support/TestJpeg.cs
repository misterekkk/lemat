// @Role: Generator minimalnych poprawnych plików JPEG (SOI, JFIF, SOF0 z wymiarami, SOS, EOI) i plików niepoprawnych do testów przesyłania zdjęć | @DependsOn: — | @UsedBy: Solutions/SolutionsEndpointsTests | @Task: TASK-127
namespace Lemat.Api.Tests.Support;

public static class TestowyJpeg
{
    /// <summary>Poprawny JPEG o podanych wymiarach; <paramref name="dopelnienieBajty"/> dokłada bajty danych skanu (rozmiar pliku).</summary>
    public static byte[] Utworz(int szerokosc = 640, int wysokosc = 480, int dopelnienieBajty = 0, byte ziarno = 0)
    {
        var bajty = new List<byte>
        {
            0xFF, 0xD8,
            0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
            0xFF, 0xC0, 0x00, 0x0B, 0x08, (byte)(wysokosc >> 8), (byte)wysokosc, (byte)(szerokosc >> 8), (byte)szerokosc, 0x01, 0x01, 0x11, 0x00,
            0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00,
            ziarno, 0x01, 0x02, 0x03,
        };
        bajty.AddRange(Enumerable.Repeat((byte)0x55, dopelnienieBajty));
        bajty.AddRange([0xFF, 0xD9]);
        return [.. bajty];
    }

    /// <summary>Nagłówek JPEG (FF D8 FF) bez żadnego segmentu SOF — nieczytelny jako obraz.</summary>
    public static byte[] Uszkodzony() => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x04, 0x11, 0x22, 0x33, 0x44, 0x55];

    public static byte[] Png() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];
}
