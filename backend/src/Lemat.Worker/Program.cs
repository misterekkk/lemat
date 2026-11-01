// @Role: Punkt kompozycji procesu worker (szkielet, bez pul prac w tle) | @DependsOn: Lemat.Application, Lemat.Infrastructure, Lemat.Agents, Lemat.Models | @UsedBy: host Generic Host | @Task: TASK-001
var builder = Host.CreateApplicationBuilder(args);
using var host = builder.Build();
await host.RunAsync();
