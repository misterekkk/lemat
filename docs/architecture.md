# Architektura (wstępna)

Dokument opisuje wstępną koncepcję systemu. Konkretne technologie zostaną
ostatecznie wybrane na kolejnych etapach projektu.

## Dwa rodzaje modeli

System łączy dwa typy modeli AI, każdy w roli, w której jest najmocniejszy:

| | Model decyzyjny (System One, np. **Jev**) | Model generatywny (LLM) |
|---|---|---|
| Zwraca | typowane decyzje z kalibrowanym prawdopodobieństwem | tekst, LaTeX, wyjaśnienia |
| Szybkość / koszt | bardzo szybki i tani (setki ms) | wolniejszy i droższy |
| Rola w systemie | ocena, wybór, klasyfikacja, „czy jest dobrze?” | generowanie zadań, wskazówek, wkładek teoretycznych |
| Ograniczenie | nie uzasadnia decyzji, odpowiedzi muszą być znane z góry | trudniej ocenić, na ile jest pewny |

Zasada: **Jev decyduje, LLM pisze.** Gdy Jev jest pewny, system działa
automatycznie; gdy nie, sprawa trafia do wolniejszego modelu lub do nauczyciela.

## System agentów

```
                ┌───────────────────┐
   Uczeń ──────►│   Orkiestrator    │◄────── Nauczyciel
                └─────────┬─────────┘
      ┌───────────┬───────┴───────┬──────────────┬──────────────┐
      ▼           ▼               ▼              ▼              ▼
 Agent         Agent          Agent          Agent          Agent
 rekomendujący generujący     rozpoznający   oceniający     wyjaśniający
 (drzewo+Jev)  (LLM)          (CV/OCR)       (Jev)          (LLM)
      │           │               │              │              │
      └───────────┴──── Baza zadań, profile uczniów, oceny ─────┘
```

### Agent rozpoznający (computer vision)

- Wejście: zdjęcie kartki.
- Wyjście: transkrypcja w tekście i LaTeX, podzielona na kroki rozwiązania,
  z zaznaczeniem fragmentów odczytanych niepewnie.
- Potrzebny, bo Jev przyjmuje tylko tekst – nie przeczyta zdjęcia sam.
- Kandydaci: modele OCR dla wzorów matematycznych lub modele multimodalne.

### Agent oceniający (Jev)

- Wejście (stan przekazywany do Jev): treść zadania, rozwiązanie wzorcowe,
  kryteria oceniania, transkrypcja rozwiązania ucznia.
- Pytania zadawane modelowi:
  - **Noul** – „Czy wynik końcowy jest poprawny?” → prawdopodobieństwo tak/nie.
  - **Score** – punkty za każde kryterium oceniania CKE (np. 0–1 za każdy krok).
  - **Choice** – „W którym kroku jest pierwszy błąd?” → numer kroku lub „brak”.
  - **Choice** – typ błędu: rachunkowy / merytoryczny / niepełne rozwiązanie / błąd odczytu.
- Każda odpowiedź ma kalibrowane prawdopodobieństwo = **pewność oceny**.
- Decyzja:
  - pewność ≥ próg → ocena wystawiona automatycznie,
  - pewność < próg → druga ocena przez mocniejszy LLM, a gdy nadal brak
    zgody → weryfikacja nauczyciela.
- Dodatkowo obliczenia mogą być sprawdzane symbolicznie (np. SymPy).

### Agent wyjaśniający (LLM)

- Jev nie podaje uzasadnienia, więc na podstawie jego decyzji (błędny krok,
  typ błędu) LLM pisze uczniowi krótką wskazówkę – bez podawania gotowego
  rozwiązania.

### Agent rekomendujący (model drzewiasty + Jev)

- Wejście: profil ucznia, historia rozwiązań.
- Wyjście: kolejna umiejętność, poziom trudności, decyzja „najpierw teoria
  czy od razu zadania”.
- Model drzewiasty (np. gradient boosting) przewiduje prawdopodobieństwo, że
  uczeń poprawnie rozwiąże zadanie z danej umiejętności i poziomu.
- Jev może podejmować decyzje na podstawie opisu sytuacji ucznia
  (np. Choice: „wkładka teoretyczna / zadanie łatwe / zadanie maturalne / powtórka”).
- Zasada: wybierz najsłabszą umiejętność i zadanie o przewidywanym
  prawdopodobieństwie sukcesu ok. 60–75% (strefa najbliższego rozwoju).
- Oba podejścia można porównać eksperymentalnie.

### Agent generujący (LLM)

- Wejście: umiejętność, poziom trudności, przykładowe zadania bazowe.
- Wyjście: treść, rozwiązanie wzorcowe, kryteria oceniania, wkładka teoretyczna.
- Walidacja: drugi model rozwiązuje zadanie niezależnie, wyniki są
  porównywane (SymPy); Jev ocenia (Noul) „czy zadanie jest jednoznaczne”
  i „czy jest zgodne z podstawą programową”.

## Przepływ oceny rozwiązania

1. Uczeń przesyła zdjęcie kartki.
2. Agent rozpoznający tworzy transkrypcję; uczeń ją zatwierdza lub poprawia.
3. Jev ocenia rozwiązanie i zwraca punkty, błędny krok oraz pewność.
4. Pewność ≥ próg → ocena automatyczna; w przeciwnym razie → LLM, a potem nauczyciel.
5. Agent wyjaśniający pisze wskazówkę do błędnego kroku.
6. Wynik aktualizuje profil ucznia; agent rekomendujący wybiera kolejny krok.

## Otwarte pytania

- Jaki próg pewności przyjąć i jak go kalibrować na ocenach nauczycieli?
- Czy Jev poprawnie radzi sobie z zapisem matematycznym (LaTeX) w stanie wejściowym?
- Czy oceniać całe rozwiązanie naraz, czy każdy krok osobnym pytaniem?
- Jak potraktować błędy odczytu pisma, żeby nie obniżały oceny ucznia?
- Jev jest we wczesnym dostępie (lista oczekujących) – potrzebny plan B,
  np. LLM zwracający ustrukturyzowane odpowiedzi z oszacowaną pewnością.
- Skąd wziąć zbiór odręcznych rozwiązań z ocenami do testów?
