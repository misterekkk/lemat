// @Role: Testy adapterów MagazynObiektowLokalny (zapis/odczyt/usunięcie, podpisany adres, wygasanie, odporność na zmieniony token i ucieczkę ze ścieżki) i ProstaObrobkaObrazow (JPEG, magic bytes, wymiary SOF, limit 10 MB, sha256) | @DependsOn: MagazynObiektowLokalny, ProstaObrobkaObrazow | @UsedBy: dotnet test | @Task: TASK-127
using System.Security.Cryptography;
using Lemat.Application.Models;
using Lemat.Infrastructure.Sandboxes;
using Lemat.Infrastructure.Storage;

namespace Lemat.Infrastructure.Tests.Storage;

public sealed class StorageAdaptersTests : IDisposable
{
    private const string Klucz = "zdjecia/u1/r1/z1.jpg";

    private readonly string _katalog = Path.Combine(Path.GetTempPath(), "lemat-test-" + Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        if (Directory.Exists(_katalog))
        {
            Directory.Delete(_katalog, recursive: true);
        }
    }

    private sealed class Zegar(DateTimeOffset teraz) : TimeProvider
    {
        public DateTimeOffset Teraz { get; set; } = teraz;

        public override DateTimeOffset GetUtcNow() => Teraz;
    }

    private static readonly DateTimeOffset Start = new(2026, 11, 2, 10, 5, 0, TimeSpan.Zero);

    private MagazynObiektowLokalny Magazyn(Zegar? zegar = null, string klucz = "tajny") => new(_katalog, klucz, zegar ?? new Zegar(Start));

