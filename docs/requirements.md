# Wymagania

## Aktorzy

- **Uczeń** – rozwiązuje zadania z platformy i przesyła zdjęcia swoich rozwiązań.
- **Nauczyciel** – zarządza klasą i zadaniami oraz weryfikuje oceny wystawione przez system.
- **Administrator** – zarządza kontami, bazą zadań i konfiguracją modeli.

## Rodzaje zadań

| Rodzaj | Przykład | Sposób oceny |
|---|---|---|
| **Zamknięte** | wybór A/B/C/D, prawda/fałsz | porównanie z kluczem, bez zdjęcia |
| **Otwarte obliczeniowe** | równanie, optymalizacja, prawdopodobieństwo | ocena każdego kroku według kryteriów CKE |
| **Otwarte z dowodem** | „Wykaż, że…”, „Udowodnij, że…” | ocena wstępna + zawsze weryfikacja nauczyciela (w przyszłości automatyzacja typowych algorytmów dowodowych) |

## Wymagania funkcjonalne

### Konto i profil

- **FR-01** Uczeń może założyć konto i zalogować się.
- **FR-02** Uczeń wybiera poziom matury (podstawowy / rozszerzony) i termin egzaminu.
- **FR-03** Nowy uczeń rozwiązuje krótki test diagnostyczny, na podstawie którego powstaje początkowy profil.
- **FR-04** Uczeń widzi swój profil: poziom opanowania każdego działu i umiejętności.

### Rekomendacja i nauka

- **FR-05** System proponuje uczniowi kolejne zadanie na podstawie jego profilu.
- **FR-06** Gdy umiejętność jest słabo opanowana, system proponuje wkładkę teoretyczną przed zadaniami.
- **FR-07** Trudność kolejnych zadań rośnie wraz z postępami ucznia.
- **FR-08** System okresowo przypomina zadania z działów już opanowanych (powtórki).
- **FR-09** Uczeń może samodzielnie wybrać dział, z którego chce ćwiczyć.
- **FR-10** System uzasadnia rekomendację (np. „3 z ostatnich 4 zadań z optymalizacji były błędne”).

### Generowanie zadań

- **FR-11** System generuje nowe zadanie dla wskazanej umiejętności i poziomu trudności.
- **FR-12** Wygenerowane zadanie zawiera treść, rozwiązanie wzorcowe i kryteria oceniania rozpisane na kroki.
- **FR-13** Przed udostępnieniem zadanie jest walidowane (poprawność matematyczna, jednoznaczność, zgodność z podstawą programową).
- **FR-14** Nauczyciel może przejrzeć, poprawić lub odrzucić wygenerowane zadanie.

### Przesyłanie rozwiązań

- **FR-15** Uczeń rozwiązuje zadanie zamknięte, zaznaczając odpowiedź bezpośrednio na platformie.
- **FR-16** Rozwiązanie zadania otwartego uczeń przesyła jako zdjęcie (jedno lub kilka) kartki; treść zadania jest znana systemowi i nie jest przesyłana.
- **FR-17** System odczytuje pismo odręczne, w tym wzory matematyczne, i dzieli transkrypcję na kolejne kroki rozwiązania.
- **FR-18** Uczeń widzi transkrypcję i może poprawić błędnie odczytany fragment przed oceną.

### Ocena rozwiązań

- **FR-19** System ocenia rozwiązanie zadania otwartego krok po kroku według kryteriów oceniania i przyznaje punkty, także cząstkowe.
- **FR-20** Poprawny wynik końcowy nie wystarcza do zaliczenia zadania – punkty za wynik są przyznawane tylko wtedy, gdy wynika on z przedstawionego rozumowania.
- **FR-21** System sprawdza, czy każdy krok rozwiązania wynika z poprzedniego (np. czy przekształcenia są równoważne).
- **FR-22** System wyznacza pewność oceny.
- **FR-23** Gdy pewność przekracza próg, ocena jest wystawiana automatycznie.
- **FR-24** Gdy pewność jest poniżej progu, rozwiązanie trafia do weryfikacji nauczyciela, a uczeń widzi ocenę wstępną.
- **FR-25** Gdy wynik końcowy jest poprawny, ale co najmniej jeden krok jest błędny lub go brakuje, rozwiązanie zawsze trafia do weryfikacji nauczyciela, niezależnie od pewności oceny.
- **FR-26** Dla zadań z dowodem system wystawia ocenę wstępną i zawsze przekazuje rozwiązanie do weryfikacji nauczyciela, niezależnie od pewności oceny.
- **FR-27** Ocena zawiera wskazanie pierwszego błędnego kroku, typ błędu (rachunkowy, merytoryczny, niepełne rozwiązanie) i krótką wskazówkę, bez podawania gotowego rozwiązania.
- **FR-28** Błędy wynikające z niepoprawnego odczytu pisma nie obniżają oceny ucznia.
- **FR-29** Wynik oceny aktualizuje profil ucznia.

### Nauczyciel

- **FR-30** Nauczyciel tworzy klasę i zaprasza do niej uczniów.
- **FR-31** Nauczyciel widzi postępy klasy i poszczególnych uczniów.
- **FR-32** Nauczyciel przypisuje zadania lub zestawy zadań.
- **FR-33** Nauczyciel ma kolejkę rozwiązań do weryfikacji, z oznaczeniem powodu (niska pewność, poprawny wynik przy błędnych krokach, zadanie z dowodem).
- **FR-34** Nauczyciel weryfikuje i koryguje oceny systemu; korekty są zapisywane do kalibracji progu pewności i ulepszania modeli.

### Zarządzanie treścią

- **FR-35** Administrator importuje zadania z arkuszy CKE wraz z kryteriami oceniania.
- **FR-36** Każde zadanie jest otagowane rodzajem, działem, umiejętnościami i poziomem trudności.

## Wymagania niefunkcjonalne

- **NFR-01** Ocena rozwiązania trwa nie dłużej niż 30 sekund.
- **NFR-02** Ocena automatyczna jest zgodna z oceną nauczyciela w co najmniej 90% przypadków (mierzone na zbiorze testowym).
- **NFR-03** Odsetek ocen automatycznie zaliczonych, które okazały się błędne, nie przekracza 2%.
- **NFR-04** Pewność oceny jest skalibrowana – system mierzy, czy deklarowana pewność odpowiada rzeczywistej zgodności z ocenami nauczycieli.
- **NFR-05** Każda ocena i rekomendacja jest możliwa do wyjaśnienia: zapisywane są decyzje modelu z prawdopodobieństwami, a uczeń dostaje wskazówkę tekstową wygenerowaną na ich podstawie.
- **NFR-06** Modele AI są wymienne – zmiana dostawcy modelu nie wymaga zmian w logice platformy.
- **NFR-07** Gdy model decyzyjny jest niedostępny, system przełącza się na model zapasowy (LLM ze strukturyzowaną odpowiedzią).
- **NFR-08** Platforma działa w przeglądarce na telefonie i komputerze; zdjęcie można zrobić bezpośrednio z aplikacji.
- **NFR-09** Dane uczniów są przetwarzane zgodnie z RODO; zdjęcia są usuwane po określonym czasie.
- **NFR-10** Interfejs jest w języku polskim.

## Poza zakresem (na razie)

- Przedmioty inne niż matematyka.
- Automatyczne zaliczanie zadań z dowodem bez weryfikacji nauczyciela (możliwe w przyszłości, po zebraniu wystarczającej liczby ocen nauczycieli).
- Aplikacja natywna na telefon.