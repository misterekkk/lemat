# Lemat — mapowanie EF Core 10 (Npgsql) na schemat PostgreSQL

| Pole | Wartość |
|---|---|
| Dokument | `docs/spec/ef-core.md` |
| Wersja | 3.1 (pętla 3.x, iteracja 2) |
| Dotyczy | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 3.6 (agregaty), 7 (model danych), 13.4 (prace w tle); schemat: [`docs/spec/ddl.md`](ddl.md) |
| Projekt | `backend/src/Lemat.Infrastructure/Persistence/` |

Plik podaje kontrakt mapowania: `LematDbContext`, konwencje i konwersje,
konfiguracje `IEntityTypeConfiguration<T>` głównych agregatów (pełne
fragmenty istotne dla niezmienników), tabelę pokrycia wszystkich 58 tabel
oraz strategię migracji. DDL z `ddl.md` jest źródłem prawdy: konfiguracja,
która daje inny schemat, jest błędem wykrywanym testem
`SchematZgodnyZDdlTest` (sekcja 8).

## 1. Pakiety i rejestracja

| Pakiet | Projekt | Uwagi |
|---|---|---|
| `Microsoft.EntityFrameworkCore` 10 | `Lemat.Infrastructure` | |
| `Npgsql.EntityFrameworkCore.PostgreSQL` 10 | `Lemat.Infrastructure` | `MapEnum`, tablice, `jsonb`, `xmin` |
| `EFCore.NamingConventions` 10 | `Lemat.Infrastructure` | `UseSnakeCaseNamingConvention()` — nazwy kolumn z właściwości PascalCase (ADR-049); nazwy tabel i indeksów są ustawiane jawnie |
| `Microsoft.EntityFrameworkCore.Design` 10 | `Lemat.Infrastructure` (`PrivateAssets=all`) | generowanie migracji i `efbundle` |

```csharp
// Lemat.Infrastructure/Persistence/RejestracjaPersistence.cs
public static IServiceCollection DodajPersistence(this IServiceCollection uslugi, LematOpcje opcje, RodzajProcesu proces)
{
    var zrodlo = new NpgsqlDataSourceBuilder(opcje.Baza.CiagPolaczenia(proces))      // pule wg ADR-059
        .MapujTypyWyliczeniowe()                                                    // sekcja 3.2 (typy ADO.NET)
        .EnableDynamicJson()                                                        // JsonDocument dla jsonb bez schematu
        .Build();
    uslugi.AddSingleton(zrodlo);
    uslugi.AddDbContext<LematDbContext>((sp, b) => b
        .UseNpgsql(zrodlo, o => o
            .MapujTypyWyliczenioweEf()                                              // sekcja 3.2 (model EF)
            .MigrationsHistoryTable("__historia_migracji")
            .CommandTimeout(proces == RodzajProcesu.Api ? 5 : 30))
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(sp.GetRequiredService<ZnacznikiCzasuInterceptor>(),
                         sp.GetRequiredService<WersjaAgregatuInterceptor>())
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));               // zapytania odczytu bez śledzenia
    uslugi.AddScoped<IJednostkaPracy, JednostkaPracyEf>();
    uslugi.AddScoped<IZlecaniePrac, ZlecaniePracSql>();
    return uslugi;
}
```

`EnableRetryOnFailure` nie jest włączane (ADR-056 p. 1). Repozytoria
agregatów wczytują je z `AsTracking()`; zapytania (modele odczytu, 6.2)
działają bez śledzenia.

## 2. `LematDbContext`

```csharp
public sealed class LematDbContext(DbContextOptions<LematDbContext> opcje) : DbContext(opcje)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasPostgresExtension("citext").HasPostgresExtension("pg_trgm");
        model.ApplyConfigurationsFromAssembly(typeof(LematDbContext).Assembly);   // wszystkie *EntityTypeConfiguration
        model.Ignore<IZdarzenieDomenowe>();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder konwencje)
    {
        KonwersjeIdentyfikatorow.Zarejestruj(konwencje);                            // sekcja 3.1
        konwencje.Properties<DateTimeOffset>().HaveColumnType("timestamptz");
        konwencje.Properties<DateOnly>().HaveColumnType("date");
        konwencje.Properties<PoziomTrudnosci>().HaveConversion<PoziomTrudnosciConverter>().HaveColumnType("smallint");
        konwencje.Properties<WariantRozwiazania>().HaveConversion<WariantRozwiazaniaConverter>().HaveColumnType("smallint");
        konwencje.Properties<PewnoscOceny>().HaveConversion<PewnoscOcenyConverter>().HavePrecision(5, 4);
        konwencje.Properties<PewnoscSurowa>().HaveConversion<PewnoscSurowaConverter>().HavePrecision(5, 4);
        konwencje.Properties<PoziomOpanowania>().HaveConversion<PoziomOpanowaniaConverter>().HavePrecision(4, 3);
        konwencje.Properties<string>().AreUnicode();                                // text
    }
}
```

`DbSet<T>` nie są publiczne: repozytoria używają `Set<T>()`, a endpointy
nie mają dostępu do kontekstu (6.5, `EndpointyTylkoPrzezAplikacje`).

## 3. Konwersje

### 3.1 Identyfikatory silnie typowane

Każdy `readonly record struct XxxId(Guid Wartosc)` z `Lemat.Domain` (3.6) ma
konwerter generowany refleksją (bez ręcznych klas na typ):

```csharp
public static class KonwersjeIdentyfikatorow
{
    public static void Zarejestruj(ModelConfigurationBuilder konwencje)
    {
        foreach (var typ in typeof(Agregat<>).Assembly.GetTypes()
                     .Where(t => t is { IsValueType: true, Name: var n } && n.EndsWith("Id") && t.GetProperty("Wartosc") is not null))
        {
            var wartosc = typ.GetProperty("Wartosc")!.PropertyType;                // Guid albo int (DzialId, UmiejetnoscId)
            var konwerter = typeof(IdentyfikatorConverter<,>).MakeGenericType(typ, wartosc);
            konwencje.Properties(typ).HaveConversion(konwerter);
        }
    }
}
public sealed class IdentyfikatorConverter<TId, TWartosc>() : ValueConverter<TId, TWartosc>(
    id => (TWartosc)typeof(TId).GetProperty("Wartosc")!.GetValue(id)!,
    w => (TId)Activator.CreateInstance(typeof(TId), w)!) where TId : struct where TWartosc : struct;
```

