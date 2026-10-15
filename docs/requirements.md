# Wymagania

## Aktorzy

- **Uczeń** – rozwiązuje zadania i przesyła rozwiązania.
- **Nauczyciel** – zarządza klasą, zadaniami i weryfikuje oceny.
- **Administrator** – zarządza kontami, bazą zadań i konfiguracją modeli.

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
- **FR-12** Wygenerowane zadanie zawiera treść, rozwiązanie wzorcowe i kryteria oceniania.
- **FR-13** Przed udostępnieniem zadanie jest walidowane (poprawność matematyczna, jednoznaczność, zgodność z podstawą programową).
- **FR-14** Nauczyciel może przejrzeć, poprawić lub odrzucić wygenerowane zadanie.

### Przesyłanie i ocena rozwiązań

- **FR-15** Uczeń przesyła zdjęcie (jedno lub kilka) kartki z rozwiązaniem.
- **FR-16** System odczytuje pismo odręczne, w tym wzory matematyczne, i pokazuje uczniowi transkrypcję.
- **FR-17** Uczeń może poprawić błędnie odczytany fragment transkrypcji przed oceną.
- **FR-18** System ocenia rozwiązanie według kryteriów oceniania i przyznaje punkty, także cząstkowe.
- **FR-19** System wyznacza pewność oceny.
- **FR-20** Gdy pewność przekracza próg, ocena jest wystawiana automatycznie.
- **FR-21** Gdy pewność jest poniżej progu, rozwiązanie trafia do weryfikacji nauczyciela, a uczeń widzi ocenę wstępną.
- **FR-22** Ocena zawiera wskazanie błędnego kroku i krótką wskazówkę.
- **FR-23** Wynik oceny aktualizuje profil ucznia.

### Nauczyciel

- **FR-24** Nauczyciel tworzy klasę i zaprasza do niej uczniów.
- **FR-25** Nauczyciel widzi postępy klasy i poszczególnych uczniów.
- **FR-26** Nauczyciel przypisuje zadania lub zestawy zadań.
- **FR-27** Nauczyciel weryfikuje i koryguje oceny systemu; korekty są zapisywane do dalszego ulepszania modeli.

### Zarządzanie treścią

- **FR-28** Administrator importuje zadania z arkuszy CKE wraz z kryteriami oceniania.
- **FR-29** Każde zadanie jest otagowane działem, umiejętnościami i poziomem trudności.

## Wymagania niefunkcjonalne

- **NFR-01** Ocena rozwiązania trwa nie dłużej niż 30 sekund.
- **NFR-02** Ocena automatyczna jest zgodna z oceną nauczyciela w co najmniej 90% przypadków (mierzone na zbiorze testowym).
- **NFR-03** Odsetek ocen automatycznie zaliczonych, które okazały się błędne, nie przekracza 2%.
- **NFR-04** Platforma działa w przeglądarce na telefonie i komputerze; zdjęcie można zrobić bezpośrednio z aplikacji.
- **NFR-05** Dane uczniów są przetwarzane zgodnie z RODO; zdjęcia są usuwane po określonym czasie.
- **NFR-06** Każda ocena i rekomendacja jest możliwa do wyjaśnienia: zapisywane są decyzje modelu z prawdopodobieństwami, a uczeń dostaje wskazówkę tekstową wygenerowaną na ich podstawie.
- **NFR-09** Pewność oceny jest skalibrowana – system mierzy, czy deklarowana pewność odpowiada rzeczywistej zgodności z ocenami nauczycieli.
- **NFR-10** Gdy model decyzyjny jest niedostępny, system przełącza się na model zapasowy (LLM ze strukturyzowaną odpowiedzią).
- **NFR-07** Modele AI są wymienne – zmiana dostawcy modelu nie wymaga zmian w logice platformy.
- **NFR-08** Interfejs jest w języku polskim.

## Poza zakresem (na razie)

- Przedmioty inne niż matematyka.
- Zadania otwarte z dowodami o dowolnej formie (tylko w trybie weryfikacji przez nauczyciela).
- Aplikacja natywna na telefon.
