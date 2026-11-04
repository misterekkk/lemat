-- ===================== 0003_notifications.sql (ADR-056 p. 3) =====================
-- pg_notify w transakcji jest dostarczane wyłącznie po COMMIT, nigdy po ROLLBACK;
-- identyczne (kanał, ładunek) z jednej transakcji PostgreSQL scala w jedno powiadomienie.
CREATE FUNCTION f_praca_w_tle_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_notify('praca_w_tle', NEW.pula);
    RETURN NULL;
END $$;
CREATE TRIGGER trg_praca_w_tle_powiadom AFTER INSERT OR UPDATE OF status, dostepna_od ON praca_w_tle
    FOR EACH ROW WHEN (NEW.status = 'OCZEKUJE' AND NEW.dostepna_od <= now())
    EXECUTE FUNCTION f_praca_w_tle_powiadom();

CREATE FUNCTION f_zdarzenie_rozwiazania(rozwiazanie_id uuid) RETURNS void LANGUAGE sql AS $$
    SELECT pg_notify('lemat_zdarzenia', json_build_object('typ', 'ROZWIAZANIE', 'id', rozwiazanie_id)::text)
$$;
CREATE FUNCTION f_rozwiazanie_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN PERFORM f_zdarzenie_rozwiazania(NEW.id); RETURN NULL; END $$;
CREATE TRIGGER trg_rozwiazanie_powiadom AFTER UPDATE ON rozwiazanie
    FOR EACH ROW WHEN (OLD.version IS DISTINCT FROM NEW.version OR OLD.status IS DISTINCT FROM NEW.status)
    EXECUTE FUNCTION f_rozwiazanie_powiadom();

CREATE FUNCTION f_ocena_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN PERFORM f_zdarzenie_rozwiazania(NEW.rozwiazanie_id); RETURN NULL; END $$;
CREATE TRIGGER trg_ocena_powiadom AFTER INSERT OR UPDATE OF status, wskazowka ON ocena
    FOR EACH ROW EXECUTE FUNCTION f_ocena_powiadom();

CREATE FUNCTION f_weryfikacja_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN PERFORM f_zdarzenie_rozwiazania(NEW.rozwiazanie_id); RETURN NULL; END $$;
CREATE TRIGGER trg_weryfikacja_powiadom AFTER INSERT OR UPDATE OF status, termin_at, typ_kolejki ON weryfikacja
    FOR EACH ROW EXECUTE FUNCTION f_weryfikacja_powiadom();

CREATE FUNCTION f_powiadomienie_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_notify('lemat_zdarzenia',
        json_build_object('typ', 'POWIADOMIENIE', 'id', NEW.id, 'uzytkownik_id', NEW.uzytkownik_id)::text);
    RETURN NULL;
END $$;
CREATE TRIGGER trg_powiadomienie_powiadom AFTER INSERT ON powiadomienie
    FOR EACH ROW EXECUTE FUNCTION f_powiadomienie_powiadom();

CREATE FUNCTION f_import_arkusza_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_notify('lemat_zdarzenia', json_build_object('typ', 'IMPORT_ARKUSZA', 'id', NEW.id)::text);
    RETURN NULL;
END $$;
CREATE TRIGGER trg_import_arkusza_powiadom AFTER UPDATE OF status, raport ON import_arkusza
    FOR EACH ROW EXECUTE FUNCTION f_import_arkusza_powiadom();

CREATE FUNCTION f_zlecenie_generowania_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_notify('lemat_zdarzenia', json_build_object('typ', 'ZLECENIE_GENEROWANIA', 'id', NEW.id)::text);
    RETURN NULL;
END $$;
CREATE TRIGGER trg_zlecenie_generowania_powiadom AFTER UPDATE OF status, liczba_cykli, zadanie_id ON zlecenie_generowania
    FOR EACH ROW EXECUTE FUNCTION f_zlecenie_generowania_powiadom();

CREATE FUNCTION f_konfiguracja_modelu_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_notify('konfiguracja_modelu', NEW.rola::text);
    RETURN NULL;
END $$;
CREATE TRIGGER trg_konfiguracja_modelu_powiadom AFTER INSERT OR UPDATE OF aktywna ON konfiguracja_modelu
    FOR EACH ROW EXECUTE FUNCTION f_konfiguracja_modelu_powiadom();

