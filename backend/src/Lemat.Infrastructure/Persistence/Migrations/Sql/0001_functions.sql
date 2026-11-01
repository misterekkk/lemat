-- ===================== 0001_functions.sql =====================
-- Kalendarz dni roboczych (A-15, ADR-043). Przykłady: piątek 18:00 + 3 → środa 18:00; sobota 10:00 + 3 → czwartek 08:00.
CREATE FUNCTION dodaj_dni_robocze(od timestamptz, n integer) RETURNS timestamptz
LANGUAGE plpgsql STABLE AS $$
DECLARE
    lokalny timestamp := od AT TIME ZONE 'Europe/Warsaw';
    dzien   date      := lokalny::date;
    godzina time      := lokalny::time;
    dodane  integer   := 0;
BEGIN
    IF n < 0 THEN RAISE EXCEPTION 'DNI_ROBOCZE_UJEMNE'; END IF;
    IF extract(isodow FROM dzien) IN (6, 7) OR EXISTS (SELECT 1 FROM dzien_wolny WHERE data = dzien) THEN
        LOOP
            dzien := dzien + 1;
            EXIT WHEN extract(isodow FROM dzien) NOT IN (6, 7)
                  AND NOT EXISTS (SELECT 1 FROM dzien_wolny WHERE data = dzien);
        END LOOP;
        godzina := time '08:00';
    END IF;
    WHILE dodane < n LOOP
        dzien := dzien + 1;
        IF extract(isodow FROM dzien) NOT IN (6, 7) AND NOT EXISTS (SELECT 1 FROM dzien_wolny WHERE data = dzien) THEN
            dodane := dodane + 1;
        END IF;
    END LOOP;
    RETURN (dzien + godzina) AT TIME ZONE 'Europe/Warsaw';
END $$;

-- Poziom trudności z parametru b modelu Elo z histerezą 0,1 (10.1.3, ADR-029)
CREATE FUNCTION poziom_bez_histerezy(b numeric) RETURNS smallint LANGUAGE sql IMMUTABLE AS $$
    SELECT CASE WHEN b < -1.2 THEN 1 WHEN b < -0.4 THEN 2 WHEN b < 0.4 THEN 3 WHEN b < 1.2 THEN 4 ELSE 5 END::smallint
$$;
CREATE FUNCTION poziom_z_b(b numeric, obecny smallint) RETURNS smallint LANGUAGE sql IMMUTABLE AS $$
    SELECT CASE
        WHEN poziom_bez_histerezy(b - 0.1) > obecny THEN poziom_bez_histerezy(b - 0.1)   -- w górę: granica przekroczona o ≥ 0,1
        WHEN poziom_bez_histerezy(b + 0.1) < obecny THEN poziom_bez_histerezy(b + 0.1)   -- w dół: granica przekroczona o ≥ 0,1
        ELSE obecny END
$$;

