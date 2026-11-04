// @Role: Wejście rejestracji konta bez HTTP (skrót hasła argon2id liczy wywołujący; oświadczenie 16+ wymagane dla Ucznia) | @DependsOn: Lemat.Domain.Accounts.RolaUzytkownika | @UsedBy: IKontaFasada.ZarejestrujAsync, rejestracja HTTP (TASK-072), seed-dev (TASK-026) | @Task: TASK-016
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public sealed record RejestracjaKontaWejscie(
    RolaUzytkownika Rola,
    string Email,
    string HasloHash,
    string Imie,
    string Nazwisko,
    bool OswiadczenieWieku16,
    bool AutozatwierdzNauczyciela = false);