Wyrażenia konwertera są kompilowane raz przez EF Core; test
`KazdyIdentyfikatorMaKonwerter` sprawdza, że każda właściwość typu `*Id`
w modelu ma konwerter, a `KazdyKonwerterTrafiaWTypKolumny` — że typ
kolumny (`uuid`/`integer`) zgadza się z DDL.

### 3.2 Typy wyliczeniowe PostgreSQL

Wartości w bazie są `UPPER_SNAKE` (ADR-049), a członkowie enumów C# —
`PascalCase`. Npgsql używa translatora nazw:

```csharp
public sealed class TlumaczNazwUpperSnake : INpgsqlNameTranslator
{
    public string TranslateTypeName(string nazwa) => KonwencjaNazw.NaSnakeCase(nazwa);        // StatusRozwiazania → status_rozwiazania
    public string TranslateMemberName(string nazwa) => KonwencjaNazw.NaSnakeCase(nazwa).ToUpperInvariant();
                                                                                         // OtwarteZDowodem → OTWARTE_Z_DOWODEM
}

public static class MapowanieTypowWyliczeniowych
{
    private static readonly INpgsqlNameTranslator T = new TlumaczNazwUpperSnake();
    // 42 typy z ddl.md; ta sama lista dla NpgsqlDataSourceBuilder i dla modelu EF
    public static readonly (Type Typ, string NazwaPg)[] Lista =
    [
        (typeof(RolaUzytkownika), "rola_uzytkownika"), (typeof(StatusKonta), "status_konta"),
        (typeof(PoziomMatury), "poziom_matury"), (typeof(RodzajZadania), "rodzaj_zadania"),
        (typeof(FormaZamknieta), "forma_zamknieta"), (typeof(PochodzenieZadania), "pochodzenie_zadania"),
        (typeof(ZasiegZadania), "zasieg_zadania"), (typeof(StatusZadania), "status_zadania"),
        (typeof(StatusWkladki), "status_wkladki"), (typeof(StatusRozwiazania), "status_rozwiazania"),
        (typeof(ZrodloTranskrypcji), "zrodlo_transkrypcji"), (typeof(StatusOceny), "status_oceny"),
        (typeof(ZrodloOceny), "zrodlo_oceny"), (typeof(TypBledu), "typ_bledu"),
        (typeof(ZrodloWskazowki), "zrodlo_wskazowki"), (typeof(PowodWeryfikacji), "powod_weryfikacji"),
        (typeof(StatusWeryfikacji), "status_weryfikacji"), (typeof(PowodAnulowania), "powod_anulowania"),
        (typeof(TypKolejkiWeryfikacji), "typ_kolejki_weryfikacji"), (typeof(TypElementuSciezki), "typ_elementu_sciezki"),
        (typeof(CelElementu), "cel_elementu"), (typeof(StatusElementuSciezki), "status_elementu_sciezki"),
        (typeof(StrategiaRekomendacji), "strategia_rekomendacji"), (typeof(RolaModelu), "rola_modelu"),
        (typeof(TypPytania), "typ_pytania"), (typeof(KontekstDecyzji), "kontekst_decyzji"),
        (typeof(ZrodloWyniku), "zrodlo_wyniku"), (typeof(ZrodloProbki), "zrodlo_probki"),
        (typeof(ZrodloProgu), "zrodlo_progu"), (typeof(ZrodloKalibratora), "zrodlo_kalibratora"),
        (typeof(PowodUniewaznieniaWyniku), "powod_uniewaznienia_wyniku"), (typeof(StatusPracy), "status_pracy"),
        (typeof(StatusZlecenia), "status_zlecenia"), (typeof(StatusImportu), "status_importu"),
        (typeof(StatusTestu), "status_testu"), (typeof(DecyzjaPrzegladu), "decyzja_przegladu"),
        (typeof(RezultatWalidacji), "rezultat_walidacji"), (typeof(InicjalizacjaOpanowania), "inicjalizacja_opanowania"),
        (typeof(StatusZgloszeniaZadania), "status_zgloszenia_zadania"), (typeof(TypZdarzeniaRodo), "typ_zdarzenia_rodo"),
        (typeof(ZrodloZdarzeniaRodo), "zrodlo_zdarzenia_rodo"), (typeof(TypBezpiecznika), "typ_bezpiecznika"),
        (typeof(PowodEskalacji), "powod_eskalacji"),
    ];
}
```

`MapujTypyWyliczeniowe()` (data source) i `MapujTypyWyliczenioweEf()`
(model, `NpgsqlDbContextOptionsBuilder.MapEnum(typ, nazwaPg, nameTranslator: T)`)
iterują tę samą listę. Test `KazdyTypWyliczeniowyDdlJestZmapowany`
porównuje listę z `SELECT typname FROM pg_type WHERE typtype = 'e'` po
migracjach (42 = 42) i wartości `enum_range(NULL::<typ>)` z członkami
enumu (kolejność i pisownia).

**Wartości z CHECK tekstowym** (kolumny `text` z listą dozwolonych
wartości) mapuje konwerter `UpperSnakeEnumConverter<TEnum> :
ValueConverter<TEnum, string>`: `rozwiazanie.kod_bledu_rozpoznawania`
(`KodBleduRozpoznawania`), `rekomendacja.regula` (`RegulaRekomendacji`,
członkowie z atrybutem `[NazwaKodu("R-SERIA")]`, ADR-049 p. 4),
`praca_w_tle.pula` (`PulaPracy`), `praca_w_tle.typ` (`TypPracy`),
`kalibrator_pewnosci.metoda` (`MetodaKalibratora`), `powiadomienie.typ`
(`TypPowiadomienia`), `arkusz_cke.sesja` (`SesjaArkusza`),
`limit_szybkosci_modelu.pula` (`PulaOgranicznika`, wartość `*` →
`Proces`).

**Tablice typów wyliczeniowych** (`weryfikacja.powody`,
`powody_poczatkowe`) są mapowane jako `List<PowodWeryfikacji>` (pole
prywatne agregatu, dostęp przez pole), co Npgsql zapisuje jako
`powod_weryfikacji[]`.

### 3.3 Kolumny `jsonb`

