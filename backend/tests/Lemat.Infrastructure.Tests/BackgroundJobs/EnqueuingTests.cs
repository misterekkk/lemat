// @Role: Testy ZlecaniePracSql na PostgreSQL: wstawienie pracy ROZPOZNAJ_PISMO (pula, priorytet, maks_prob, ładunek snake_case), deduplikacja OCZEKUJE/W_TOKU, ponowne zlecenie po zakończeniu, wymóg transakcji i wycofanie razem z jednostką pracy | @DependsOn: BazaTestowa, ZlecaniePracSql, JednostkaPracyEf | @UsedBy: dotnet test | @Task: TASK-127
using System.Text.Json;
using Lemat.Application.Common;
using Lemat.Application.Common.BackgroundJobs;
using Lemat.Infrastructure.BackgroundJobs;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Tests.Persistence;
using Lemat.Infrastructure.Tests.Persistence.Support;

namespace Lemat.Infrastructure.Tests.BackgroundJobs;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class EnqueuingTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private BazaTestowa _baza = null!;

    public async ValueTask InitializeAsync() => _baza = await BazaTestowa.UtworzAsync(postgres);

    public async ValueTask DisposeAsync() => await _baza.DisposeAsync();

    private static ZleceniePracy Zlecenie(string klucz) =>
        new(TypPracy.RozpoznajPismo, new { RozwiazanieId = Guid.Parse("d1000000-0000-4000-8000-000000000011"), PrzeslanieNr = 1, PonowienieNr = 0, MockScenariusz = "domyslny" }, klucz);

    [Fact]
    public async Task ZlecenieWstawiaPraceZParametramiZKatalogu()
    {
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);

        var rezultat = await uow.WykonajAsync(ct => new ZlecaniePracSql(uow).ZlecAsync(Zlecenie("r:1:0"), ct), Ct);

        rezultat.Wynik.ShouldBe(WynikZlecenia.Wstawiona);
        (await _baza.SkalarAsync<long>("SELECT count(*) FROM praca_w_tle")).ShouldBe(1);
        (await _baza.SkalarAsync<string>("SELECT typ FROM praca_w_tle")).ShouldBe("ROZPOZNAJ_PISMO");
        (await _baza.SkalarAsync<string>("SELECT pula FROM praca_w_tle")).ShouldBe("ROZPOZNAWANIE");
        (await _baza.SkalarAsync<string>("SELECT status::text FROM praca_w_tle")).ShouldBe("OCZEKUJE");
        (await _baza.SkalarAsync<short>("SELECT priorytet FROM praca_w_tle")).ShouldBe((short)1);
        (await _baza.SkalarAsync<short>("SELECT maks_prob FROM praca_w_tle")).ShouldBe((short)3);
        var ladunek = JsonDocument.Parse(await _baza.SkalarAsync<string>("SELECT ladunek::text FROM praca_w_tle")).RootElement;
        ladunek.GetProperty("rozwiazanie_id").GetString().ShouldBe("d1000000-0000-4000-8000-000000000011");
        ladunek.GetProperty("przeslanie_nr").GetInt32().ShouldBe(1);
        ladunek.GetProperty("ponowienie_nr").GetInt32().ShouldBe(0);
        ladunek.GetProperty("mock_scenariusz").GetString().ShouldBe("domyslny");
    }

    [Fact]
    public async Task DrugieZlecenieZTymSamymKluczemJestZdeduplikowaneDoZakonczeniaPracy()
    {
        long pierwsza;
        await using (var kontekst = _baza.Kontekst())
        {
            var uow = new JednostkaPracyEf(kontekst);
            pierwsza = (await uow.WykonajAsync(ct => new ZlecaniePracSql(uow).ZlecAsync(Zlecenie("r:1:0"), ct), Ct)).PracaId;
        }

        await using (var kontekst = _baza.Kontekst())
        {
            var uow = new JednostkaPracyEf(kontekst);
            var druga = await uow.WykonajAsync(ct => new ZlecaniePracSql(uow).ZlecAsync(Zlecenie("r:1:0"), ct), Ct);
            druga.Wynik.ShouldBe(WynikZlecenia.Zdeduplikowana);
            druga.PracaId.ShouldBe(pierwsza);
        }

        (await _baza.SkalarAsync<long>("SELECT count(*) FROM praca_w_tle")).ShouldBe(1);
        await _baza.WykonajAsync($"UPDATE praca_w_tle SET status = 'ZAKONCZONA' WHERE id = {pierwsza}");

        await using var po = _baza.Kontekst();
        var uowPo = new JednostkaPracyEf(po);
        var nowa = await uowPo.WykonajAsync(ct => new ZlecaniePracSql(uowPo).ZlecAsync(Zlecenie("r:1:0"), ct), Ct);
        nowa.Wynik.ShouldBe(WynikZlecenia.Wstawiona);
        nowa.PracaId.ShouldNotBe(pierwsza);
    }

    [Fact]
    public async Task ZlecenieBezTransakcjiJestOdrzucone()
    {
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);

        var blad = await Should.ThrowAsync<InvalidOperationException>(() => new ZlecaniePracSql(uow).ZlecAsync(Zlecenie("r:1:0"), Ct));

        blad.Message.ShouldBe(IJednostkaPracy.KodPozaJednostkaPracy);
    }

    [Fact]
    public async Task WycofanieJednostkiPracyWycofujeZlecenie()
    {
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);

        await Should.ThrowAsync<InvalidOperationException>(() => uow.WykonajAsync<object?>(
            async ct =>
            {
                await new ZlecaniePracSql(uow).ZlecAsync(Zlecenie("r:1:0"), ct);
                throw new InvalidOperationException("Błąd po zleceniu.");
            },
            Ct));

        (await _baza.SkalarAsync<long>("SELECT count(*) FROM praca_w_tle")).ShouldBe(0);
    }
}
