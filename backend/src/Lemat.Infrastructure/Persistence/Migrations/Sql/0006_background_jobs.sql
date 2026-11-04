-- ===================== 0006_background_jobs.sql (3.2, C7-03) =====================
-- Ponowienie i odzyskiwanie dzierżaw bez naruszenia ux_praca_dedup_oczekujace: przy typach TrybDeduplikacji.TylkoOczekujace*
-- może istnieć praca A W_TOKU i praca B OCZEKUJE z tym samym kluczem. Powrót A do OCZEKUJE łamałby indeks, więc
-- A kończy się jako ZAKONCZONA z ostatni_blad = 'ZASTAPIONA_NOWSZA' (B ma nowszy ładunek albo przetworzy wszystkie
-- niezastosowane Wyniki zadania). Każdy wiersz jest zmieniany w osobnym bloku EXCEPTION (podtransakcja), więc konflikt
-- jednej pracy nie wycofuje odzyskania pozostałych; wyścig z równoległym INSERT B (zatwierdzonym po odczycie) też trafia
-- w gałąź unique_violation.
CREATE FUNCTION ponow_prace(p_id bigint, p_wykonawca text, p_blad text, p_odstep interval)
RETURNS status_pracy LANGUAGE plpgsql AS $$
DECLARE v_status status_pracy;
BEGIN
    BEGIN
        UPDATE praca_w_tle p
        SET status = CASE WHEN p.proby >= p.maks_prob THEN 'MARTWA' ELSE 'OCZEKUJE' END::status_pracy,
            dostepna_od = now() + p_odstep, ostatni_blad = p_blad, wykonawca = NULL, dzierzawa_do = NULL, updated_at = now()
        WHERE p.id = p_id AND p.wykonawca = p_wykonawca AND p.status = 'W_TOKU'
        RETURNING p.status INTO v_status;
    EXCEPTION WHEN unique_violation THEN
        UPDATE praca_w_tle p
        SET status = 'ZAKONCZONA', ostatni_blad = 'ZASTAPIONA_NOWSZA', wykonawca = NULL, dzierzawa_do = NULL, updated_at = now()
        WHERE p.id = p_id AND p.wykonawca = p_wykonawca AND p.status = 'W_TOKU'
        RETURNING p.status INTO v_status;
    END;
    RETURN v_status;                                    -- NULL = utrata dzierżawy (ADR-056 p. 4)
END $$;

CREATE FUNCTION odzyskaj_dzierzawy(p_limit integer DEFAULT 500)
RETURNS TABLE (praca_id bigint, praca_typ text, praca_status status_pracy) LANGUAGE plpgsql AS $$
DECLARE r record;
BEGIN
    FOR r IN SELECT p.id FROM praca_w_tle p
             WHERE p.status = 'W_TOKU' AND p.dzierzawa_do < now()
             ORDER BY p.dzierzawa_do LIMIT p_limit
             FOR UPDATE SKIP LOCKED                     -- wiersz potwierdzany przez jednostkę pracy (FOR UPDATE) jest pomijany
    LOOP
        BEGIN
            UPDATE praca_w_tle p
            SET status = CASE WHEN p.proby >= p.maks_prob THEN 'MARTWA' ELSE 'OCZEKUJE' END::status_pracy,
                wykonawca = NULL, dzierzawa_do = NULL, ostatni_blad = 'DZIERZAWA_WYGASLA', updated_at = now()
            WHERE p.id = r.id
            RETURNING p.id, p.typ, p.status INTO praca_id, praca_typ, praca_status;
        EXCEPTION WHEN unique_violation THEN
            UPDATE praca_w_tle p
            SET status = 'ZAKONCZONA', wykonawca = NULL, dzierzawa_do = NULL, ostatni_blad = 'ZASTAPIONA_NOWSZA', updated_at = now()
            WHERE p.id = r.id
            RETURNING p.id, p.typ, p.status INTO praca_id, praca_typ, praca_status;
        END;
        RETURN NEXT;
    END LOOP;
END $$;

