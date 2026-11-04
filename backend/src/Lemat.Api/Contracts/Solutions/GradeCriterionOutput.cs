// @Role: DTO punktów za Kryterium w Ocenie (KryteriumOcenyWyjscie, kontrakt POC 7.5) z opisem i punktami maksymalnymi z wersji Zadania | @DependsOn: — | @UsedBy: OcenaWyjscie, EndpointyRozwiazan | @Task: TASK-127
namespace Lemat.Api.Contracts.Solutions;

public sealed record KryteriumOcenyWyjscie(short NrKryterium, string Opis, decimal? Punkty, decimal PunktyMax, decimal? Prawdopodobienstwo);
