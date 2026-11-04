// @Role: Identyfikator silnie typowany Tokenu odświeżania (token_odswiezania.id) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Accounts.TokenOdswiezania, repozytorium tokenów | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public readonly record struct TokenOdswiezaniaId(Guid Wartosc) : IIdentyfikator<Guid>;