| Rodzaj kolumny | Mapowanie | Kolumny |
|---|---|---|
| Schemat stały, wartość obiektu domeny (7.5) | `OwnsOne(…, b => b.ToJson())`; nazwy kluczy JSON z `HasJsonPropertyName` = `snake_case` | `zadanie_wersja.opcje_zamkniete`, `rozwiazanie_wzorcowe`, `wynik_koncowy_wzorcowy`; `weryfikacja.zgloszenie`, `opinia_pomocnicza`; `transkrypcja.zakres_poprawek`, `zgodnosc_poprawek`; `krok_transkrypcji.obszar`, `niepewne_fragmenty`, `zastapienia_danych_osobowych`; `zdjecie_rozwiazania.obszary_maskowania`; `konfiguracja_modelu.parametry`; `zlecenie_generowania.wywolania_narzedzi` |
| Schemat stały, wyłącznie do zapisu i raportów (bez logiki domeny) | właściwość typu rekordu + konwerter `JsonbConverter<T>` (System.Text.Json, kontekst źródłowy `LematJsonKontekst`, `SnakeCaseLower`, `UnsafeRelaxedJsonEscaping`) | `rekomendacja.cechy`, `decyzja`, `uzasadnienie_fakty`; `walidacja_zadania.sprawdzenia`; `probka_kalibracyjna.kryteria`; `kalibrator_pewnosci.punkty`; `import_arkusza.raport`; `decyzja_modelu.opcje`, `wybor`, `rozklad`; `szkic_rozstrzygniecia.dane` |
| Bez schematu | `JsonDocument` (`EnableDynamicJson`) | `praca_w_tle.ladunek`, `powiadomienie.dane`, `dziennik_audytu.dane`, `zdarzenie_rodo.dane`, `klucz_idempotencji.cialo_http`, `odpowiedz.wybor` |

`UnsafeRelaxedJsonEscaping` jest konieczne, bo domyślny koder System.Text.Json
zapisuje `<`, `>`, `&` jako `<`, `>`, `&`; treść LaTeX w
`jsonb` musi być równa bajtowo tekstowi kroku (ADR-058 p. 4).

## 4. Współbieżność

| Rodzaj | Tabele | Konfiguracja | Zmiana wartości |
|---|---|---|---|
| `version` zmieniane przez aplikację | `uzytkownik`, `uczen`, `nauczyciel`, `wkladka_teoretyczna`, `zadanie`, `klasa`, `zestaw_zadan`, `profil_ucznia`, `sciezka_nauki`, `rozwiazanie`, `stan_bezpiecznika` | `Property(x => x.Version).IsConcurrencyToken()` | `WersjaAgregatuInterceptor` (`SavingChangesAsync`) zwiększa `Version` każdego agregatu w stanie `Modified`, którego zmieniona właściwość nie jest na liście `[BezZmianyWersji]` agregatu (ADR-029: `zadanie` przy zmianie wyłącznie `updated_at`) |
| `version` zmieniane przez trigger | `weryfikacja` (`trg_weryfikacja_wersja`, ADR-037) | `Property(w => w.Version).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate()` | EF Core nie wysyła `version` w `SET`, warunek `WHERE version = @oryginalna` i odczyt `RETURNING version`; interceptor pomija `Weryfikacja` |
| `xmin` (systemowy licznik wiersza PostgreSQL) | `transkrypcja`, `wynik_zadania`, `zlecenie_generowania`, `import_arkusza`, `konfiguracja_modelu`, `ocena` | `Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()` | PostgreSQL przy każdej zmianie wiersza; kolumny nie ma w DDL (kolumna systemowa) |
| brak (zapis wyłącznie SQL) | `praca_w_tle`, `kalibracja_trudnosci`, `limit_szybkosci_modelu`, `wstrzymanie_w_doborze`, `klucz_idempotencji` | encja tylko do odczytu albo brak encji | warunki `UPDATE … WHERE` (13.2, 13.4, 10.1.3, C-36) |

`DbUpdateConcurrencyException` jest tłumaczony przez `Lemat.Api` na `412
KONFLIKT_WERSJI` (żądanie z `If-Match`) albo przez handler pracy w tle na
`rezultat_przestarzaly` (6.3 p. 3). ETag = `"W/\"<version>\""`; `If-Match`
ustawia wartość oryginalną: `entry.Property(x => x.Version).OriginalValue
= wersjaZNaglowka` w repozytorium przed modyfikacją.

## 5. Interceptory

| Interceptor | Zdarzenie | Działanie |
|---|---|---|
| `ZnacznikiCzasuInterceptor` | `SavingChangesAsync` | `updated_at = TimeProvider.GetUtcNow()` dla encji `Modified` z właściwością `UpdatedAt`; `created_at` ma `DEFAULT now()` w bazie i `ValueGeneratedOnAdd` |
| `WersjaAgregatuInterceptor` | `SavingChangesAsync` | sekcja 4 |
| — | — | **brak** interceptora `NOTIFY` i zlecania prac (ADR-056): `NOTIFY` wysyłają triggery, prace zleca `ZlecaniePracSql` |

## 6. Konfiguracje agregatów (kluczowe fragmenty)

Wszystkie klasy są w `Lemat.Infrastructure/Persistence/Konfiguracje/<Modul>/`
i nazywają się `<Encja>EntityTypeConfiguration` (3.1 p. 7). Wspólne:
`builder.Ignore(a => a.Zdarzenia)`; kolekcje encji wewnętrznych przez pola
prywatne (`UsePropertyAccessMode(PropertyAccessMode.Field)`).

### 6.1 Konta

