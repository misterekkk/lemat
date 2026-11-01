// @Role: Wyliczenie Typ pytania do Modelu decyzyjnego (typ PostgreSQL typ_pytania: NOUL, SCORE, CHOICE) | @DependsOn: — | @UsedBy: Pytanie, DecyzjaSkladowa, DecyzjaModelu | @Task: TASK-018
namespace Lemat.Domain.Models;

// SCORE jest nazwą typu pytania Jev (CLAUDE.md 4.C.3 — wyjątek od zakazu synonimu „score”).
public enum TypPytania
{
    Noul,
    Score,
    Choice,
}