    private static byte[] Jpeg(int szerokosc, int wysokosc) =>
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
        0xFF, 0xC0, 0x00, 0x0B, 0x08, (byte)(wysokosc >> 8), (byte)wysokosc, (byte)(szerokosc >> 8), (byte)szerokosc, 0x01, 0x01, 0x11, 0x00,
        0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, 0x01, 0xFF, 0xD9,
    ];

    // ---------- MagazynObiektowLokalny ----------

    [Fact]
    public async Task ZapisOdczytIUsuniecieDzialajaIUsuniecieJestIdempotentne()
    {
        var magazyn = Magazyn();

        await magazyn.ZapiszAsync(Klucz, new byte[] { 1, 2, 3 }, "image/jpeg", Ct);
        (await magazyn.PobierzAsync(Klucz, Ct)).ShouldBe([1, 2, 3]);
        await magazyn.UsunAsync(Klucz, Ct);
        await magazyn.UsunAsync(Klucz, Ct);

        File.Exists(Path.Combine(_katalog, Klucz)).ShouldBeFalse();
        Directory.GetFiles(Path.Combine(_katalog, "zdjecia"), "*.tmp", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("../poza.jpg")]
    [InlineData("zdjecia/../../poza.jpg")]
    [InlineData("/etc/passwd")]
    [InlineData("zdjecia/a b.jpg")]
    [InlineData("")]
    public async Task KluczWychodzacyPozaKatalogLubNiepoprawnyJestOdrzucany(string klucz)
    {
        var magazyn = Magazyn();

        await Should.ThrowAsync<ArgumentException>(() => magazyn.ZapiszAsync(klucz, new byte[] { 1 }, "image/jpeg", Ct));
        Should.Throw<ArgumentException>(() => magazyn.PodpisanyUrl(klucz, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public async Task BrakSciezkiMagazynuKonczySieBledemDopieroPrzyUzyciu()
    {
        var magazyn = new MagazynObiektowLokalny(string.Empty, "k", TimeProvider.System);

        await Should.ThrowAsync<InvalidOperationException>(() => magazyn.ZapiszAsync(Klucz, new byte[] { 1 }, "image/jpeg", Ct));
    }

    [Fact]
    public async Task PodpisanyAdresDajeDostepDoWygasniecia()
    {
        var zegar = new Zegar(Start);
        var magazyn = Magazyn(zegar);
        await magazyn.ZapiszAsync(Klucz, new byte[] { 9, 8, 7 }, "image/jpeg", Ct);

        var adres = magazyn.PodpisanyUrl(Klucz, TimeSpan.FromMinutes(5));

        adres.WygasaAt.ShouldBe(Start.AddMinutes(5));
        adres.Url.IsAbsoluteUri.ShouldBeFalse();
        var token = adres.Url.OriginalString["/api/v1/magazyn-lokalny/".Length..];
        (await magazyn.OtworzPodpisanyAsync(token, Ct)).ShouldBe([9, 8, 7]);

        zegar.Teraz = Start.AddMinutes(5).AddSeconds(-1);
        (await magazyn.OtworzPodpisanyAsync(token, Ct)).ShouldNotBeNull();
        zegar.Teraz = Start.AddMinutes(5);
        (await magazyn.OtworzPodpisanyAsync(token, Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task ZmienionyAlboObcyTokenIBrakObiektuDajaNull()
    {
        var magazyn = Magazyn();
        await magazyn.ZapiszAsync(Klucz, new byte[] { 1 }, "image/jpeg", Ct);
        var token = magazyn.PodpisanyUrl(Klucz, TimeSpan.FromMinutes(5)).Url.OriginalString["/api/v1/magazyn-lokalny/".Length..];
        var obcyKlucz = Magazyn(klucz: "inny").PodpisanyUrl(Klucz, TimeSpan.FromMinutes(5)).Url.OriginalString["/api/v1/magazyn-lokalny/".Length..];
        var brak = magazyn.PodpisanyUrl("zdjecia/u1/r1/nie-ma.jpg", TimeSpan.FromMinutes(5)).Url.OriginalString["/api/v1/magazyn-lokalny/".Length..];
        var zmieniony = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        (await magazyn.OtworzPodpisanyAsync(zmieniony, Ct)).ShouldBeNull();
        (await magazyn.OtworzPodpisanyAsync(obcyKlucz, Ct)).ShouldBeNull();
        (await magazyn.OtworzPodpisanyAsync(brak, Ct)).ShouldBeNull();
        (await magazyn.OtworzPodpisanyAsync("nie-token", Ct)).ShouldBeNull();
        (await magazyn.OtworzPodpisanyAsync("a.b", Ct)).ShouldBeNull();
        (await magazyn.OtworzPodpisanyAsync(string.Empty, Ct)).ShouldBeNull();
    }

    // ---------- ProstaObrobkaObrazow ----------

    [Fact]
    public async Task PoprawnyJpegDajeWymiaryZNaglowkaISha256()
    {
        var plik = Jpeg(1280, 960);

        var obraz = await new ProstaObrobkaObrazow().NormalizujAsync(plik, Ct);

        obraz.Szerokosc.ShouldBe(1280);
        obraz.Wysokosc.ShouldBe(960);
        obraz.Sha256.ShouldBe(SHA256.HashData(plik));
        obraz.Jpeg.ShouldBe(plik);
        obraz.ZHeic.ShouldBeFalse();
    }

    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A }, "NIEOBSLUGIWANY_FORMAT")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, "NIEOBSLUGIWANY_FORMAT")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 }, "NIEOBSLUGIWANY_FORMAT")]
    [InlineData(new byte[] { 0, 0, 0, 0x18, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63 }, "NIEOBSLUGIWANY_FORMAT")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x04, 0x11, 0x22 }, "ZDJECIE_NIEPRAWIDLOWE")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF }, "ZDJECIE_NIEPRAWIDLOWE")]
    [InlineData(new byte[] { 0x01, 0x02, 0x03 }, "ZDJECIE_NIEPRAWIDLOWE")]
    [InlineData(new byte[0], "ZDJECIE_NIEPRAWIDLOWE")]
    public async Task PlikiInneNizPoprawnyJpegSaOdrzucaneZKodem(byte[] plik, string kod)
    {
        var blad = await Should.ThrowAsync<ZdjecieNieprawidloweException>(() => new ProstaObrobkaObrazow().NormalizujAsync(plik, Ct));

        blad.Kod.ShouldBe(kod);
    }

    [Fact]
    public async Task JpegZZerowymWymiaremAlboPlikPowyzej10MbJestOdrzucany()
    {
        var zerowy = await Should.ThrowAsync<ZdjecieNieprawidloweException>(() => new ProstaObrobkaObrazow().NormalizujAsync(Jpeg(0, 100), Ct));
        var duzy = new byte[ProstaObrobkaObrazow.MaksRozmiarBajty + 1];
        Jpeg(10, 10).CopyTo(duzy, 0);
        var zaDuzy = await Should.ThrowAsync<ZdjecieNieprawidloweException>(() => new ProstaObrobkaObrazow().NormalizujAsync(duzy, Ct));

        zerowy.Kod.ShouldBe("ZDJECIE_NIEPRAWIDLOWE");
        zaDuzy.Kod.ShouldBe("ZDJECIE_ZA_DUZE");
    }
}
