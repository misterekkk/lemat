-- ===================== 0007_api.sql (3.3, C8-03, ADR-068; 3.5: C9-01, C9-07 p. 1) =====================
-- Rezerwacja klucza idempotencji w krótkiej transakcji przed wykonaniem; odpowiedź 2xx zapisuje
-- przypadek użycia w SWOJEJ transakcji: UPDATE … SET kod_statusu_http, cialo_http, w_toku_do = NULL,
-- rezerwacja = NULL WHERE (uzytkownik_id, klucz) AND rezerwacja = @moja (0 wierszy → ROLLBACK skutku i powtórka).
CREATE FUNCTION zarezerwuj_klucz_idempotencji(p_uzytkownik_id uuid, p_klucz uuid, p_skrot bytea, p_rezerwacja uuid)
RETURNS TABLE (wynik text, kod_statusu_http smallint, cialo_http jsonb, retry_after_s integer)
LANGUAGE plpgsql AS $$
DECLARE r klucz_idempotencji;
BEGIN
    FOR proba IN 1..2 LOOP                                   -- 2. obieg: wiersz usunięty przez sprzątanie między INSERT a SELECT
        INSERT INTO klucz_idempotencji AS k (uzytkownik_id, klucz, skrot_zapytania, rezerwacja, w_toku_do, wygasa_at)
        VALUES (p_uzytkownik_id, p_klucz, p_skrot, p_rezerwacja, now() + interval '60 seconds', now() + interval '24 hours')
        ON CONFLICT (uzytkownik_id, klucz) DO UPDATE
            SET rezerwacja       = EXCLUDED.rezerwacja,
                w_toku_do        = EXCLUDED.w_toku_do,
                skrot_zapytania  = EXCLUDED.skrot_zapytania,
                kod_statusu_http = NULL,
                cialo_http       = NULL,
                wygasa_at        = CASE WHEN k.wygasa_at <= now() THEN EXCLUDED.wygasa_at ELSE k.wygasa_at END,
                created_at       = CASE WHEN k.wygasa_at <= now() THEN now() ELSE k.created_at END
            WHERE k.wygasa_at <= now()                                        -- 3.5 (C9-07 p. 1): wiersz po 24 h = brak klucza
               OR (k.kod_statusu_http IS NULL AND k.w_toku_do < now()        -- przejęcie po awarii procesu …
                   AND k.skrot_zapytania = EXCLUDED.skrot_zapytania)          -- … wyłącznie przez to samo zapytanie (C9-07 p. 1)
        RETURNING * INTO r;
        IF FOUND THEN
            RETURN QUERY SELECT 'WYKONAJ'::text, NULL::smallint, NULL::jsonb, NULL::integer; RETURN;
        END IF;
        SELECT * INTO r FROM klucz_idempotencji k WHERE k.uzytkownik_id = p_uzytkownik_id AND k.klucz = p_klucz;
        IF FOUND THEN
            IF r.skrot_zapytania <> p_skrot THEN
                RETURN QUERY SELECT 'KONFLIKT_CIALA'::text, NULL::smallint, NULL::jsonb, NULL::integer; RETURN;
            END IF;
            IF r.kod_statusu_http IS NOT NULL THEN
                RETURN QUERY SELECT 'POWTORKA'::text, r.kod_statusu_http, r.cialo_http, NULL::integer; RETURN;
            END IF;
            RETURN QUERY SELECT 'W_TOKU'::text, NULL::smallint, NULL::jsonb,
                                GREATEST(1, ceil(extract(epoch FROM r.w_toku_do - now())))::integer;
            RETURN;
        END IF;
    END LOOP;
    RAISE EXCEPTION 'KLUCZ_IDEMPOTENCJI_NIEUSTALONY';
END $$;
GRANT EXECUTE ON FUNCTION zarezerwuj_klucz_idempotencji(uuid, uuid, bytea, uuid) TO lemat_api;
