// @Role: Punkt kompozycji procesu api (szkielet, bez endpointów) | @DependsOn: Lemat.Application, Lemat.Infrastructure, Lemat.Agents, Lemat.Models | @UsedBy: host ASP.NET Core | @Task: TASK-001
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
await app.RunAsync();
