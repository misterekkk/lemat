// @Role: Wejście POST /rozwiazania (multipart, [AsParameters], ADR-067): zadanie_wersja_id, pola opcjonalne ignorowane w POC (U-24) i pliki; odczyt pliku wyłącznie z pola zdjecia | @DependsOn: Microsoft.AspNetCore.Http.IFormFileCollection | @UsedBy: EndpointyRozwiazan | @Task: TASK-127
using Microsoft.AspNetCore.Mvc;

namespace Lemat.Api.Contracts.Solutions;

public sealed class RozwiazanieWejscie
{
    [FromForm(Name = "zadanie_wersja_id")]
    public Guid? ZadanieWersjaId { get; set; }

    /// <summary>Ignorowane w POC: serwer tworzy techniczny element Ścieżki (U-24).</summary>
    [FromForm(Name = "element_sciezki_id")]
    public Guid? ElementSciezkiId { get; set; }

    /// <summary>Ignorowane w POC (brak Klas).</summary>
    [FromForm(Name = "przypisanie_id")]
    public Guid? PrzypisanieId { get; set; }

    public IFormFileCollection Pliki { get; set; } = null!;
}
