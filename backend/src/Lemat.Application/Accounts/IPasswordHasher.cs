// @Role: Port haszowania haseł (argon2id): skrót hasła i jego weryfikacja; implementacja w Infrastructure | @DependsOn: — | @UsedBy: PocSeeder (seed-poc), uwierzytelnianie (planowane TASK-123) | @Task: TASK-122
namespace Lemat.Application.Accounts;

/// <summary>Port haszowania haseł (ADR-020). Skrót jest samoopisujący (parametry i sól w ciągu), więc weryfikacja nie zależy od bieżącej konfiguracji.</summary>
public interface IHasherHasel
{
    /// <summary>Zwraca skrót argon2id hasła (losowa sól za każdym razem).</summary>
    string Haszuj(string haslo);

    /// <summary>Sprawdza hasło względem skrótu; niepoprawny lub uszkodzony skrót daje <c>false</c>.</summary>
    bool Weryfikuj(string haslo, string skrot);
}
