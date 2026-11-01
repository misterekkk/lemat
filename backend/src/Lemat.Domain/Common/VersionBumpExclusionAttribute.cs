// @Role: Oznacza właściwość, której zmiana sama nie podnosi version korzenia agregatu (ADR-029, np. theta Opanowania umiejętności) | @DependsOn: — | @UsedBy: Lemat.Infrastructure.Persistence.Interceptors.WersjaAgregatuInterceptor, encje modułu Profil | @Task: TASK-015
namespace Lemat.Domain.Common;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class BezZmianyWersjiAttribute : Attribute
{
}
