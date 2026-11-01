// @Role: Narzędzie wiersza poleceń (dispatcher; polecenia migrate i seed-poc; seed-dev w kolejnych zadaniach) | @DependsOn: Lemat.Tools.Commands, Lemat.Application, Lemat.Infrastructure | @UsedBy: operator, scripts | @Task: TASK-001, TASK-003, TASK-013, TASK-122
using Lemat.Tools.Commands;

var dispatcher = new CommandDispatcher([new MigrujBazeCommand(), new SeedPocCommand()]);
return await dispatcher.WykonajAsync(args, Console.Out, Console.Error);
