// @Role: Testy ładowania klucza JWT ES256: PEM z konfiguracji (także ze znakami \n), klucz deweloperski tworzony raz i ponownie odczytywany, brak klucza w Production, odrzucenie niepoprawnego klucza | @DependsOn: KluczJwt, JwtOpcje | @UsedBy: dotnet test | @Task: TASK-123
using System.Security.Cryptography;
using Lemat.Application.Common;
using Lemat.Infrastructure.Security;

namespace Lemat.Infrastructure.Tests.Security;

public sealed class JwtKeyTests : IDisposable
{
    private readonly string _katalog = Path.Combine(Path.GetTempPath(), $"lemat-jwt-test-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_katalog))
        {
            Directory.Delete(_katalog, recursive: true);
        }
    }

    [Fact]
    public void KluczZKonfiguracjiPemJestUzywanyTakzeZZapisemNowychLinii()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pem = ecdsa.ExportECPrivateKeyPem();

        var zNowymiLiniami = KluczJwt.Zaladuj(new JwtOpcje { Klucz = pem }, "Production");
        var zZapisemTekstowym = KluczJwt.Zaladuj(new JwtOpcje { Klucz = pem.Replace("\n", "\\n", StringComparison.Ordinal) }, "Production");

        zNowymiLiniami.Klucz.KeyId.ShouldBe(zZapisemTekstowym.Klucz.KeyId);
    }

    [Fact]
    public void BrakKluczaWProductionJestBledem()
    {
        Should.Throw<InvalidOperationException>(() => KluczJwt.Zaladuj(new JwtOpcje(), "Production"));
    }

    [Fact]
    public void NiepoprawnyAlboZlejKrzywejKluczJestOdrzucany()
    {
        Should.Throw<InvalidOperationException>(() => KluczJwt.Zaladuj(new JwtOpcje { Klucz = "nie-pem" }, "Production"));
        using var p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        Should.Throw<InvalidOperationException>(() => KluczJwt.Zaladuj(new JwtOpcje { Klucz = p384.ExportECPrivateKeyPem() }, "Production"));
    }

    [Fact]
    public void KluczDeweloperskiJestTworzonyJedenRazIZapisanyPozaRepozytorium()
    {
        var pierwszy = KluczJwt.Zaladuj(new JwtOpcje(), "Development", _katalog);
        var drugi = KluczJwt.Zaladuj(new JwtOpcje(), "Development", _katalog);

        File.Exists(Path.Combine(_katalog, KluczJwt.NazwaPlikuDev)).ShouldBeTrue();
        drugi.Klucz.KeyId.ShouldBe(pierwszy.Klucz.KeyId);
        KluczJwt.KatalogDomyslnyDev.ShouldNotContain("lemat/backend");
    }

    [Fact]
    public void NiezapisywalnyKatalogDevDajeKluczWPamieci()
    {
        var plik = Path.Combine(Path.GetTempPath(), $"lemat-jwt-plik-{Guid.NewGuid():N}");
        File.WriteAllText(plik, "to jest plik, nie katalog");
        try
        {
            var klucz = KluczJwt.Zaladuj(new JwtOpcje(), "Development", plik);

            klucz.Klucz.KeyId.ShouldNotBeNullOrWhiteSpace();
        }
        finally
        {
            File.Delete(plik);
        }
    }
}
