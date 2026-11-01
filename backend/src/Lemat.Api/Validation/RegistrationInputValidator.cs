// @Role: Walidator RejestracjaWejscie: e-mail, hasło 10-128, imię/nazwisko, rola UCZEN (U-21), oświadczenie 16+ i zgoda na regulamin (kody reguł z kontraktu POC 1.1) | @DependsOn: FluentValidation, RejestracjaWejscie, Uzytkownik.TryNormalizujEmail | @UsedBy: AccountsEndpoints | @Task: TASK-123
using FluentValidation;
using Lemat.Api.Contracts.Accounts;
using Lemat.Domain.Accounts;

namespace Lemat.Api.Validation;

public sealed class RejestracjaWejscieWalidator : AbstractValidator<RejestracjaWejscie>
{
    public RejestracjaWejscieWalidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(w => w.Email)
            .NotEmpty().WithErrorCode("WYMAGANE").WithMessage("Podaj adres e-mail.")
            .MaximumLength(Uzytkownik.MaksDlugoscEmail).WithErrorCode("ZA_DLUGIE").WithMessage("Adres e-mail może mieć co najwyżej 254 znaki.")
            .Must(e => Uzytkownik.TryNormalizujEmail(e, out _)).WithErrorCode("FORMAT_EMAIL").WithMessage("Podaj poprawny adres e-mail.");

        RuleFor(w => w.Haslo)
            .NotEmpty().WithErrorCode("WYMAGANE").WithMessage("Podaj hasło.")
            .MinimumLength(10).WithErrorCode("ZA_KROTKIE").WithMessage("Hasło musi mieć co najmniej 10 znaków.")
            .MaximumLength(128).WithErrorCode("ZA_DLUGIE").WithMessage("Hasło może mieć co najwyżej 128 znaków.");

        RuleFor(w => w.Imie)
            .NotEmpty().WithErrorCode("WYMAGANE").WithMessage("Podaj imię.")
            .Must(i => i.Trim().Length <= Uzytkownik.MaksDlugoscImie).WithErrorCode("ZA_DLUGIE").WithMessage("Imię może mieć co najwyżej 60 znaków.");

        RuleFor(w => w.Nazwisko)
            .NotEmpty().WithErrorCode("WYMAGANE").WithMessage("Podaj nazwisko.")
            .Must(n => n.Trim().Length <= Uzytkownik.MaksDlugoscNazwisko).WithErrorCode("ZA_DLUGIE").WithMessage("Nazwisko może mieć co najwyżej 80 znaków.");

        RuleFor(w => w.Rola)
            .Must(r => r == RolaRejestracji.Uczen).WithErrorCode("ROLA_NIEOBSLUGIWANA").WithMessage("Rejestracja jest na razie dostępna tylko dla uczniów.");

        RuleFor(w => w.OswiadczenieWieku16)
            .Must(o => o).WithErrorCode("WYMAGANE_OSWIADCZENIE").WithMessage("Wymagane oświadczenie o ukończeniu 16 lat.");

        RuleFor(w => w.ZgodaRegulamin)
            .Must(o => o).WithErrorCode("WYMAGANE_OSWIADCZENIE").WithMessage("Wymagana akceptacja regulaminu.");
    }
}
