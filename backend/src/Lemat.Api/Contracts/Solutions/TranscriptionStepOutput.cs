// @Role: DTO kroku Transkrypcji (KrokTranskrypcjiWyjscie, kontrakt POC 7.5); pola Filtra danych osobowych i niepewnych fragmentów stałe w POC | @DependsOn: — | @UsedBy: TranskrypcjaWyjscie, EndpointyRozwiazan | @Task: TASK-127
namespace Lemat.Api.Contracts.Solutions;

/// <summary>W POC <c>niepewne_fragmenty</c> i <c>zastapienia_danych_osobowych</c> są puste, a flagi <c>false</c> (U-26).</summary>
public sealed record KrokTranskrypcjiWyjscie(
    short Nr,
    string Tekst,
    string Latex,
    decimal? PewnoscOdczytu,
    IReadOnlyList<object> NiepewneFragmenty,
    bool ZmienionyPrzezUcznia,
    short? NrKrokuAgenta,
    bool WymagaSprawdzenia,
    bool PodejrzenieDanychOsobowych,
    bool TylkoDaneOsobowe,
    IReadOnlyList<object> ZastapieniaDanychOsobowych);
