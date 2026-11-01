// @Role: Zdarzenie domenowe KlasaPrzekazana: Przekazanie Klasy innemu Nauczycielowi (ADR-044) | @DependsOn: Lemat.Domain.Common.IZdarzenieDomenowe, KlasaId, NauczycielId | @UsedBy: Lemat.Domain.Classes.Klasa, obsługi zdarzeń (Audyt, Weryfikacje) | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Classes;

public sealed record KlasaPrzekazana(KlasaId KlasaId, NauczycielId? Poprzedni, NauczycielId Nowy, DateTimeOffset Wystapilo) : IZdarzenieDomenowe;
