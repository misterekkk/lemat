-- ===================== 0005_permissions.sql =====================
-- Właścicielem schematu jest lemat_migracje (jedyny użytkownik z DDL, 5.4).
-- 3.2 (C7-15 p. 4): role aplikacji tworzy infrastruktura (Terraform, docker compose: init-db.sh) jako role z LOGIN
-- przed pierwszą migracją; na czystej bazie (Testcontainers, CI) blok DO tworzy je jako role NOLOGIN,
-- więc migracja nie zależy od środowiska. Istniejących ról blok nie zmienia.
DO $$
DECLARE r text;
BEGIN
    FOREACH r IN ARRAY ARRAY['lemat_api','lemat_worker','lemat_scheduler'] LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = r) THEN
            EXECUTE format('CREATE ROLE %I NOLOGIN', r);
        END IF;
    END LOOP;
END $$;
GRANT USAGE ON SCHEMA public TO lemat_api, lemat_worker, lemat_scheduler;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO lemat_api, lemat_worker, lemat_scheduler;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO lemat_api, lemat_worker, lemat_scheduler;
-- Dziennik audytu i Zdarzenia RODO są dopisywane, nie zmieniane (NFR-05, ADR-041):
REVOKE UPDATE, DELETE ON dziennik_audytu FROM lemat_api, lemat_worker, lemat_scheduler;
REVOKE UPDATE, DELETE ON zdarzenie_rodo FROM lemat_api, lemat_scheduler;
REVOKE UPDATE ON zdarzenie_rodo FROM lemat_worker;     -- DELETE po 2 latach wykonuje WYCZYSC_DANE_TECHNICZNE (worker)
ALTER DEFAULT PRIVILEGES IN SCHEMA public                -- 3.2: dla roli wykonującej migrację (lemat_migracje w produkcji)
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO lemat_api, lemat_worker, lemat_scheduler;

