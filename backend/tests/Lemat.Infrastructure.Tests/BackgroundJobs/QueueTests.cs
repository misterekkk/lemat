// @Role: Testy KolejkaPracSql i ProcesorPrac na PostgreSQL: pobranie z dzierżawą (priorytet, dostępność), brak podwójnego pobrania przez dwóch workerów (SKIP LOCKED), zakończenie, ponowienie z limitem prób do MARTWA, odzyskiwanie wygasłych dzierżaw, błąd jednej pracy nie zatrzymuje procesora | @DependsOn: BazaTestowa, KolejkaPracSql, ProcesorPrac, IHandlerPracy | @UsedBy: dotnet test | @Task: TASK-128
using Lemat.Application.Common;
using Lemat.Application.Common.BackgroundJobs;
using Lemat.Infrastructure.BackgroundJobs;
using Lemat.Infrastructure.Tests.Persistence;
using Lemat.Infrastructure.Tests.Persistence.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lemat.Infrastructure.Tests.BackgroundJobs;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class QueueTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly TypPracy[] Typy = [TypPracy.RozpoznajPismo, TypPracy.OcenRozwiazanie];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private BazaTestowa _baza = null!;

    public async ValueTask InitializeAsync() => _baza = await BazaTestowa.UtworzAsync(postgres);

    public async ValueTask DisposeAsync() => await _baza.DisposeAsync();

    private Task WstawAsync(string typ, string klucz, int priorytet = 1, string pula = "ROZPOZNAWANIE", string dostepnaOd = "now()") =>
        _baza.WykonajAsync(
            $"INSERT INTO praca_w_tle (typ, pula, klucz_deduplikacji, ladunek, priorytet, maks_prob, dostepna_od) VALUES ('{typ}', '{pula}', '{klucz}', '{{\"k\":\"{klucz}\"}}', {priorytet}, 3, {dostepnaOd})");

    [Fact]
    public async Task PobranaPracaDostajeDzierzaweIStatusWToku()
    {
        var kolejka = new KolejkaPracSql(_baza.Zrodlo);
        await WstawAsync("ROZPOZNAJ_PISMO", "a");

        var praca = await kolejka.PobierzNastepnaAsync(Typy, "w1", TimeSpan.FromSeconds(60), Ct);

        praca.ShouldNotBeNull();
        praca.Typ.ShouldBe(TypPracy.RozpoznajPismo);
        praca.Proby.ShouldBe(1);
        praca.MaksProb.ShouldBe(3);
        praca.Ladunek.ShouldContain("\"k\"");
        (await _baza.SkalarAsync<string>("SELECT status::text FROM praca_w_tle")).ShouldBe("W_TOKU");
        (await _baza.SkalarAsync<string>("SELECT wykonawca FROM praca_w_tle")).ShouldBe("w1");
        (await _baza.SkalarAsync<bool>("SELECT dzierzawa_do > now() + interval '50 seconds' FROM praca_w_tle")).ShouldBeTrue();
        (await kolejka.PobierzNastepnaAsync(Typy, "w2", TimeSpan.FromSeconds(60), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task KolejkaRespektujePriorytetDostepnoscITypy()
    {
        var kolejka = new KolejkaPracSql(_baza.Zrodlo);
        await WstawAsync("ROZPOZNAJ_PISMO", "niski", priorytet: 5);
        await WstawAsync("OCEN_ROZWIAZANIE", "wysoki", priorytet: 1, pula: "OCENIANIE");
        await WstawAsync("ROZPOZNAJ_PISMO", "przyszla", priorytet: 1, dostepnaOd: "now() + interval '1 hour'");
        await WstawAsync("AKTUALIZUJ_PROFIL", "obcy", priorytet: 1, pula: "POMOCNICZA");

        (await kolejka.PobierzNastepnaAsync(Typy, "w", TimeSpan.FromSeconds(60), Ct))!.Typ.ShouldBe(TypPracy.OcenRozwiazanie);
        (await kolejka.PobierzNastepnaAsync(Typy, "w", TimeSpan.FromSeconds(60), Ct))!.Ladunek.ShouldContain("niski");
        (await kolejka.PobierzNastepnaAsync(Typy, "w", TimeSpan.FromSeconds(60), Ct)).ShouldBeNull();
        (await kolejka.PobierzNastepnaAsync([], "w", TimeSpan.FromSeconds(60), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task DwajWorkerzyNaJednejBazieNieWykonujaTejSamejPracy()
    {
        var kolejka = new KolejkaPracSql(_baza.Zrodlo);
        for (var i = 0; i < 40; i++)
        {
            await WstawAsync("ROZPOZNAJ_PISMO", $"p{i}");
        }

        async Task<List<long>> PetlaAsync(string wykonawca)
        {
            var wzieto = new List<long>();
            while (await kolejka.PobierzNastepnaAsync(Typy, wykonawca, TimeSpan.FromSeconds(60), Ct) is { } praca)
            {
                wzieto.Add(praca.Id);
            }

            return wzieto;
        }

        var wyniki = await Task.WhenAll(PetlaAsync("w1"), PetlaAsync("w2"), PetlaAsync("w3"));

        var wszystkie = wyniki.SelectMany(w => w).ToList();
        wszystkie.Count.ShouldBe(40);
        wszystkie.Distinct().Count().ShouldBe(40);
        (await _baza.SkalarAsync<long>("SELECT count(*) FROM praca_w_tle WHERE proby <> 1")).ShouldBe(0);
    }

    [Fact]
    public async Task ZakonczenieWymagaWlasnejDzierzawy()
    {
        var kolejka = new KolejkaPracSql(_baza.Zrodlo);
        await WstawAsync("ROZPOZNAJ_PISMO", "a");
        var praca = (await kolejka.PobierzNastepnaAsync(Typy, "w1", TimeSpan.FromSeconds(60), Ct))!;

        (await kolejka.ZakonczAsync(praca.Id, "obcy", Ct)).ShouldBeFalse();
        (await kolejka.ZakonczAsync(praca.Id, "w1", Ct)).ShouldBeTrue();

        (await _baza.SkalarAsync<string>("SELECT status::text FROM praca_w_tle")).ShouldBe("ZAKONCZONA");
    }

    [Fact]
    public async Task PonowieniaKonczaSiePoWyczerpaniuProbStatusemMartwa()
    {
        var kolejka = new KolejkaPracSql(_baza.Zrodlo);
        await WstawAsync("ROZPOZNAJ_PISMO", "a");

        for (var proba = 1; proba <= 3; proba++)
        {
            var praca = await kolejka.PobierzNastepnaAsync(Typy, "w1", TimeSpan.FromSeconds(60), Ct);
            praca.ShouldNotBeNull();
            praca.Proby.ShouldBe(proba);
            var status = await kolejka.PonowAsync(praca.Id, "w1", "błąd testowy", TimeSpan.Zero, Ct);
            status.ShouldBe(proba < 3 ? StatusPracy.Oczekuje : StatusPracy.Martwa);
        }

        (await _baza.SkalarAsync<string>("SELECT status::text FROM praca_w_tle")).ShouldBe("MARTWA");
        (await _baza.SkalarAsync<string>("SELECT ostatni_blad FROM praca_w_tle")).ShouldBe("błąd testowy");
        (await kolejka.PobierzNastepnaAsync(Typy, "w1", TimeSpan.FromSeconds(60), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task WygasleDzierzawyWracajaDoKolejki()
    {
        var kolejka = new KolejkaPracSql(_baza.Zrodlo);
        await WstawAsync("ROZPOZNAJ_PISMO", "a");
        await kolejka.PobierzNastepnaAsync(Typy, "padniety", TimeSpan.FromSeconds(60), Ct);
        await _baza.WykonajAsync("UPDATE praca_w_tle SET dzierzawa_do = now() - interval '1 second'");

        (await kolejka.OdzyskajDzierzawyAsync(Ct)).ShouldBe(1);

        var ponownie = await kolejka.PobierzNastepnaAsync(Typy, "zdrowy", TimeSpan.FromSeconds(60), Ct);
        ponownie.ShouldNotBeNull();
        ponownie.Proby.ShouldBe(2);
    }

    private sealed class ZawszeBlad(TypPracy typ) : IHandlerPracy
    {
        public int Wywolania { get; private set; }

        public TypPracy Typ => typ;

        public Task WykonajAsync(PracaWTle praca, CancellationToken ct)
        {
            Wywolania++;
            throw new InvalidOperationException("Awaria handlera.");
        }
    }

    private sealed class Licznik(TypPracy typ) : IHandlerPracy
    {
        public List<long> Wykonane { get; } = [];

        public TypPracy Typ => typ;

        public Task WykonajAsync(PracaWTle praca, CancellationToken ct)
        {
            Wykonane.Add(praca.Id);
            return Task.CompletedTask;
        }
    }

    private ProcesorPrac Procesor(params IHandlerPracy[] handlery)
    {
        var uslugi = new ServiceCollection();
        foreach (var handler in handlery)
        {
            uslugi.AddSingleton(handler);
        }

        var opcje = new LematOpcje();
        opcje.Poc.OdstepPonowieniaMs = 0;
        return new ProcesorPrac(
            new KolejkaPracSql(_baza.Zrodlo),
            uslugi.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            opcje,
            NullLogger<ProcesorPrac>.Instance);
    }

    [Fact]
    public async Task ProcesorPonawiaPraceZBledemDoMartwejIKontynuujeZInnymi()
    {
        var blad = new ZawszeBlad(TypPracy.RozpoznajPismo);
        var dobry = new Licznik(TypPracy.OcenRozwiazanie);
        var procesor = Procesor(blad, dobry);
        await WstawAsync("ROZPOZNAJ_PISMO", "zla", priorytet: 1);
        await WstawAsync("OCEN_ROZWIAZANIE", "dobra", priorytet: 2, pula: "OCENIANIE");

        var wykonane = 0;
        while (await procesor.PrzetworzJednaAsync(Ct))
        {
            wykonane++;
        }

        wykonane.ShouldBe(4);
        blad.Wywolania.ShouldBe(3);
        dobry.Wykonane.Count.ShouldBe(1);
        (await _baza.SkalarAsync<string>("SELECT status::text FROM praca_w_tle WHERE typ = 'ROZPOZNAJ_PISMO'")).ShouldBe("MARTWA");
        (await _baza.SkalarAsync<string>("SELECT ostatni_blad FROM praca_w_tle WHERE typ = 'ROZPOZNAJ_PISMO'")).ShouldBe("InvalidOperationException: Awaria handlera.");
        (await _baza.SkalarAsync<string>("SELECT status::text FROM praca_w_tle WHERE typ = 'OCEN_ROZWIAZANIE'")).ShouldBe("ZAKONCZONA");
    }
}
