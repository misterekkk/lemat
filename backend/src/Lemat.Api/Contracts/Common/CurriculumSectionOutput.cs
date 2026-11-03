// @Role: DTO Działu w odpowiedzi GET /dzialy (kontrakt POC 4.2) | @DependsOn: Lemat.Domain.Common.PoziomMatury | @UsedBy: EndpointyTresci | @Task: TASK-124
using Lemat.Domain.Common;

namespace Lemat.Api.Contracts.Common;

public sealed record DzialWyjscie(int Id, string Kod, string Nazwa, PoziomMatury PoziomMatury);
