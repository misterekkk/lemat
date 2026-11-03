// @Role: Widok Ucznia zwracany przez PobierzUczniaHandler i ZmienUczniaHandler (dane konta i profilu nauki bez pól wewnętrznych) | @DependsOn: Lemat.Domain.Accounts, Lemat.Domain.Common.PoziomMatury | @UsedBy: PobierzUczniaHandler, ZmienUczniaHandler, Lemat.Api (EndpointyUcznia) | @Task: TASK-124
using Lemat.Domain.Common;

namespace Lemat.Application.Accounts;

public sealed record UczenWidok(
    Guid Id,
    string Email,
    string Imie,
    string Nazwisko,
    PoziomMatury? PoziomMatury,
    DateOnly? TerminEgzaminu,
    int? WybranyDzialId,
    bool ZgodaNaTrening,
    int Version);
