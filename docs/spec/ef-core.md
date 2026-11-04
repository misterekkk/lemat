# Lemat — mapowanie EF Core 10 (Npgsql) na schemat PostgreSQL

| Pole | Wartość |
|---|---|
| Dokument | `docs/spec/ef-core.md` |
| Wersja | 3.5 (C9-02: `szkic_transkrypcji.numer_zapisu` jako `long`/`bigint` z licznikiem serwera; C9-03: `KonwencjaNazw` z wyjątkami dla oznaczeń z cyframi; C9-07 p. 2: `.editorconfig` ze stałymi `PascalCase`; wcześniej 3.4 — ADR-075) |
| Dotyczy | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 3.6 (agregaty), 7 (model danych), 13.4 (prace w tle); schemat: [`docs/spec/ddl.md`](ddl.md) |
| Projekt | `backend/src/Lemat.Infrastructure/Persistence/` |

Plik podaje kontrakt mapowania: `LematDbContext`, konwencje i konwersje,
konfiguracje `IEntityTypeConfiguration<T>` głównych agregatów (pełne
fragmenty istotne dla niezmienników), tabelę pokrycia wszystkich 60 tabel
oraz strategię migracji. DDL z `ddl.md` jest **jedynym** źródłem schematu
(ADR-063): model EF Core jest mapowaniem i nie generuje migracji;
konfiguracja niezgodna z katalogiem bazy (tabela, kolumna, typ, `NULL`,
klucz) jest błędem wykrywanym testem `ModelEfZgodnyZKatalogiem` (sekcja 8).
Od wersji 3.2 konfiguracje nie zawierają `HasCheckConstraint`,
`HasMaxLength`, `HasTrigger` ani `HasIndex` — CHECK-i, długości, triggery i
indeksy (także unikalne częściowe `ux_*`) istnieją wyłącznie w DDL i nie
wpływają na zapytania ani `SaveChanges` w Npgsql (C7-05); wszystkie
fragmenty kodu z sekcji 6 sprawdzono budową modelu i zapisem na PostgreSQL
16.15 z EF Core 10.0.3 (fragment schematu: `uzytkownik`, `uczen`,
`profil_ucznia`, `opanowanie_umiejetnosci`, `stan_bezpiecznika`,
`sciezka_nauki`, `element_sciezki`, `praca_w_tle`, `weryfikacja`).

## 1. Pakiety i rejestracja

| Pakiet | Projekt | Uwagi |
|---|---|---|
| `Microsoft.EntityFrameworkCore` 10 | `Lemat.Infrastructure` | |
| `Npgsql.EntityFrameworkCore.PostgreSQL` 10 | `Lemat.Infrastructure` | `MapEnum`, tablice, `jsonb`, `xmin` |
| `EFCore.NamingConventions` 10 | `Lemat.Infrastructure` | `UseSnakeCaseNamingConvention()` — nazwy kolumn z właściwości PascalCase (ADR-049); nazwy tabel i indeksów są ustawiane jawnie |
| `Microsoft.EntityFrameworkCore.Design` 10 | `Lemat.Infrastructure` (`PrivateAssets=all`) | wyłącznie `efbundle` (migracje SQL, ADR-063); `dotnet ef migrations add` nie jest używane |

```csharp
// Lemat.Infrastructure/Persistence/PersistenceRegistration.cs
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
    uslugi.AddDbContextFactory<LematDbContext>(lifetime: ServiceLifetime.Singleton);   // 3.2 (C7-10): porty wywoływane z agentów
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
}                                                                                        // EwaluacjaZt1 → EWALUACJA_ZT1, IzotonicznyCv5 → IZOTONICZNY_CV5
// KonwencjaNazw (Lemat.Domain/Common/NamingConvention.cs, listing w api.md 1) jest tą samą regułą, którą stosuje NazwyLemat
// w JSON API (3.5, C9-03): separator `_` przed liczbą (OswiadczenieWieku16 → oswiadczenie_wieku_16) poza tokenami
// KonwencjaNazw.TokenyZCyframi (zt1, cv5, sha256, fnv1a, argon2id, uuid5, uuid7, es256, s3). Kolumny: UseSnakeCaseNamingConvention
// daje oswiadczenie_wieku16, więc właściwość, dla której KonwencjaNazw.NaSnakeCase ≠ wynik konwencji EF, ma HasColumnName
// (test KolumnyZCyframiMajaHasColumnName porównuje obie funkcje dla każdej właściwości modelu; argumenty_sha256 — bez HasColumnName).

public static class MapowanieTypowWyliczeniowych
{
    private static readonly INpgsqlNameTranslator T = new TlumaczNazwUpperSnake();
    // 43 typy z ddl.md; ta sama lista dla NpgsqlDataSourceBuilder i dla modelu EF
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
migracjach (43 = 43) i wartości `enum_range(NULL::<typ>)` z członkami
enumu (kolejność i pisownia).

**Definicje enumów bez listingu w 3.6/10/13 (3.2, C7-15 p. 2).** Wartości
pobrane z katalogu po załadowaniu DDL (`enum_range`); pozostałe 31 enumów
są w 3.6, 10 i 13 dokumentu głównego.

```csharp
namespace Lemat.Domain.Common;   // przestrzenie według modułu właściciela (3.5); tu zebrane dla czytelności
public enum StatusWkladki { Szkic, DoPrzegladu, Opublikowana, Wycofana }
public enum RolaModelu { DecyzyjnyGlowny, DecyzyjnyZapasowy, RozpoznawaniePisma, GeneratywnyZadania,
                         GeneratywnyRozwiazujacyNiezaleznie, GeneratywnyWskazowki, GeneratywnyWkladki, GeneratywnyOpiniaPomocnicza }
