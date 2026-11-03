#!/usr/bin/env bash
# Test dymny przepływu POC na działającym API (TASK-130; używany przez TASK-140). Wymaga: scripts/dev-up.sh, curl, jq.
# Przepływ: rejestracja (nowy Uczeń) → logowanie → PATCH /uczniowie/ja → lista Zadań → przesłanie JPEG → odpytywanie do TRANSKRYPCJA_GOTOWA
#           → zatwierdzenie → odpytywanie do OCENIONE → Ocena → profil uproszczony → drugi Uczeń (konto demo) nie widzi cudzego Rozwiązania.
# Uzycie: scripts/poc-smoke.sh [adres_api]   (domyslnie http://localhost:5080). Kod wyjscia 0 = sukces.
set -euo pipefail
API="${1:-${LEMAT_API_URL:-http://localhost:5080}}/api/v1"
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
command -v jq >/dev/null || { echo "Wymagany jq" >&2; exit 2; }

krok() { echo "== $*"; }
blad() { echo "BLAD: $*" >&2; exit 1; }

# zadaj METODA SCIEZKA OCZEKIWANY_STATUS [argumenty curl...] → ciało w $TMP/body, nagłówki w $TMP/headers
zadaj() {
  local metoda="$1" sciezka="$2" oczekiwany="$3"; shift 3
  local status
  status=$(curl -sS -o "$TMP/body" -D "$TMP/headers" -w '%{http_code}' -X "$metoda" "$API$sciezka" "$@")
  [ "$status" = "$oczekiwany" ] || { cat "$TMP/body" >&2; echo >&2; blad "$metoda $sciezka → HTTP $status (oczekiwano $oczekiwany)"; }
}

# Minimalny poprawny JPEG 640x480 (SOI, JFIF, SOF0, SOS, EOI) - Mock nie czyta pikseli.
printf '\xff\xd8\xff\xe0\x00\x10JFIF\x00\x01\x01\x00\x00\x01\x00\x01\x00\x00\xff\xc0\x00\x0b\x08\x01\xe0\x02\x80\x01\x01\x11\x00\xff\xda\x00\x08\x01\x01\x00\x00\x3f\x00\x07\x01\x02\x03\xff\xd9' >"$TMP/kartka.jpg"

EMAIL="smoke.$(date +%s%N)@example.com"
HASLO="BardzoTajne#2026"

krok "zdrowie"
zadaj GET /zdrowie 200

krok "rejestracja Ucznia $EMAIL"
zadaj POST /auth/rejestracja 201 -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"haslo\":\"$HASLO\",\"imie\":\"Smoke\",\"nazwisko\":\"Test\",\"rola\":\"UCZEN\",\"oswiadczenie_wieku_16\":true,\"zgoda_regulamin\":true,\"kod_zaproszenia\":null}"

krok "logowanie"
zadaj POST /auth/logowanie 200 -H 'Content-Type: application/json' -d "{\"email\":\"$EMAIL\",\"haslo\":\"$HASLO\"}"
TOKEN=$(jq -r '.access_token' "$TMP/body")
[ -n "$TOKEN" ] && [ "$TOKEN" != null ] || blad "brak access_token"
AUTH=(-H "Authorization: Bearer $TOKEN")

krok "GET /uczniowie/ja i PATCH (If-Match)"
zadaj GET /uczniowie/ja 200 "${AUTH[@]}"
WERSJA=$(jq -r '.version' "$TMP/body")
zadaj PATCH /uczniowie/ja 200 "${AUTH[@]}" -H 'Content-Type: application/merge-patch+json' -H "If-Match: \"$WERSJA\"" \
  -d '{"poziom_matury":"PODSTAWOWY","termin_egzaminu":"2027-05-05","wybrany_dzial_id":3}'

krok "lista Zadań"
zadaj GET /zadania 200 "${AUTH[@]}"
LICZBA=$(jq '.elementy | length' "$TMP/body")
[ "$LICZBA" -ge 1 ] || blad "pusta lista Zadań (czy uruchomiono seed-poc?)"
ZADANIE_ID=$(jq -r '.elementy[1].id // .elementy[0].id' "$TMP/body")
echo "   Zadań: $LICZBA, wybrane: $ZADANIE_ID"
zadaj GET "/zadania/$ZADANIE_ID" 200 "${AUTH[@]}"
WERSJA_ZADANIA=$(jq -r '.wersja_id // .zadanie_wersja_id // .aktualna_wersja_id // empty' "$TMP/body")
# Zadanie POC: wersja ma id b1…0N odpowiadające a1…0N, gdy odpowiedź nie niesie jawnego id wersji.
[ -n "$WERSJA_ZADANIA" ] || WERSJA_ZADANIA="b1${ZADANIE_ID:2}"

