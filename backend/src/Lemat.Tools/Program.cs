// @Role: Narzędzie wiersza poleceń (dispatcher; polecenia migrate i seed-dev w kolejnych zadaniach) | @DependsOn: Lemat.Tools.Commands, Lemat.Application, Lemat.Infrastructure | @UsedBy: operator, scripts | @Task: TASK-001, TASK-003, TASK-013
using Lemat.Tools.Commands;

var dispatcher = new CommandDispatcher([new MigrujBazeCommand()]);
return await dispatcher.WykonajAsync(args, Console.Out, Console.Error);