public enum ZrodloProbki { WeryfikacjaNiskaPewnosc, KontrolaLosowa, WeryfikacjaInnyPowod, KorektaNauczyciela }
public enum ZrodloKalibratora { EwaluacjaZt1, KalibracjaAutomatyczna }
public enum StatusZlecenia { Oczekuje, WToku, Zakonczone, Nieudane }
public enum StatusImportu { Oczekuje, Przetwarzany, Zakonczony, Odrzucony }
public enum StatusTestu { WToku, Zakonczony, Pominiety }
public enum DecyzjaPrzegladu { Zatwierdzone, Poprawione, Odrzucone }
public enum RezultatWalidacji { Pozytywny, Negatywny }
public enum StatusZgloszeniaZadania { Otwarte, Uwzglednione, Odrzucone }
public enum TypZdarzeniaRodo { WniosekOUsuniecie, AnulowanieUsuniecia, UsuniecieWykonane, OdwolanieZgody, UdzielenieZgody }
public enum ZrodloZdarzeniaRodo { Api, Worker }
```

Translator daje dla każdego członka wartość z DDL (`EwaluacjaZt1` →
`EWALUACJA_ZT1`, `WniosekOUsuniecie` → `WNIOSEK_O_USUNIECIE`); test
`KazdyTypWyliczeniowyDdlJestZmapowany` porównuje wszystkie 43.

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

`UnsafeRelaxedJsonEscaping` (wersja 3.2, C7-15 p. 8): domyślny koder
System.Text.Json zapisuje `<`, `>`, `&` jako sekwencje `\u003C`, `\u003E`,
`\u0026`. Dla kolumn `jsonb` nie ma to wpływu na wartość, bo PostgreSQL
dekoduje sekwencje `\uXXXX` przy zapisie; koder ma znaczenie dla (a) tekstu
wiadomości `user` wysyłanej do modelu (`BudowniczyWiadomosci`), (b) kolumn
`text` z JSON-em (`decyzja_modelu.stan`) i (c) skrótów SHA-256 liczonych z
serializacji — tam równość bajtowa z `krok_transkrypcji.latex` jest
wymagana (ADR-058 p. 4, test `ZgodnoscBajtowaLatexTests`).

## 4. Współbieżność

| Rodzaj | Tabele | Konfiguracja | Zmiana wartości |
|---|---|---|---|
| `version` zmieniane przez aplikację | `uzytkownik`, `uczen`, `nauczyciel`, `wkladka_teoretyczna`, `zadanie`, `klasa`, `zestaw_zadan`, `przypisanie` (3.3, ADR-074), `profil_ucznia`, `sciezka_nauki`, `rozwiazanie`, `stan_bezpiecznika` | `Property(x => x.Version).IsConcurrencyToken()`; agregat nowy ma `Version = 1` z fabryki | `WersjaAgregatuInterceptor` (`SavingChangesAsync`, 3.2 — C7-06 p. 8): dla każdego wpisu `Added`/`Modified`/`Deleted` wyszukuje korzeń agregatu (po kluczu własności `IsOwnership` albo FK encji `IEncjaWewnetrzna`, rekurencyjnie, dla `Deleted` po wartościach oryginalnych) i zwiększa `Version` korzenia `Unchanged`/`Modified` o 1, o ile zmiana nie dotyczy wyłącznie właściwości `[BezZmianyWersji]` (ADR-029); sprawdzone: zmiana wyłącznie `opanowanie_umiejetnosci.theta` daje `UPDATE profil_ucznia SET version = … WHERE version = @oryginalna` |
| `version` zmieniane przez trigger | `weryfikacja` (`trg_weryfikacja_wersja`, ADR-037) | `Property(w => w.Version).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate()` | EF Core nie wysyła `version` w `SET`, warunek `WHERE version = @oryginalna` i odczyt `RETURNING version`; interceptor pomija `Weryfikacja` |
| `xmin` (systemowy licznik wiersza PostgreSQL) | `transkrypcja`, `wynik_zadania`, `zlecenie_generowania`, `import_arkusza`, `konfiguracja_modelu`, `ocena` | `Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()` | PostgreSQL przy każdej zmianie wiersza; kolumny nie ma w DDL (kolumna systemowa) |
| brak (zapis wyłącznie SQL) | `praca_w_tle`, `kalibracja_trudnosci`, `limit_szybkosci_modelu`, `wstrzymanie_w_doborze`, `klucz_idempotencji` (funkcja `zarezerwuj_klucz_idempotencji` i `UPDATE … WHERE rezerwacja = @moja`, ADR-068), `szkic_transkrypcji` (`INSERT … ON CONFLICT … DO UPDATE SET numer_zapisu = numer_zapisu + 1 … WHERE numer_zapisu = @poprzedni_numer_zapisu`, ADR-073 uzup. 3.5) | encja tylko do odczytu albo brak encji | warunki `UPDATE … WHERE` (13.2, 13.4, 10.1.3, C-36) |

`DbUpdateConcurrencyException` jest tłumaczony przez `Lemat.Api` na `412
KONFLIKT_WERSJI` (żądanie z `If-Match`), `409 KONFLIKT_WSPOLBIEZNOSCI` (bez
`If-Match`) albo przez handler pracy w tle na `rezultat_przestarzaly`
(15.2 dokumentu głównego). Wartość `If-Match` to `"<version>"` z pola
`version` ciała odpowiedzi (od 3.3 API nie wysyła nagłówka `ETag` w
odczytach, ADR-069);
`If-Match` ustawia wartość oryginalną: `entry.Property(x => x.Version).OriginalValue
= wersjaZNaglowka` w repozytorium przed modyfikacją.

## 5. Interceptory

| Interceptor | Zdarzenie | Działanie |
|---|---|---|
| `ZnacznikiCzasuInterceptor` | `SavingChangesAsync` | `updated_at = TimeProvider.GetUtcNow()` dla encji `Modified` z właściwością `UpdatedAt`; `created_at` ma `DEFAULT now()` w bazie i `ValueGeneratedOnAdd` |
| `WersjaAgregatuInterceptor` | `SavingChangesAsync` | sekcja 4 |
| — | — | **brak** interceptora `NOTIFY` i zlecania prac (ADR-056): `NOTIFY` wysyłają triggery, prace zleca `ZlecaniePracSql` |

## 6. Konfiguracje agregatów (kluczowe fragmenty)

Wszystkie klasy są w `Lemat.Infrastructure/Persistence/Configurations/<Module>/`
i nazywają się `<Entity>EntityTypeConfiguration` (3.1 p. 7). Wspólne:
`builder.Ignore(a => a.Zdarzenia)`; kolekcje encji wewnętrznych przez pola
prywatne (`UsePropertyAccessMode(PropertyAccessMode.Field)`).

### 6.1 Konta

```csharp
public sealed class UzytkownikEntityTypeConfiguration : IEntityTypeConfiguration<Uzytkownik>
{
    public void Configure(EntityTypeBuilder<Uzytkownik> b)
    {
        b.ToTable("uzytkownik");
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).HasColumnType("citext").IsRequired();
        b.Property<string>("HasloHash").HasColumnName("haslo_hash").IsRequired();     // pole prywatne, poza API domeny
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
        b.ToTable("nauczyciel");
        b.HasKey(n => n.Id);
        b.Property(n => n.Id).HasColumnName("uzytkownik_id");
        // 3.2 (C7-06 p. 1, ADR-063 p. 3): brak relacji EF do Uzytkownik (NauczycielId ≠ UzytkownikId — model by się nie zbudował);
        // FK nauczyciel.uzytkownik_id → uzytkownik(id) jest w DDL DEFERRABLE INITIALLY DEFERRED, więc kolejność INSERT nie ma znaczenia.
        b.Property(n => n.DeklarowaneMinutyWeryfikacji).HasColumnType("smallint");
        b.Property(n => n.Version).IsConcurrencyToken();
    }
}