krok "przesłanie zdjęcia JPEG (wersja Zadania $WERSJA_ZADANIA)"
zadaj POST /rozwiazania 202 "${AUTH[@]}" -F "zadanie_wersja_id=$WERSJA_ZADANIA" -F "zdjecia=@$TMP/kartka.jpg;type=image/jpeg"
ROZWIAZANIE=$(jq -r '.id' "$TMP/body")
[ "$(jq -r '.status' "$TMP/body")" = PRZESLANE ] || blad "oczekiwano statusu PRZESLANE"
echo "   Rozwiązanie: $ROZWIAZANIE"

czekaj() { # $1 = oczekiwany status
  for _ in $(seq 1 120); do
    zadaj GET "/rozwiazania/$ROZWIAZANIE" 200 "${AUTH[@]}"
    local s; s=$(jq -r '.status' "$TMP/body")
    [ "$s" = "$1" ] && return 0
    [ "$s" = BLAD_ROZPOZNAWANIA ] && blad "BLAD_ROZPOZNAWANIA"
    sleep 0.5
  done
  blad "Rozwiązanie nie osiągnęło statusu $1 w 60 s (czy działa Worker?)"
}

krok "odpytywanie do TRANSKRYPCJA_GOTOWA"
czekaj TRANSKRYPCJA_GOTOWA
KROKI=$(jq '.transkrypcja.kroki | length' "$TMP/body")
[ "$KROKI" -ge 1 ] || blad "Transkrypcja bez kroków"
echo "   kroków Transkrypcji: $KROKI"
VER=$(jq -r '.version' "$TMP/body")
BODY_ZATW=$(jq -c '{transkrypcja_id: .transkrypcja.id, wersja: .transkrypcja.wersja}' "$TMP/body")

krok "zatwierdzenie Transkrypcji"
zadaj POST "/rozwiazania/$ROZWIAZANIE/zatwierdzenie" 202 "${AUTH[@]}" -H 'Content-Type: application/json' -H "If-Match: \"$VER\"" -d "$BODY_ZATW"
[ "$(jq -r '.status' "$TMP/body")" = ZATWIERDZONE ] || blad "oczekiwano statusu ZATWIERDZONE"

krok "odpytywanie do OCENIONE"
czekaj OCENIONE

krok "GET /rozwiazania/{id}/ocena"
zadaj GET "/rozwiazania/$ROZWIAZANIE/ocena" 200 "${AUTH[@]}"
PUNKTY=$(jq -r '.punkty' "$TMP/body"); MAX=$(jq -r '.punkty_max' "$TMP/body")
[ "$(jq -r '.status' "$TMP/body")" = OSTATECZNA ] || blad "Ocena nie jest OSTATECZNA"
echo "   Ocena: $PUNKTY / $MAX pkt, pewność $(jq -r '.pewnosc_oceny' "$TMP/body")"
jq -r '.wskazowka // .komunikat // empty' "$TMP/body" | head -c 300 | sed 's/^/   Wskazówka: /' || true

krok "profil uproszczony"
zadaj GET /uczniowie/ja/profil-uproszczony 200 "${AUTH[@]}"
[ "$(jq -r '.liczba_ocenionych' "$TMP/body")" = 1 ] || blad "profil: oczekiwano liczba_ocenionych = 1"
echo "   suma punktów: $(jq -r '.suma_punktow' "$TMP/body") / $(jq -r '.suma_punktow_max' "$TMP/body"), średni procent: $(jq -r '.sredni_procent_punktow' "$TMP/body")"

krok "izolacja: konto demo uczen2 nie widzi cudzego Rozwiązania"
zadaj POST /auth/logowanie 200 -H 'Content-Type: application/json' -d '{"email":"uczen2@lemat.local","haslo":"Lemat-Demo-2026!"}'
TOKEN2=$(jq -r '.access_token' "$TMP/body")
zadaj GET "/rozwiazania/$ROZWIAZANIE" 404 -H "Authorization: Bearer $TOKEN2"
zadaj GET "/rozwiazania/$ROZWIAZANIE/ocena" 404 -H "Authorization: Bearer $TOKEN2"

echo
echo "POC SMOKE: OK — Ocena $PUNKTY / $MAX pkt"
