# Słownik pojęć

| Pojęcie | Definicja |
|---|---|
| **Uczeń** | Osoba przygotowująca się do matury, która rozwiązuje zadania na platformie. |
| **Nauczyciel** | Osoba prowadząca klasę; przypisuje zadania, przegląda postępy i weryfikuje oceny wystawione przez system. |
| **Zadanie** | Pojedyncze polecenie matematyczne z treścią, rozwiązaniem wzorcowym, kryteriami oceniania i przypisanym działem. |
| **Zadanie bazowe** | Zadanie pochodzące z arkuszy CKE lub dodane ręcznie przez nauczyciela. |
| **Zadanie wygenerowane** | Zadanie utworzone przez agenta generującego, wzorowane na zadaniach bazowych. Przed udostępnieniem przechodzi walidację. |
| **Dział** | Obszar programu matury, np. funkcje, ciągi, geometria analityczna, prawdopodobieństwo, optymalizacja. |
| **Umiejętność** | Węższa kompetencja w obrębie działu, np. „wyznaczanie ekstremum funkcji kwadratowej na przedziale”. |
| **Profil ucznia** | Aktualny szacowany poziom opanowania każdej umiejętności przez ucznia. |
| **Poziom trudności** | Skala trudności zadania (np. 1–5), kalibrowana na podstawie wyników uczniów. |
| **Ścieżka nauki** | Kolejność wkładek teoretycznych i zadań zaproponowana uczniowi przez agenta rekomendującego. |
| **Wkładka teoretyczna** | Krótki materiał wyjaśniający teorię potrzebną do rozwiązania zadań z danej umiejętności. |
| **Rozwiązanie** | Praca ucznia nad zadaniem przesłana jako zdjęcie kartki. |
| **Transkrypcja** | Tekstowa postać rozwiązania (tekst + LaTeX) uzyskana ze zdjęcia przez moduł rozpoznawania pisma. |
| **Kryteria oceniania** | Lista kroków rozwiązania i punktów za każdy krok, w formacie zgodnym z zasadami CKE. |
| **Ocena** | Liczba punktów przyznanych za rozwiązanie wraz z uzasadnieniem i wskazaniem błędnego kroku. |
| **Pewność oceny** | Oszacowane przez system prawdopodobieństwo, że ocena jest poprawna. |
| **Próg pewności** | Wartość pewności, powyżej której ocena jest wystawiana automatycznie; poniżej trafia do weryfikacji. |
| **Weryfikacja** | Sprawdzenie oceny przez nauczyciela, gdy pewność systemu jest zbyt niska. |
| **Agent** | Komponent AI realizujący jedno zadanie w systemie (rekomendacja, generowanie, rozpoznawanie, ocenianie). |
| **Orkiestrator** | Komponent koordynujący współpracę agentów i przepływ danych między nimi. |
| **Model System One** | Model AI zwracający zamiast tekstu typowane decyzje (wybór, wynik, tak/nie) z kalibrowanym prawdopodobieństwem, przeznaczone bezpośrednio dla kodu aplikacji. |
| **Jev** | Model typu System One firmy TypeSafe; w projekcie używany jako silnik decyzyjny agenta oceniającego. |
| **Kalibracja** | Właściwość modelu, w której deklarowane prawdopodobieństwo odpowiada rzeczywistej trafności (np. decyzje z pewnością 90% są trafne w ok. 90% przypadków). |
| **Model generatywny (LLM)** | Model językowy tworzący tekst; w projekcie generuje zadania, wkładki teoretyczne i wskazówki. |
| **Arkusz CKE** | Oficjalny arkusz egzaminacyjny Centralnej Komisji Egzaminacyjnej wraz z zasadami oceniania. |