public sealed class UczenEntityTypeConfiguration : IEntityTypeConfiguration<Uczen>
{
    public void Configure(EntityTypeBuilder<Uczen> b)
    {
        b.ToTable("uczen");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).HasColumnName("uzytkownik_id");                          // FK odroczony w DDL, bez relacji EF (jak Nauczyciel)
        b.Property(u => u.OswiadczenieWieku16).HasColumnName("oswiadczenie_wieku_16"); // C7-06 p. 7, ADR-049 p. 14
        b.Property(u => u.GrupaEksperymentu).HasColumnName("procent_grupy_eksperymentu").HasColumnType("smallint");
        b.Property(u => u.Version).IsConcurrencyToken();
    }
}

public sealed class TokenOdswiezaniaEntityTypeConfiguration : IEntityTypeConfiguration<TokenOdswiezania>
{
    public void Configure(EntityTypeBuilder<TokenOdswiezania> b)
    {
        b.ToTable("token_odswiezania");
        b.HasKey(t => t.Id);
        b.HasOne<TokenOdswiezania>().WithMany().HasForeignKey(t => t.ZastapionyPrzez).OnDelete(DeleteBehavior.SetNull);
        b.Property(t => t.UzyciaWOknieTolerancji).HasColumnType("smallint");
    }
}
```

Rotacja z oknem tolerancji (ADR-057) nie używa śledzenia zmian: jedno
polecenie `UPDATE … WHERE id = @t AND zastapiony_at IS NULL AND
uniewazniony_at IS NULL RETURNING …` przez `Database.SqlQuery<T>(…).ToListAsync()`
(3.2: `ExecuteUpdateAsync` nie zwraca wierszy) i drugie dla gałęzi okna
tolerancji (`uzycia_w_oknie_tolerancji < 5`, `ExecuteUpdateAsync` z liczbą
wierszy), żeby dwa równoległe odświeżenia nie utworzyły dwóch następców.

### 6.2 Treści

```csharp
public sealed class ZadanieEntityTypeConfiguration : IEntityTypeConfiguration<Zadanie>
{
    public void Configure(EntityTypeBuilder<Zadanie> b)
    {
        b.ToTable("zadanie");
        b.HasKey(z => z.Id);
        b.HasMany(z => z.Wersje).WithOne().HasForeignKey("zadanie_id").IsRequired();
        b.Navigation(z => z.Wersje).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        b.Property<ZadanieWersjaId?>("aktualna_wersja_id");                          // FK DEFERRABLE (ddl.md); ustawia repozytorium
        b.Ignore(z => z.AktualnaWersja).Ignore(z => z.CzyBazowe).Ignore(z => z.CzyWygenerowane);
        b.Property(z => z.Version).IsConcurrencyToken();
    }
}

