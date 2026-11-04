// @Role: Walidator LogowanieWejscie: obecność e-maila i hasła oraz górne limity długości (ochrona przed kosztownym haszowaniem), bez ujawniania istnienia konta | @DependsOn: FluentValidation, LogowanieWejscie | @UsedBy: AccountsEndpoints | @Task: TASK-123
using FluentValidation;
using Lemat.Api.Contracts.Accounts;

namespace Lemat.Api.Validation;

public sealed class LogowanieWejscieWalidator : AbstractValidator<LogowanieWejscie>
{
    public LogowanieWejscieWalidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(w => w.Email)
            .NotEmpty().WithErrorCode("WYMAGANE").WithMessage("Podaj adres e-mail.")
            .MaximumLength(254).WithErrorCode("ZA_DLUGIE").WithMessage("Adres e-mail może mieć co najwyżej 254 znaki.");

        RuleFor(w => w.Haslo)
            .NotEmpty().WithErrorCode("WYMAGANE").WithMessage("Podaj hasło.")
            .MaximumLength(128).WithErrorCode("ZA_DLUGIE").WithMessage("Hasło może mieć co najwyżej 128 znaków.");
    }
}