```csharp
public sealed class UzytkownikEntityTypeConfiguration : IEntityTypeConfiguration<Uzytkownik>
{
    public void Configure(EntityTypeBuilder<Uzytkownik> b)
    {
        b.ToTable("uzytkownik", t =>
        {
            t.HasCheckConstraint("ck_uzytkownik_karencja", "(status = 'USUWANE') = (usuniecie_zaplanowane_at IS NOT NULL)");
            t.HasCheckConstraint("ck_uzytkownik_status_przed", "(status = 'USUWANE') = (status_przed_wnioskiem IS NOT NULL)");
        });
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).HasColumnType("citext").IsRequired();
        b.HasIndex(u => u.Email).IsUnique();
        b.Property<string>("HasloHash").HasColumnName("haslo_hash").IsRequired();     // pole prywatne, poza API domeny
        b.Property(u => u.Imie).HasMaxLength(60);
        b.Property(u => u.Nazwisko).HasMaxLength(80);
        b.OwnsOne(u => u.OkresKarencji, k =>                                         // VO w kolumnach tabeli właściciela
        {
            k.Property(o => o.UsuniecieZaplanowaneAt).HasColumnName("usuniecie_zaplanowane_at");
            k.Property(o => o.StatusPrzedWnioskiem).HasColumnName("status_przed_wnioskiem");
        });
        b.Navigation(u => u.OkresKarencji).IsRequired(false);
        b.Property(u => u.Version).IsConcurrencyToken();
        b.Ignore(u => u.CzyProcesyWstrzymane);
    }
}

public sealed class NauczycielEntityTypeConfiguration : IEntityTypeConfiguration<Nauczyciel>
{
    public void Configure(EntityTypeBuilder<Nauczyciel> b)
    {
        b.ToTable("nauczyciel", t =>
        {
            t.HasCheckConstraint("ck_nauczyciel_deklarowane", "NOT weryfikator_globalny OR deklarowane_minuty_weryfikacji >= 90");
            t.HasCheckConstraint("ck_nauczyciel_umowa", "NOT weryfikator_globalny OR umowa_poufnosci_at IS NOT NULL");
        });
        b.HasKey(n => n.Id);
        b.Property(n => n.Id).HasColumnName("uzytkownik_id");
        b.HasOne<Uzytkownik>().WithOne().HasForeignKey<Nauczyciel>(n => n.Id).OnDelete(DeleteBehavior.Cascade);
        b.Property(n => n.DeklarowaneMinutyWeryfikacji).HasColumnType("smallint");
        b.Property(n => n.Version).IsConcurrencyToken();
    }
}

public sealed class TokenOdswiezaniaEntityTypeConfiguration : IEntityTypeConfiguration<TokenOdswiezania>
{
    public void Configure(EntityTypeBuilder<TokenOdswiezania> b)
    {
        b.ToTable("token_odswiezania");
        b.HasKey(t => t.Id);
        b.HasIndex(t => t.TokenHash).IsUnique();
        b.HasIndex(t => t.RodzinaId).HasDatabaseName("ix_token_rodzina").HasFilter("uniewazniony_at IS NULL");
        b.HasOne<TokenOdswiezania>().WithMany().HasForeignKey(t => t.ZastapionyPrzez).OnDelete(DeleteBehavior.SetNull);
        b.Property(t => t.UzyciaWOknieTolerancji).HasColumnType("smallint");
    }
}
```

Rotacja z oknem tolerancji (ADR-057) nie używa śledzenia zmian: jedno
polecenie `ExecuteUpdateAsync` z warunkiem `zastapiony_at IS NULL AND
uniewazniony_at IS NULL` i drugie dla gałęzi okna tolerancji
(`uzycia_w_oknie_tolerancji < 5`), żeby dwa równoległe odświeżenia nie
utworzyły dwóch następców.

### 6.2 Treści

```csharp
public sealed class ZadanieEntityTypeConfiguration : IEntityTypeConfiguration<Zadanie>
{
    public void Configure(EntityTypeBuilder<Zadanie> b)
    {
        b.ToTable("zadanie", t =>
        {
            t.HasCheckConstraint("ck_zadanie_forma", "(rodzaj = 'ZAMKNIETE') = (forma_zamknieta IS NOT NULL)");
            t.HasCheckConstraint("ck_zadanie_arkusz", "(pochodzenie = 'BAZOWE_ARKUSZ_CKE') = (arkusz_cke_id IS NOT NULL)");
            t.HasCheckConstraint("ck_zadanie_zlecenie", "(pochodzenie = 'WYGENEROWANE') = (zlecenie_generowania_id IS NOT NULL)");
            t.HasCheckConstraint("ck_zadanie_zasieg_cke", "pochodzenie <> 'BAZOWE_ARKUSZ_CKE' OR zasieg = 'GLOBALNY'");
            t.HasCheckConstraint("ck_zadanie_diagnostyczne", "NOT diagnostyczne OR (rodzaj = 'ZAMKNIETE' AND zasieg = 'GLOBALNY')");
        });
        b.HasKey(z => z.Id);
        b.HasMany(z => z.Wersje).WithOne().HasForeignKey("zadanie_id").IsRequired();
        b.Navigation(z => z.Wersje).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        b.Property<ZadanieWersjaId?>("aktualna_wersja_id");                          // FK DEFERRABLE (ddl.md); ustawia repozytorium
        b.Ignore(z => z.AktualnaWersja).Ignore(z => z.CzyBazowe).Ignore(z => z.CzyWygenerowane);
        b.HasIndex(z => new { z.PoziomMatury, z.Status, z.Zasieg, z.Rodzaj }).HasDatabaseName("ix_zadanie_dobor");
        b.Property(z => z.Version).IsConcurrencyToken();
    }
}

public sealed class ZadanieWersjaEntityTypeConfiguration : IEntityTypeConfiguration<ZadanieWersja>
{
    public void Configure(EntityTypeBuilder<ZadanieWersja> b)
    {
        b.ToTable("zadanie_wersja");
        b.HasKey(w => w.Id);
        b.HasIndex("zadanie_id", nameof(ZadanieWersja.NrWersji)).IsUnique();
        b.Property(w => w.TrescMd).HasMaxLength(8000);
        b.OwnsOne(w => w.OpcjeZamkniete, j => j.ToJson("opcje_zamkniete"));
        b.OwnsOne(w => w.RozwiazanieWzorcowe, j => { j.ToJson("rozwiazanie_wzorcowe"); j.OwnsMany(r => r.Warianty, v => v.OwnsMany(x => x.Kroki)); });
        b.OwnsOne(w => w.WynikKoncowyWzorcowy, j => j.ToJson("wynik_koncowy_wzorcowy"));
        b.OwnsMany(w => w.KryteriaOceniania, k =>                                    // tabela kryterium_oceniania
        {
            k.ToTable("kryterium_oceniania");
            k.WithOwner().HasForeignKey("zadanie_wersja_id");
            k.Property<Guid>("id").HasDefaultValueSql("gen_random_uuid()");
            k.HasKey("id");
            k.HasIndex("zadanie_wersja_id", nameof(KryteriumOceniania.Wariant), nameof(KryteriumOceniania.NrKryterium)).IsUnique();
        });
        b.Navigation(w => w.KryteriaOceniania).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.ToTable(t => t.HasTrigger("trg_suma_kryteriow"));
    }
}
```