public sealed class ZadanieWersjaEntityTypeConfiguration : IEntityTypeConfiguration<ZadanieWersja>
{
    public void Configure(EntityTypeBuilder<ZadanieWersja> b)
    {
        b.ToTable("zadanie_wersja");
        b.HasKey(w => w.Id);
        b.OwnsOne(w => w.OpcjeZamkniete, j => j.ToJson("opcje_zamkniete"));
        b.OwnsOne(w => w.RozwiazanieWzorcowe, j => { j.ToJson("rozwiazanie_wzorcowe"); j.OwnsMany(r => r.Warianty, v => v.OwnsMany(x => x.Kroki)); });
        b.OwnsOne(w => w.WynikKoncowyWzorcowy, j => j.ToJson("wynik_koncowy_wzorcowy"));
        b.OwnsMany(w => w.KryteriaOceniania, k =>                                    // tabela kryterium_oceniania
        {
            k.ToTable("kryterium_oceniania");
            k.WithOwner().HasForeignKey("zadanie_wersja_id");
            k.Property<Guid>("id").HasDefaultValueSql("gen_random_uuid()");
            k.HasKey("id");
        });
        b.Navigation(w => w.KryteriaOceniania).UsePropertyAccessMode(PropertyAccessMode.Field);
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
        b.ToTable("rozwiazanie");
        b.HasKey(r => r.Id);
        b.Property(r => r.KodBleduRozpoznawania).HasConversion<UpperSnakeEnumConverter<KodBleduRozpoznawania>>();
        b.Property(r => r.LiczbaPrzeslan).HasColumnType("smallint");
        b.Property(r => r.LiczbaPonowienRozpoznania).HasColumnType("smallint");
        b.Property(r => r.CzasSystemowy).HasColumnName("czas_systemowy_ms")
            .HasConversion(v => v.HasValue ? (int?)v.Value.TotalMilliseconds : null, v => v.HasValue ? TimeSpan.FromMilliseconds(v.Value) : null);
        b.HasMany(r => r.Zdjecia).WithOne().HasForeignKey("rozwiazanie_id").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(r => r.Zdjecia).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        b.Property(r => r.Version).IsConcurrencyToken();
    }
}

