// @Role: Walidator ZatwierdzenieRozwiazaniaWejscie: wymagane transkrypcja_id (niepuste UUID) i wersja ≥ 1 | @DependsOn: FluentValidation, ZatwierdzenieRozwiazaniaWejscie | @UsedBy: EndpointyRozwiazan | @Task: TASK-128
using FluentValidation;
using Lemat.Api.Contracts.Solutions;

namespace Lemat.Api.Validation;

public sealed class ZatwierdzenieRozwiazaniaWejscieWalidator : AbstractValidator<ZatwierdzenieRozwiazaniaWejscie>
{
    public ZatwierdzenieRozwiazaniaWejscieWalidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(w => w.TranskrypcjaId)
            .NotNull().WithErrorCode("WYMAGANE").WithMessage("Podaj identyfikator transkrypcji.")
            .NotEqual(Guid.Empty).WithErrorCode("WYMAGANE").WithMessage("Podaj identyfikator transkrypcji.");

        RuleFor(w => w.Wersja)
            .NotNull().WithErrorCode("WYMAGANE").WithMessage("Podaj wersję transkrypcji.")
            .GreaterThanOrEqualTo(1).WithErrorCode("TYP_NIEPRAWIDLOWY").WithMessage("Wersja transkrypcji musi być liczbą od 1.");
    }
}
