// @Role: Punkt kompozycji procesu worker (szkielet, /zdrowie na porcie 8081) | @DependsOn: Lemat.Worker.Configuration, Lemat.Application, Lemat.Infrastructure | @UsedBy: host ASP.NET Core | @Task: TASK-001, TASK-003
using Lemat.Worker.Configuration;

var builder = WorkerHost.CreateBuilder(args);
var app = builder.Build();
app.MapHealth();
await app.RunAsync();
