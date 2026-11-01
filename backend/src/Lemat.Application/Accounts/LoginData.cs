// @Role: Dane konta potrzebne do weryfikacji hasła przy logowaniu (skrót hasła nie należy do agregatu Użytkownik) | @DependsOn: Lemat.Domain.Accounts | @UsedBy: IUzytkownikRepozytorium, uwierzytelnianie (TASK-072) | @Task: TASK-016
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public sealed record DaneLogowania(UzytkownikId UzytkownikId, string HasloHash, RolaUzytkownika Rola, StatusKonta Status);
