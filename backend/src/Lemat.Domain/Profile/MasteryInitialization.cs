// @Role: Źródło inicjalizacji Opanowania Umiejętności (DOMYSLNA, TEST_DIAGNOSTYCZNY, PROPAGACJA_Z_DZIALU; 10.1.3) | @DependsOn: — | @UsedBy: OpanowanieUmiejetnosci, konfiguracja EF | @Task: TASK-019
namespace Lemat.Domain.Profile;

public enum InicjalizacjaOpanowania
{
    Domyslna,
    TestDiagnostyczny,
    PropagacjaZDzialu,
}