`KalibracjaTrudnosci` i `WstrzymanieWDoborze` są osobnymi encjami bez
`version` (ADR-029, C3-09); aktualizacja `kalibracja_trudnosci` to wyłącznie
polecenie SQL 10.1.3 w `KalibracjaTrudnosciRepozytorium.ZastosujWynikAsync`.

### 6.3 Rozwiązania

```csharp
public sealed class RozwiazanieEntityTypeConfiguration : IEntityTypeConfiguration<Rozwiazanie>
{
    public void Configure(EntityTypeBuilder<Rozwiazanie> b)
    {
        b.ToTable("rozwiazanie", t =>
        {
            t.HasCheckConstraint("ck_rozwiazanie_kontekst", "element_sciezki_id IS NOT NULL OR przypisanie_id IS NOT NULL");
            t.HasCheckConstraint("ck_rozwiazanie_najnowsza", "(najnowsza_transkrypcja_id IS NULL) = (najnowsza_wersja_transkrypcji IS NULL)");
            t.HasCheckConstraint("ck_rozwiazanie_rozpoznanie", "ostatnie_rozpoznanie_od_at >= przeslano_at");
            t.HasTrigger("trg_kontekst_proby");
            t.HasTrigger("trg_rozwiazanie_powiadom");
        });
        b.HasKey(r => r.Id);
        b.Property(r => r.KodBleduRozpoznawania).HasConversion<UpperSnakeEnumConverter<KodBleduRozpoznawania>>();
        b.Property(r => r.LiczbaPrzeslan).HasColumnType("smallint");
        b.Property(r => r.LiczbaPonowienRozpoznania).HasColumnType("smallint");
        b.Property(r => r.CzasSystemowy).HasColumnName("czas_systemowy_ms")
            .HasConversion(v => v.HasValue ? (int?)v.Value.TotalMilliseconds : null, v => v.HasValue ? TimeSpan.FromMilliseconds(v.Value) : null);
        b.HasMany(r => r.Zdjecia).WithOne().HasForeignKey("rozwiazanie_id").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(r => r.Zdjecia).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        b.HasIndex(r => r.ElementSciezkiId).IsUnique().HasDatabaseName("ux_rozwiazanie_element")
            .HasFilter("element_sciezki_id IS NOT NULL AND status NOT IN ('PORZUCONE','ANULOWANE')");
        b.HasIndex(r => new { r.UczenId, r.PrzypisanieId, r.ZadanieId }).IsUnique().HasDatabaseName("ux_rozwiazanie_przypisanie")
            .HasFilter("przypisanie_id IS NOT NULL AND status NOT IN ('PORZUCONE','ANULOWANE')");
        b.Property(r => r.Version).IsConcurrencyToken();
    }
}

public sealed class TranskrypcjaEntityTypeConfiguration : IEntityTypeConfiguration<Transkrypcja>
{
    public void Configure(EntityTypeBuilder<Transkrypcja> b)
    {
        b.ToTable("transkrypcja");
        b.HasKey(t => t.Id);
        b.HasIndex(t => new { t.RozwiazanieId, t.Wersja }).IsUnique();
        b.OwnsOne(t => t.ZakresPoprawek, j => j.ToJson("zakres_poprawek"));
        b.OwnsOne(t => t.ZgodnoscPoprawek, j => { j.ToJson("zgodnosc_poprawek"); j.OwnsMany(z => z.Kroki); });
        b.OwnsMany(t => t.Kroki, k =>                                                // tabela krok_transkrypcji
        {
            k.ToTable("krok_transkrypcji");
            k.WithOwner().HasForeignKey("transkrypcja_id");
            k.Property<Guid>("id").HasDefaultValueSql("gen_random_uuid()");
            k.HasKey("id");
            k.HasIndex("transkrypcja_id", nameof(KrokTranskrypcji.Nr)).IsUnique();
            k.OwnsOne(x => x.Obszar, j => j.ToJson("obszar"));
            k.OwnsMany(x => x.NiepewneFragmenty, j => j.ToJson("niepewne_fragmenty"));
            k.OwnsMany(x => x.ZastapieniaDanychOsobowych, j => j.ToJson("zastapienia_danych_osobowych"));
        });
        b.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
```

Zapis `zgodnosc_poprawek` (jednokrotny, C-35, C-38) jest poleceniem SQL
`UPDATE transkrypcja SET zgodnosc_poprawek = @r WHERE id = @id AND
zgodnosc_poprawek IS NULL` w `TranskrypcjaRepozytorium.ZapiszZgodnoscAsync`,
a nie przez śledzenie zmian, bo warunek `IS NULL` jest częścią niezmiennika.

### 6.4 Ocenianie i Weryfikacje

```csharp
public sealed class OcenaEntityTypeConfiguration : IEntityTypeConfiguration<Ocena>
{
    public void Configure(EntityTypeBuilder<Ocena> b)
    {
        b.ToTable("ocena", t => { t.HasTrigger("trg_ocena_niezmienna"); t.HasTrigger("trg_ocena_powiadom"); });
        b.HasKey(o => o.Id);
        b.HasIndex(o => new { o.RozwiazanieId, o.Wersja }).IsUnique();
        b.HasIndex(o => o.RozwiazanieId).IsUnique().HasDatabaseName("ux_ocena_aktualna").HasFilter("status <> 'ZASTAPIONA'");
        b.OwnsOne(o => o.Wskazowka, w =>                                             // VO w dwóch kolumnach
        {
            w.Property(x => x.Tresc).HasColumnName("wskazowka").HasMaxLength(400);
            w.Property(x => x.Zrodlo).HasColumnName("zrodlo_wskazowki");
        });
        b.HasMany(o => o.Kryteria).WithOne().HasForeignKey("ocena_id").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(o => o.Kryteria).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Ignore(o => o.Widocznosc);                                                 // wyliczana (3.5 #111)
        b.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}

public sealed class WeryfikacjaEntityTypeConfiguration : IEntityTypeConfiguration<Weryfikacja>
{
    public void Configure(EntityTypeBuilder<Weryfikacja> b)
    {
        b.ToTable("weryfikacja", t =>
        {
            t.HasTrigger("trg_weryfikacja_wersja");
            t.HasTrigger("trg_powody_poczatkowe");
            t.HasTrigger("trg_weryfikacja_powiadom");
        });
        b.HasKey(w => w.Id);
        b.Property<List<PowodWeryfikacji>>("_powody").HasColumnName("powody");
        b.Property<List<PowodWeryfikacji>>("_powodyPoczatkowe").HasColumnName("powody_poczatkowe");
        b.Ignore(w => w.Powody).Ignore(w => w.PowodyPoczatkowe);                     // widoki IReadOnlySet nad polami
        b.OwnsOne(w => w.Blokada, k =>                                               // Blokada weryfikacji: trzy kolumny
        {
            k.Property(x => x.PodjetaPrzez).HasColumnName("podjeta_przez");
            k.Property(x => x.PodjetaAt).HasColumnName("podjeta_at");
            k.Property(x => x.ZablokowanaDo).HasColumnName("zablokowana_do");
        });
        b.OwnsOne(w => w.Zgloszenie, j => j.ToJson("zgloszenie"));
        b.OwnsOne(w => w.OpiniaPomocnicza, j => { j.ToJson("opinia_pomocnicza"); j.OwnsMany(o => o.Kryteria); });
        b.HasIndex(w => w.RozwiazanieId).IsUnique().HasDatabaseName("ux_weryfikacja_otwarta").HasFilter("status IN ('OCZEKUJE','W_TRAKCIE')");
        b.Property(w => w.Version).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();   // trigger, ADR-037
    }
}
```

