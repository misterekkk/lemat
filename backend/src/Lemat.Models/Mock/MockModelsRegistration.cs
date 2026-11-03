// @Role: Rejestracja w DI Mocków modeli POC (Agent rozpoznający i Agent oceniający) wybierana konfiguracją Lemat:Modele:Dostawca=mock; inny dostawca w POC nie jest obsługiwany (realne adaptery: TASK-054 …) | @DependsOn: LematOpcje, IAgentRozpoznajacy, IAgentOceniajacy, MockDane | @UsedBy: Lemat.Worker (WorkerHost) | @Task: TASK-128
using Lemat.Application.Common;
using Lemat.Application.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Models.Mock;

public static class MockModelsRegistration
{
    public static IServiceCollection DodajModeleMock(this IServiceCollection uslugi, LematOpcje opcje)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        ArgumentNullException.ThrowIfNull(opcje);
        if (!string.Equals(opcje.Modele.Dostawca, ModeleOpcje.Mock, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Dostawca modeli '{opcje.Modele.Dostawca}' nie jest obsługiwany w POC; dostępny jest wyłącznie '{ModeleOpcje.Mock}' (U-26).");
        }

        uslugi.AddSingleton(_ => MockDane.Wczytaj());
        uslugi.AddSingleton<IAgentRozpoznajacy, MockAgentRozpoznajacy>();
        uslugi.AddSingleton<IAgentOceniajacy, MockAgentOceniajacy>();
        return uslugi;
    }
}
