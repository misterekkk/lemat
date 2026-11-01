// @Role: Punkt kompozycji procesu api (Minimal API, zdrowie, OpenAPI) | @DependsOn: Lemat.Api.Configuration, Lemat.Application, Lemat.Infrastructure, Lemat.Agents, Lemat.Models | @UsedBy: host ASP.NET Core, WebApplicationFactory | @Task: TASK-001, TASK-003
using Lemat.Api.Configuration;

var builder = ApiHost.CreateBuilder(args);
var app = builder.Build();
app.MapEndpoints();
await app.RunAsync();

/// <summary>Znacznik punktu wejścia dla <c>WebApplicationFactory</c>.</summary>
public partial class Program;