Podjęcie, przedłużenie, zwolnienie i eskalacja Weryfikacji są poleceniami
`ExecuteUpdateAsync` z warunkami 9.6.1/11.4.2 (C-07), bo jedno polecenie
`UPDATE … RETURNING` daje atomowość bez odczytu; metody agregatu 3.6 są
wykonywane na wierszu zwróconym przez `RETURNING` (sprawdzenie niezmienników i
wartości `PodjetaAt`), a repozytorium porównuje wynik metody z wierszem i przy
różnicy rzuca `InvalidOperationException` (test `PodjecieSqlZgodneZAgregatem`
generuje 2 000 losowych stanów FsCheck i porównuje oba wyniki).

### 6.5 Profil, Rekomendacje, Kalibracja

```csharp
public sealed class ProfilUczniaEntityTypeConfiguration : IEntityTypeConfiguration<ProfilUcznia>
{
    public void Configure(EntityTypeBuilder<ProfilUcznia> b)
    {
        b.ToTable("profil_ucznia");
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasColumnName("uczen_id");
        b.OwnsMany<OpanowanieUmiejetnosci>("_opanowanie", o =>                     // słownik nad listą wierszy
        {
            o.ToTable("opanowanie_umiejetnosci");
            o.WithOwner().HasForeignKey("uczen_id");
            o.HasKey("uczen_id", nameof(OpanowanieUmiejetnosci.UmiejetnoscId));
            o.Property(x => x.Theta).HasPrecision(6, 3);
        });
        b.Ignore(p => p.Opanowanie);
        b.Property(p => p.Version).IsConcurrencyToken();
    }
}

public sealed class SciezkaNaukiEntityTypeConfiguration : IEntityTypeConfiguration<SciezkaNauki>
{
    public void Configure(EntityTypeBuilder<SciezkaNauki> b)
    {
        b.ToTable("sciezka_nauki");
        b.HasKey(s => s.Id);
        b.HasIndex(s => s.UczenId).IsUnique();
        b.OwnsOne(s => s.Seria, s =>
        {
            s.Property(x => x.UmiejetnoscId).HasColumnName("seria_umiejetnosc_id");
            s.Property(x => x.RozpoczetaAt).HasColumnName("seria_rozpoczeta_at");
            s.Property(x => x.LiczbaElementow).HasColumnName("seria_liczba_elementow");
            s.Property(x => x.PoziomTrudnosci).HasColumnName("seria_poziom_trudnosci");
            s.Property(x => x.KolejnePorazki).HasColumnName("seria_kolejne_porazki");
        });
        b.HasMany<ElementSciezki>("_aktywne").WithOne().HasForeignKey("sciezka_nauki_id");
        b.Ignore(s => s.AktywneElementy);
        b.Property(s => s.Version).IsConcurrencyToken();
    }
}
```

Repozytorium `SciezkaNaukiRepozytorium.WczytajAsync` wczytuje agregat z
filtrowanym `Include` wyłącznie elementów `PROPONOWANY` i `ROZPOCZETY`
(`.Include("_aktywne", e => e.Status == Proponowany || e.Status == Rozpoczety)`),
czyli ≤ 2 wiersze; 10 ostatnich elementów dla Agenta rekomendującego czyta
osobne zapytanie (C6-15 p. 4).

```csharp
public sealed class StanBezpiecznikaEntityTypeConfiguration : IEntityTypeConfiguration<StanBezpiecznika>
{
    public void Configure(EntityTypeBuilder<StanBezpiecznika> b)
    {
        b.ToTable("stan_bezpiecznika", t =>
        {
            t.HasCheckConstraint("ck_bezpiecznik_rodzaj", "rodzaj_zadania = 'OTWARTE_OBLICZENIOWE'");
            t.HasCheckConstraint("ck_bezpiecznik_dezaktywacja", "(dezaktywowano_at IS NULL) = (uzasadnienie_dezaktywacji IS NULL)");
        });
        b.Property<KonfiguracjaModeluId>("konfiguracja_modelu_id");                   // KluczBezpiecznika → 3 kolumny klucza
        b.Property<RodzajZadania>("rodzaj_zadania");
        b.Property<TypBezpiecznika>("typ");
        b.HasKey("konfiguracja_modelu_id", "rodzaj_zadania", "typ");
        b.Ignore(s => s.Id);                                                         // składany z trzech kolumn w konstruktorze materializacji
        b.Ignore(s => s.BlokadaObnizaniaProgu).Ignore(s => s.KazdaPoprawkaDoWeryfikacji);
        b.Property(s => s.Version).IsConcurrencyToken();                             // C6-08
    }
}
```

Klucz złożony `KluczBezpiecznika` jest mapowany przez trzy właściwości
cieni, a właściwość `Id` agregatu składa konstruktor materializacji
`StanBezpiecznika(KonfiguracjaModeluId konfiguracjaModeluId, RodzajZadania
rodzajZadania, TypBezpiecznika typ)` (EF Core wiąże parametry konstruktora z
właściwościami cieni o tych samych nazwach).

