// @Role: Punkt kompozycji procesu scheduler (szkielet, /zdrowie na porcie 8082) | @DependsOn: Lemat.Scheduler.Configuration, Lemat.Application, Lemat.Infrastructure | @UsedBy: host ASP.NET Core | @Task: TASK-001, TASK-003
using Lemat.Scheduler.Configuration;

var builder = SchedulerHost.CreateBuilder(args);
var app = builder.Build();
app.MapHealth();
await app.RunAsync();
