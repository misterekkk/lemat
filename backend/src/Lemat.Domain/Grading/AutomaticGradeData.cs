// @Role: Dane wejściowe fabryki Oceny automatycznej (wynik Modelu decyzyjnego po kalibracji pewności i trasowaniu): punkty, błąd, pewność, próg, kalibrator, Kryteria | @DependsOn: StatusOceny, TypBledu, PewnoscOceny, PewnoscSurowa, WariantRozwiazania, Wskazowka, OcenaKryterium | @UsedBy: Ocena.Automatyczna, handler OCEN_ROZWIAZANIE (planowane TASK-128) | @Task: TASK-126
using Lemat.Domain.Calibration;
using Lemat.Domain.Content;
using Lemat.Domain.Models;
using Lemat.Domain.Solutions;

namespace Lemat.Domain.Grading;

public sealed record DaneOcenyAutomatycznej(
    RozwiazanieId RozwiazanieId,
    int Wersja,
    TranskrypcjaId TranskrypcjaId,
    StatusOceny Status,
    WariantRozwiazania Wariant,
    int PunktySurowe,
    int Punkty,
    int PunktyMax,
    int? PierwszyBlednyKrok,
    bool BrakujacyKrok,
    TypBledu? TypBledu,
    bool MozliwyBladOdczytu,
    int? KrokPodejrzanyOdczyt,
    PewnoscOceny PewnoscOceny,
    PewnoscSurowa? PewnoscSurowa,
    ProgPewnosciId ProgPewnosciId,
    KalibratorPewnosciId KalibratorPewnosciId,
    KonfiguracjaModeluId KonfiguracjaModeluId,
    string Uzasadnienie,
    Wskazowka? Wskazowka,
    IReadOnlyList<OcenaKryterium> Kryteria);