### 6.6 Modele i decyzje

```csharp
public sealed class DecyzjaModeluEntityTypeConfiguration : IEntityTypeConfiguration<DecyzjaModelu>
{
    public void Configure(EntityTypeBuilder<DecyzjaModelu> b)
    {
        b.ToTable("decyzja_modelu");                                                 // tabela partycjonowana (ddl.md, 0004_partycje.sql)
        b.HasKey(d => new { d.Id, d.CreatedAt });
        b.Property(d => d.SkrotStanu).HasColumnType("bytea");
        b.Property(d => d.Opcje).HasConversion<JsonbConverter<OpcjePytania?>>().HasColumnType("jsonb");
        b.Property(d => d.Rozklad).HasConversion<JsonbConverter<IReadOnlyDictionary<string, decimal>?>>().HasColumnType("jsonb");
        b.HasIndex(d => new { d.Kontekst, d.KontekstId }).HasDatabaseName("ix_decyzja_kontekst");
    }
}
```

Wiersze `decyzja_modelu` są wstawiane wsadowo (`AddRange` + `SaveChanges`
w jednostce pracy zapisu Oceny, ≤ 29 wierszy); EF Core 10 wysyła je jednym
poleceniem wielowierszowym. Migracje EF Core nie tworzą partycji — tabela
nadrzędna jest tworzona w `0004_partycje.sql`, a w modelu EF ma
`ExcludeFromMigrations()`.

## 7. Pokrycie tabel

| Tabela | Typ C# | Rodzaj | Konfiguracja / dostęp | Moduł (6.2) |
|---|---|---|---|---|
| `uzytkownik` | `Uzytkownik` | AG | 6.1 | Konta |
| `uczen` | `Uczen` | AG | `UczenEntityTypeConfiguration` (PK `uzytkownik_id`, CHECK `oswiadczenie_wieku_16`, `GrupaEksperymentu` → `procent_grupy_eksperymentu`) | Konta |
| `nauczyciel` | `Nauczyciel` | AG | 6.1 | Konta |
| `token_odswiezania` | `TokenOdswiezania` | EN (techniczna) | 6.1 | Konta |
| `proba_logowania` | `ProbaLogowania` | EN (techniczna) | tylko `INSERT` i zapytanie liczące | Konta |
| `zdarzenie_rodo` | `ZdarzenieRodo` | AG (niezmienny) | `ZdarzenieRodoEntityTypeConfiguration` (PK bez wartości domyślnej, `dane` jako `JsonDocument`) | Konta |
| `dzien_wolny` | `DzienWolny` | EN | dane referencyjne, tylko odczyt | Wspolne |
| `dzial`, `umiejetnosc`, `umiejetnosc_wymagana` | `Dzial`, `Umiejetnosc` (kolekcja `WymaganeUmiejetnosci`) | AG (referencyjne) | tylko odczyt, `HasData` z 7.4 i `umiejetnosci.yaml` | Tresci |
| `wkladka_teoretyczna` | `WkladkaTeoretyczna` | AG | `version`, indeks `ux_wkladka_opublikowana` | Tresci |
| `arkusz_cke` | `ArkuszCke` | AG | CHECK `sesja` (konwerter `SesjaArkusza`) | Tresci |
| `import_arkusza` | `ImportArkusza` | AG | `xmin`, `raport` jako `JsonbConverter<RaportImportu>` | Tresci |
| `zlecenie_generowania` | `ZlecenieGenerowania` | AG | `xmin`, `wywolania_narzedzi` `OwnsMany(…).ToJson()` | Tresci |
| `zadanie`, `zadanie_wersja`, `kryterium_oceniania` | `Zadanie`, `ZadanieWersja`, `KryteriumOceniania` | AG + EN | 6.2 | Tresci |
| `zadanie_umiejetnosc`, `zalacznik_zadania` | `ZadanieUmiejetnosc`, `ZalacznikZadania` | EN agregatu `Zadanie` | `OwnsMany` w `ZadanieWersja` (załączniki) i `Zadanie` (Umiejętności) | Tresci |
| `kalibracja_trudnosci` | `KalibracjaTrudnosci` | AG (statystyka) | SQL 10.1.3, odczyt przez zapytanie | Tresci |
| `walidacja_zadania` | `WalidacjaZadania` | AG (niezmienny) | tylko `INSERT` | Tresci |
| `przeglad_zadania` | `PrzegladZadania` | AG (niezmienny) | tylko `INSERT` | Tresci |
| `zgloszenie_bledu_zadania` | `ZgloszenieBleduZadania` | AG | `xmin`; `ux_zgloszenie_zadania_otwarte` | Tresci |
| `wstrzymanie_w_doborze` | `WstrzymanieWDoborze` | AG | SQL C-36 (`INSERT … ON CONFLICT DO NOTHING`) | Tresci |
| `klasa`, `czlonkostwo_klasy` | `Klasa`, `CzlonkostwoKlasy` | AG + EN | `OwnsMany` członkostwa; `KodZaproszenia` → `kod_zaproszenia`, `kod_wazny_do` | Klasy |
| `zestaw_zadan`, `pozycja_zestawu` | `ZestawZadan`, `PozycjaZestawu` | AG + EN | `version`; `OwnsMany` pozycji | Klasy |
| `przypisanie`, `przypisanie_uczen` | `Przypisanie`, `PrzypisanieUczen` | AG + EN | `OwnsMany` adresatów | Klasy |
| `profil_ucznia`, `opanowanie_umiejetnosci` | `ProfilUcznia`, `OpanowanieUmiejetnosci` | AG + EN | 6.5 | Profil |
| `test_diagnostyczny`, `pozycja_testu_diagnostycznego` | `TestDiagnostyczny`, `PozycjaTestuDiagnostycznego` | AG + EN | `pominiete_dzialy` jako `List<DzialId>` (`integer[]`) | Profil |
| `wynik_zadania` | `WynikZadania` | AG | `xmin`; indeksy częściowe z ddl.md | Profil |
| `sciezka_nauki`, `element_sciezki` | `SciezkaNauki`, `ElementSciezki` | AG + EN | 6.5 | Rekomendacje |
| `rekomendacja` | `Rekomendacja` | AG (niezmienny) | `RegulaRekomendacji` konwerter kodów; `cechy`, `decyzja`, `uzasadnienie_fakty` jako `JsonbConverter` | Rekomendacje |
| `rozwiazanie`, `zdjecie_rozwiazania` | `Rozwiazanie`, `ZdjecieRozwiazania` | AG + EN | 6.3 | Rozwiazania |
| `transkrypcja`, `krok_transkrypcji` | `Transkrypcja`, `KrokTranskrypcji` | AG + EN | 6.3 | Rozwiazania |
| `odpowiedz` | `Odpowiedz` | AG | `wybor` jako `JsonbConverter<WyborOdpowiedzi>`; `trg_kontekst_proby` | Rozwiazania |
| `kopia_treningowa` | `KopiaTreningowa` | AG | `ON DELETE RESTRICT` do `uczen` | Rozwiazania |
| `ocena`, `ocena_kryterium` | `Ocena`, `OcenaKryterium` | AG + EN | 6.4 | Ocenianie |
| `weryfikacja` | `Weryfikacja` | AG | 6.4 | Weryfikacje |
| `szkic_rozstrzygniecia` | `SzkicRozstrzygniecia` | AG | klucz złożony; `dane` jako `JsonbConverter<SzkicDane>` | Weryfikacje |
| `konfiguracja_modelu` | `KonfiguracjaModelu` | AG | `xmin`; `parametry` `OwnsOne(…).ToJson()` (`ParametryModelu`); `ux_konfiguracja_aktywna` | Modele |
| `decyzja_modelu` | `DecyzjaModelu` | AG (niezmienny) | 6.6 | Modele |
| `limit_szybkosci_modelu` | — | tabela techniczna `UNLOGGED` | wyłącznie SQL `OgranicznikSzybkosci` (13.2) | — |
| `kalibrator_pewnosci` | `KalibratorPewnosci` | AG (niezmienny) | `punkty` jako `JsonbConverter<PunktyKalibratora>`; `MetodaKalibratora` konwerter | Kalibracja |
| `prog_pewnosci` | `ProgPewnosci` | AG (niezmienny poza `aktywny`) | `ux_prog_aktywny`; zmiana `aktywny` przez `ExecuteUpdate` w transakcji KALIBRUJ | Kalibracja |
| `stan_bezpiecznika` | `StanBezpiecznika` | AG | 6.5 | Kalibracja |
| `probka_kalibracyjna` | `ProbkaKalibracyjna` | AG (niezmienny) | kolumny `zgodna`, `uwzgledniana_w_progu` jako `HasComputedColumnSql(…, stored: true)` | Kalibracja |
| `praca_w_tle` | `PracaWTle` | EN (techniczna) | tylko odczyt (`AsNoTracking`); zapis `SklepPracWTle`, `ZlecaniePracSql` | Orkiestracja / Wspolne.PraceWTle |
| `klucz_idempotencji` | `KluczIdempotencji` | EN (techniczna) | SQL w filtrze idempotencji `Lemat.Api` | — |
| `powiadomienie` | `Powiadomienie` | AG | `Id` = `Guid.CreateVersion7()` w fabryce; `TypPowiadomienia` konwerter | Powiadomienia |
| `dziennik_audytu` | `WpisAudytu` | AG (niezmienny) | tylko `INSERT` | Audyt |

