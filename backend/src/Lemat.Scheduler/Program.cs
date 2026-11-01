// @Role: Punkt kompozycji procesu scheduler (szkielet, POST-MVP) | @DependsOn: Lemat.Application, Lemat.Infrastructure | @UsedBy: host Generic Host | @Task: TASK-001
var builder = Host.CreateApplicationBuilder(args);
using var host = builder.Build();
await host.RunAsync();
