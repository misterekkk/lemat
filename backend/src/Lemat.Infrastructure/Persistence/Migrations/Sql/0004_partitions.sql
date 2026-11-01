-- ===================== 0004_partitions.sql =====================
CREATE FUNCTION utworz_partycje_decyzji(miesiac date) RETURNS void LANGUAGE plpgsql AS $$
DECLARE
    od    date := date_trunc('month', miesiac)::date;
    nazwa text := 'decyzja_modelu_' || to_char(od, 'YYYY_MM');
BEGIN
    EXECUTE format('CREATE TABLE IF NOT EXISTS %I PARTITION OF decyzja_modelu FOR VALUES FROM (%L) TO (%L)',
                   nazwa, od, (od + interval '1 month')::date);
END $$;
-- Migracja początkowa: bieżący miesiąc i 3 kolejne; dalej praca UTRZYMAJ_PARTYCJE (13.3).
SELECT utworz_partycje_decyzji((date_trunc('month', now()) + make_interval(months => m))::date)
FROM generate_series(0, 3) AS m;