58 tabel, 0 bez przypisania. Test `KazdaTabelaDdlMaWlasciciela` porównuje
listę tabel po migracjach z kolumną „Tabela”.

## 8. Strategia migracji

1. **Źródło prawdy:** `docs/spec/ddl.md` (bloki A i B). Skrypt
   `backend/narzedzia/wyodrebnij-ddl.sh` wycina je do
   `Persistence/Schemat/ddl.sql` (plik w repozytorium; CI sprawdza brak
   różnic po ponownym wycięciu).
2. **Migracja początkowa** `0001_SchematPoczatkowy` jest generowana przez
   `dotnet ef migrations add` z modelu EF Core, a następnie uzupełniana
   wywołaniami `migrationBuilder.Sql(ZasobSql.Wczytaj("0001_funkcje.sql"))`
   … `0005_uprawnienia.sql` (zasoby osadzone w `Persistence/Migrations/Sql/`).
   Obiekty spoza modelu EF: triggery `trg_*`, funkcje, partycje
   `decyzja_modelu`, tabela `UNLOGGED limit_szybkosci_modelu`, CHECK z
   wyrażeniami na tablicach (`cardinality(powody) >= 1`), indeks GIN
   `ix_zadanie_wersja_tresc_trgm`, FK `DEFERRABLE`.
3. **Kolejne migracje** — jedna migracja na zmianę schematu w
   `ddl.md`; zmiana triggera = nowy plik `Sql/NNNN_<opis>.sql` z `CREATE
   OR REPLACE FUNCTION`; migracje są wyłącznie „w przód” (bez `Down` w
   produkcji; `Down` generowany tylko do testów lokalnych).
4. **Zmiany bez przestoju** (dwie fazy): dodanie kolumny `NULL` lub z
   `DEFAULT` → wdrożenie kodu piszącego → migracja z `NOT NULL`/CHECK;
   usunięcie kolumny — najpierw kod, który jej nie czyta, potem migracja.
   Zmiana nazwy kolumny = nowa kolumna + kopiowanie + usunięcie (nigdy
   `RENAME` przy działających replikach).
5. **Wykonanie:** `efbundle` w kontenerze `migracje` (użytkownik
   `lemat_migracje`) przed wdrożeniem obrazów `api`/`worker`/`scheduler`
   (17.5); procesy aplikacji przy starcie sprawdzają
   `__historia_migracji` i kończą się błędem, gdy ostatnia migracja w
   bazie jest starsza niż migracja w obrazie
   (`BazaNieaktualnaException`).
6. **Test `SchematZgodnyZDdlTest`** (`Lemat.Infrastructure.Tests`,
   Testcontainers PostgreSQL 16): baza A ← `ddl.sql`, baza B ← `efbundle`;
   porównanie znormalizowanych wyników `pg_dump --schema-only --no-owner
   --no-privileges` (posortowane obiekty, usunięte komentarze i tabela
   `__historia_migracji`) oraz zapytań katalogowych (`pg_constraint`,
   `pg_index`, `pg_trigger`, `pg_enum`). Różnica kończy test błędem z listą
   obiektów.
