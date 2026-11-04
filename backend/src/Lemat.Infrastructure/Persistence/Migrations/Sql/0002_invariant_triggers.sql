-- ===================== 0002_invariant_triggers.sql =====================
-- Suma kryteriów per wariant = punkty_max wersji, ≤ 1 kryterium wyniku końcowego na wariant, ≤ 24 kryteria (W-1, 13.2)
CREATE FUNCTION f_suma_kryteriow() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    wersja uuid := COALESCE(NEW.zadanie_wersja_id, OLD.zadanie_wersja_id);
    pmax   smallint;
BEGIN
    SELECT punkty_max INTO pmax FROM zadanie_wersja WHERE id = wersja;
    IF EXISTS (SELECT 1 FROM kryterium_oceniania WHERE zadanie_wersja_id = wersja
               GROUP BY wariant
               HAVING sum(punkty_max) <> pmax OR count(*) FILTER (WHERE czy_wynik_koncowy) > 1) THEN
        RAISE EXCEPTION 'KRYTERIA_NIESPOJNE';
    END IF;
    IF (SELECT count(*) FROM kryterium_oceniania WHERE zadanie_wersja_id = wersja) > 24 THEN
        RAISE EXCEPTION 'ZA_DUZO_KRYTERIOW';
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER trg_suma_kryteriow AFTER INSERT OR UPDATE OR DELETE ON kryterium_oceniania
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION f_suma_kryteriow();

-- Umiejętność główna Zadania należy do jego Działu
CREATE FUNCTION f_umiejetnosc_glowna_w_dziale() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.glowna AND NOT EXISTS (
        SELECT 1 FROM zadanie z JOIN umiejetnosc u ON u.id = NEW.umiejetnosc_id
        WHERE z.id = NEW.zadanie_id AND u.dzial_id = z.dzial_id) THEN
        RAISE EXCEPTION 'UMIEJETNOSC_GLOWNA_POZA_DZIALEM';
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER trg_umiejetnosc_glowna_w_dziale AFTER INSERT OR UPDATE ON zadanie_umiejetnosc
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION f_umiejetnosc_glowna_w_dziale();

-- Kontekst próby (ADR-032, C1-21): dziedziczenie przypisanie_id z elementu R-PRZYPISANIE
CREATE FUNCTION f_kontekst_proby() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    przypisanie_elementu uuid;
BEGIN
    IF NEW.element_sciezki_id IS NOT NULL THEN
        SELECT przypisanie_id INTO przypisanie_elementu FROM element_sciezki WHERE id = NEW.element_sciezki_id;
        IF przypisanie_elementu IS NOT NULL AND NEW.przypisanie_id IS NULL THEN
            NEW.przypisanie_id := przypisanie_elementu;
        ELSIF NEW.przypisanie_id IS NOT NULL AND NEW.przypisanie_id IS DISTINCT FROM przypisanie_elementu THEN
            RAISE EXCEPTION 'KONTEKST_PROBY_NIESPOJNY';
        END IF;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_kontekst_proby BEFORE INSERT ON rozwiazanie FOR EACH ROW EXECUTE FUNCTION f_kontekst_proby();
CREATE TRIGGER trg_kontekst_proby BEFORE INSERT ON odpowiedz   FOR EACH ROW EXECUTE FUNCTION f_kontekst_proby();

-- Ocena niezmienna poza WSTEPNA|OSTATECZNA → ZASTAPIONA i jednokrotnym zapisem Wskazówki (Z-5)
CREATE FUNCTION f_ocena_niezmienna() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF (to_jsonb(NEW) - 'status' - 'wskazowka' - 'zrodlo_wskazowki')
       IS DISTINCT FROM (to_jsonb(OLD) - 'status' - 'wskazowka' - 'zrodlo_wskazowki') THEN
        RAISE EXCEPTION 'OCENA_NIEZMIENNA';
    END IF;
    IF NEW.status IS DISTINCT FROM OLD.status
       AND NOT (OLD.status IN ('WSTEPNA', 'OSTATECZNA') AND NEW.status = 'ZASTAPIONA') THEN
        RAISE EXCEPTION 'OCENA_NIEZMIENNA';
    END IF;
    IF (NEW.wskazowka IS DISTINCT FROM OLD.wskazowka OR NEW.zrodlo_wskazowki IS DISTINCT FROM OLD.zrodlo_wskazowki)
       AND OLD.wskazowka IS NOT NULL THEN
        RAISE EXCEPTION 'OCENA_NIEZMIENNA';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_ocena_niezmienna BEFORE UPDATE ON ocena FOR EACH ROW EXECUTE FUNCTION f_ocena_niezmienna();

-- weryfikacja.version = wersja TREŚCI (ADR-037)
CREATE FUNCTION f_weryfikacja_wersja() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.powody IS DISTINCT FROM OLD.powody
       OR NEW.zgloszenie IS DISTINCT FROM OLD.zgloszenie
       OR NEW.ocena_weryfikowana_id IS DISTINCT FROM OLD.ocena_weryfikowana_id
       OR NEW.ponowna_ocena_at IS DISTINCT FROM OLD.ponowna_ocena_at
       OR NEW.ocena_nauczyciela_id IS DISTINCT FROM OLD.ocena_nauczyciela_id
       OR NEW.powod_anulowania IS DISTINCT FROM OLD.powod_anulowania
       OR (NEW.status IN ('ZAKONCZONA', 'ANULOWANA') AND NEW.status IS DISTINCT FROM OLD.status) THEN
        NEW.version := OLD.version + 1;
    ELSE
        NEW.version := OLD.version;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_weryfikacja_wersja BEFORE UPDATE ON weryfikacja FOR EACH ROW EXECUTE FUNCTION f_weryfikacja_wersja();

CREATE FUNCTION f_powody_poczatkowe() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.powody_poczatkowe IS DISTINCT FROM OLD.powody_poczatkowe
       AND NOT (OLD.ocena_weryfikowana_id IS NULL AND NEW.ocena_weryfikowana_id IS NOT NULL) THEN
        RAISE EXCEPTION 'POWODY_NIEZMIENNE';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_powody_poczatkowe BEFORE UPDATE ON weryfikacja FOR EACH ROW EXECUTE FUNCTION f_powody_poczatkowe();