public sealed class TranskrypcjaEntityTypeConfiguration : IEntityTypeConfiguration<Transkrypcja>
{
    public void Configure(EntityTypeBuilder<Transkrypcja> b)
    {
        b.ToTable("transkrypcja");
        b.HasKey(t => t.Id);
        b.OwnsOne(t => t.ZakresPoprawek, j => j.ToJson("zakres_poprawek"));
        b.OwnsOne(t => t.ZgodnoscPoprawek, j => { j.ToJson("zgodnosc_poprawek"); j.OwnsMany(z => z.Kroki); });
        b.OwnsMany(t => t.Kroki, k =>                                                // tabela krok_transkrypcji
        {
            k.ToTable("krok_transkrypcji");
            k.WithOwner().HasForeignKey("transkrypcja_id");
            k.Property<Guid>("id").HasDefaultValueSql("gen_random_uuid()");
            k.HasKey("id");
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
        b.ToTable("ocena");
        b.HasKey(o => o.Id);
        b.OwnsOne(o => o.Wskazowka, w =>                                             // VO w dwóch kolumnach
        {
            w.Property(x => x.Tresc).HasColumnName("wskazowka");
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
        b.ToTable("weryfikacja");
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
        b.Property(w => w.Version).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();   // trigger, ADR-037
    }
}
```

Podjęcie, przedłużenie i zwolnienie Weryfikacji są jednym poleceniem
`UPDATE … RETURNING` z warunkami 9.6.1/11.4.2 (C-07) wykonywanym przez
`Database.SqlQuery<WierszBlokadyWeryfikacji>($"UPDATE weryfikacja SET … WHERE
id = {id} AND (…) RETURNING id, status, podjeta_przez, podjeta_at,
zablokowana_do, version").ToListAsync()` (wersja 3.2, C7-06 p. 5–6:
`ExecuteUpdateAsync` zwraca wyłącznie liczbę wierszy, a SQL z `RETURNING` jest
niekomponowalny, więc `FirstOrDefaultAsync` rzuca `InvalidOperationException`
— sprawdzone na Npgsql EF 10.0.3). Pusta lista → `409 WERYFIKACJA_ZAJETA`
(podjęcie) albo `409 WERYFIKACJA_NIE_PODJETA` (przedłużenie, zwolnienie).
Metoda agregatu `Weryfikacja.Podejmij(...)` jest wykonywana w pamięci na
stanie sprzed polecenia i jej wynik porównywany z wierszem `RETURNING`
(różnica → `InvalidOperationException`, test `PodjecieSqlZgodneZAgregatem`
z 2 000 losowymi stanami FsCheck). Encja `Weryfikacja` nie jest wczytywana
z `AsTracking()` przed poleceniem w tej samej jednostce pracy; gdy przypadek
użycia potrzebuje jej potem (rozstrzygnięcie po podjęciu w jednym żądaniu —
nie występuje w API), repozytorium czyta ją po poleceniu albo wywołuje
`ReloadAsync()` (15.2 dokumentu głównego). Eskalacja (`ESKALUJ_WERYFIKACJE`)
nie potrzebuje wiersza wynikowego i używa `ExecuteUpdateAsync`.

### 6.5 Profil, Rekomendacje, Kalibracja

```csharp
public sealed class ProfilUczniaEntityTypeConfiguration : IEntityTypeConfiguration<ProfilUcznia>
{
    public void Configure(EntityTypeBuilder<ProfilUcznia> b)
    {
        b.ToTable("profil_ucznia");
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasColumnName("uczen_id");
        // 3.2 (C7-06 p. 2): pole agregatu to List<OpanowanieUmiejetnosci> _opanowanie; słownik Opanowanie
        // (IReadOnlyDictionary<UmiejetnoscId, OpanowanieUmiejetnosci>) jest budowany w pamięci przy pierwszym dostępie.
        b.OwnsMany<OpanowanieUmiejetnosci>("_opanowanie", o =>
        {
            o.ToTable("opanowanie_umiejetnosci");
            o.WithOwner().HasForeignKey("uczen_id");
            o.HasKey("uczen_id", nameof(OpanowanieUmiejetnosci.UmiejetnoscId));
            o.Property(x => x.Theta).HasPrecision(6, 3);
        });
        b.Navigation("_opanowanie").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Ignore(p => p.Opanowanie);
        b.HasOne<Uczen>().WithOne().HasForeignKey<ProfilUcznia>(p => p.Id);              // relacja bez nawigacji: kolejność INSERT (ADR-063 p. 3)
        b.Property(p => p.Version).IsConcurrencyToken();
    }
}

public sealed class SciezkaNaukiEntityTypeConfiguration : IEntityTypeConfiguration<SciezkaNauki>
{
    public void Configure(EntityTypeBuilder<SciezkaNauki> b)
    {
        b.ToTable("sciezka_nauki");
        b.HasKey(s => s.Id);
        b.OwnsOne(s => s.Seria, s =>
        {
            s.Property(x => x.UmiejetnoscId).HasColumnName("seria_umiejetnosc_id");
            s.Property(x => x.RozpoczetaAt).HasColumnName("seria_rozpoczeta_at");
            s.Property(x => x.LiczbaElementow).HasColumnName("seria_liczba_elementow");
            s.Property(x => x.PoziomTrudnosci).HasColumnName("seria_poziom_trudnosci");
            s.Property(x => x.KolejnePorazki).HasColumnName("seria_kolejne_porazki");
        });
        b.HasMany<ElementSciezki>("_aktywne").WithOne().HasForeignKey("sciezka_nauki_id").IsRequired();   // NOT NULL w DDL (wykryte testem 8)
        b.Ignore(s => s.AktywneElementy);
        b.Property(s => s.Version).IsConcurrencyToken();
    }
}
```

Repozytorium `SciezkaNaukiRepozytorium.WczytajAsync` wczytuje agregat z
filtrowanym `Include` wyłącznie elementów `PROPONOWANY` i `ROZPOCZETY`
(`.Include(s => EF.Property<List<ElementSciezki>>(s, "_aktywne").Where(e => e.Status == StatusElementuSciezki.Proponowany || e.Status == StatusElementuSciezki.Rozpoczety))`
— przeciążenie `Include(string, predykat)` nie istnieje, C7-06 p. 4; sprawdzone),
czyli ≤ 2 wiersze; 10 ostatnich elementów dla Agenta rekomendującego czyta
osobne zapytanie (C6-15 p. 4).

```csharp
public sealed class StanBezpiecznikaEntityTypeConfiguration : IEntityTypeConfiguration<StanBezpiecznika>
{
    public void Configure(EntityTypeBuilder<StanBezpiecznika> b)
    {
        b.ToTable("stan_bezpiecznika");
        // 3.2 (C7-06 p. 3): cienie o nazwach PascalCase = nazwy parametrów konstruktora; kolumny przez HasColumnName
        b.Property<KonfiguracjaModeluId>("KonfiguracjaModeluId").HasColumnName("konfiguracja_modelu_id");
        b.Property<RodzajZadania>("RodzajZadania").HasColumnName("rodzaj_zadania");
        b.Property<TypBezpiecznika>("Typ").HasColumnName("typ");
        b.HasKey("KonfiguracjaModeluId", "RodzajZadania", "Typ");
        b.Ignore(s => s.Id);                                                         // składany z trzech kolumn w konstruktorze materializacji
        b.Ignore(s => s.BlokadaObnizaniaProgu).Ignore(s => s.KazdaPoprawkaDoWeryfikacji);
        b.Property(s => s.Version).IsConcurrencyToken();                             // C6-08
    }
}
```

Klucz złożony `KluczBezpiecznika` jest mapowany przez trzy właściwości
cieni o nazwach PascalCase, a właściwość `Id` agregatu składa **jedyny**
konstruktor `StanBezpiecznika(KonfiguracjaModeluId konfiguracjaModeluId,
RodzajZadania rodzajZadania, TypBezpiecznika typ)` — EF Core wiąże parametry
z właściwościami po nazwie bez rozróżniania wielkości liter, więc cienie
`snake_case` z wersji 3.1 nie wiązały się (C7-06 p. 3). Agregat nie ma
konstruktora bezparametrowego (3.6 tworzy go fabryką z tym konstruktorem).
Sprawdzone: odczyt wiersza daje `Id = (konfiguracja, OtwarteObliczeniowe,
Poprawki)`.

### 6.6 Modele i decyzje

```csharp
public sealed class DecyzjaModeluEntityTypeConfiguration : IEntityTypeConfiguration<DecyzjaModelu>
{
    public void Configure(EntityTypeBuilder<DecyzjaModelu> b)
    {
        b.ToTable("decyzja_modelu");                                                 // tabela partycjonowana (ddl.md, 0004_partitions.sql)
        b.HasKey(d => new { d.Id, d.CreatedAt });
        b.Property(d => d.SkrotStanu).HasColumnType("bytea");
        b.Property(d => d.Opcje).HasConversion<JsonbConverter<OpcjePytania?>>().HasColumnType("jsonb");
        b.Property(d => d.Rozklad).HasConversion<JsonbConverter<IReadOnlyDictionary<string, decimal>?>>().HasColumnType("jsonb");
    }
}
```

Wiersze `decyzja_modelu` są wstawiane wsadowo (`AddRange` + `SaveChanges`
w jednostce pracy zapisu Oceny, ≤ 29 wierszy); EF Core 10 wysyła je jednym
poleceniem wielowierszowym. Tabela nadrzędna i partycje są tworzone w `0004_partitions.sql` (migracje są
wyłącznie SQL, ADR-063).

## 7. Pokrycie tabel

| Tabela | Typ C# | Rodzaj | Konfiguracja / dostęp | Moduł (6.2) |
|---|---|---|---|---|
| `uzytkownik` | `Uzytkownik` | AG | 6.1 | Konta |
| `uczen` | `Uczen` | AG | 6.1 `UczenEntityTypeConfiguration` (PK `uzytkownik_id`, `HasColumnName("oswiadczenie_wieku_16")`, `GrupaEksperymentu` → `procent_grupy_eksperymentu`) | Konta |
| `nauczyciel` | `Nauczyciel` | AG | 6.1 | Konta |
| `token_odswiezania` | `TokenOdswiezania` | EN (techniczna) | 6.1 | Konta |
| `proba_logowania` | `ProbaLogowania` | EN (techniczna) | tylko `INSERT` i zapytanie liczące | Konta |
| `zdarzenie_rodo` | `ZdarzenieRodo` | AG (niezmienny) | `ZdarzenieRodoEntityTypeConfiguration` (PK bez wartości domyślnej, `dane` jako `JsonDocument`) | Konta |
| `dzien_wolny` | `DzienWolny` | EN | dane referencyjne, tylko odczyt | Wspolne |
| `dzial`, `umiejetnosc`, `umiejetnosc_wymagana` | `Dzial`, `Umiejetnosc` (kolekcja `WymaganeUmiejetnosci`) | AG (referencyjne) | tylko odczyt, `HasData` z 7.4 i `skills.yaml` | Tresci |
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
| `przypisanie`, `przypisanie_uczen` | `Przypisanie`, `PrzypisanieUczen` | AG + EN | `OwnsMany` adresatów; `version` (3.3), `wycofano_at`/`powod_wycofania` (ADR-074) | Klasy |
| `szkic_transkrypcji` | `SzkicTranskrypcji` | EN (techniczna, 3.3) | `kroki` jako `JsonbConverter<IReadOnlyList<KrokSzkicu>>`; zapis SQL, odczyt w `IOdczytRozwiazan` (ADR-073) | Rozwiazania |
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
| `praca_w_tle` | `PracaWTle` | EN (techniczna) | tylko odczyt (`AsNoTracking`, rekord 13.4.1 z kompletem kolumn, `MaksProb` → `maks_prob`); zapis `SklepPracWTle` (`ponow_prace`, `odzyskaj_dzierzawy`), `ZlecaniePracSql` | Orkiestracja / Common.BackgroundJobs |
| `przebieg_harmonogramu` | `PrzebiegHarmonogramu` | EN (techniczna) | SQL `INSERT … ON CONFLICT DO UPDATE` w `HarmonogramHostedService`; odczyt do panelu (3.2, ADR-066 p. 5) | Common.BackgroundJobs |
| `klucz_idempotencji` | `KluczIdempotencji` | EN (techniczna) | SQL w filtrze idempotencji `Lemat.Api` | — |
| `powiadomienie` | `Powiadomienie` | AG | `Id` = `Guid.CreateVersion7()` w fabryce; `TypPowiadomienia` konwerter | Powiadomienia |
| `dziennik_audytu` | `WpisAudytu` | AG (niezmienny) | tylko `INSERT` | Audyt |

60 tabel, 0 bez przypisania (wersja 3.1 podawała 58 przy 59 wierszach
tabeli; od 3.2 doszła `przebieg_harmonogramu`). Test
`KazdaTabelaDdlMaWlasciciela` porównuje listę tabel po migracjach z kolumną
„Tabela”.

## 8. Strategia migracji i test zgodności (wersja 3.2, ADR-063)

1. **Źródło:** `docs/spec/ddl.md` (bloki A i B) → `extract-ddl.sh` →
   `Persistence/Migrations/Sql/0000_schema.sql`, `0001_functions.sql` …
   `0006_background_jobs.sql` (zasoby osadzone).
2. **Migracje:** klasy `Migration` wyłącznie z
   `migrationBuilder.Sql(ZasobSql.Wczytaj("…"))`; `0001_SchematPoczatkowy`
   wykonuje pliki `0000` … `0006`. Brak `ModelSnapshot`; model EF Core nie
   ma `ExcludeFromMigrations()` ani konfiguracji, które służyłyby wyłącznie
   generowaniu schematu.
3. **`ModelEfZgodnyZKatalogiem`** (`Lemat.Infrastructure.Tests`,
   Testcontainers PostgreSQL 16, każdy PR) — po migracjach dla każdej
   tabeli `IModel.GetRelationalModel().Tables` (sprawdzony szkic
   porównania: `information_schema.columns` z normalizacją typów
   `USER-DEFINED` → `schemat.typ`, `numeric` → `numeric(p,s)`):
   - (a) tabela i każda kolumna modelu istnieją;
   - (b) `StoreType` = typ kolumny (`character varying` w modelu jest błędem);
   - (c) `IsNullable` = `is_nullable` (sprawdzony szkic wykrył
     `element_sciezki.sciezka_nauki_id` — w modelu nullowalny, w DDL `NOT NULL`);
   - (d) klucz główny modelu = `pg_index.indisprimary`;
   - (e) kolumny `NOT NULL` bez `DEFAULT` w tabelach zapisywanych przez EF
     są w modelu;
   - (f) cele `ON CONFLICT` z `KatalogPrac` i C-36 istnieją jako indeksy
     unikalne z tym predykatem.
4. **`KazdyKluczObcyMaRelacjeAlboJestOdroczony`** — każdy wiersz
   `pg_constraint (contype = 'f')` ma `IForeignKey` w modelu na tych samych
   kolumnach albo `condeferred = true` (dziś: `uczen_uzytkownik_id_fkey`,
   `nauczyciel_uzytkownik_id_fkey`, `fk_zadanie_aktualna_wersja`,
   `fk_rozwiazanie_najnowsza_transkrypcja`); tabele tylko SQL (`praca_w_tle`,
   `limit_szybkosci_modelu`, `klucz_idempotencji`, `przebieg_harmonogramu`)
   są wyłączone.
5. **`KazdyTriggerWKatalogu`** — `KatalogTriggerow` (15 pozycji: tabela,
   nazwa, plik SQL) = `pg_trigger WHERE NOT tgisinternal`:
   `import_arkusza.trg_import_arkusza_powiadom`,
   `konfiguracja_modelu.trg_konfiguracja_modelu_powiadom`,
   `rozwiazanie.trg_kontekst_proby`, `odpowiedz.trg_kontekst_proby`,
   `ocena.trg_ocena_niezmienna`, `ocena.trg_ocena_powiadom`,
   `powiadomienie.trg_powiadomienie_powiadom`,
   `weryfikacja.trg_powody_poczatkowe`, `praca_w_tle.trg_praca_w_tle_powiadom`,
   `rozwiazanie.trg_rozwiazanie_powiadom`,
   `kryterium_oceniania.trg_suma_kryteriow`,
   `zadanie_umiejetnosc.trg_umiejetnosc_glowna_w_dziale`,
   `weryfikacja.trg_weryfikacja_powiadom`, `weryfikacja.trg_weryfikacja_wersja`,
   `zlecenie_generowania.trg_zlecenie_generowania_powiadom` (C7-15 p. 3:
   `trg_suma_kryteriow` jest na `kryterium_oceniania`, nie na
   `zadanie_wersja`).
6. **`OgraniczeniaZgodneZMigawka`** — `Persistence/Schema/constraints.txt`
   (posortowane `tabela | nazwa | pg_get_constraintdef`) = katalog; zmiana
   DDL wymaga regeneracji pliku w tym samym PR.
7. **Wykonanie i role:** `efbundle` w kontenerze `migracje`; `0005`
   tworzy role `NOLOGIN`, gdy nie istnieją (C7-15 p. 4) — test
   `MigracjeNaPustejBazieBezRolIBezMock`.
