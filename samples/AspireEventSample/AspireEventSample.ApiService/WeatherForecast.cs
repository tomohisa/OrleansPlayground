using AspireEventSample.ApiService.Aggregates.Branches;
using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class AspireEventSampleApiServiceCommandTypes : ICommandTypes
{
    public Task<ResultBox<CommandResponse>> ExecuteGeneral(
        CommandExecutor executor,
        ICommandWithHandlerSerializable command,
        PartitionKeys partitionKeys,
        CommandMetadata commandMetadata,
        Func<PartitionKeys, IAggregateProjector, Task<ResultBox<Aggregate>>> loader,
        Func<string, List<IEvent>, Task<ResultBox<List<IEvent>>>> saver) =>
        command switch
        {
            RegisterBranch registerBranch => executor
                .ExecuteGeneralWithPartitionKeys<RegisterBranch, NoInjection, IAggregatePayload>(
                    registerBranch,
                    command.GetProjector(),
                    partitionKeys,
                    NoInjection.Empty,
                    registerBranch.Handle,
                    commandMetadata,
                    loader,
                    saver),
            _ => Task.FromResult(ResultBox<CommandResponse>.Error(new ApplicationException("Unknown command type")))
        };
}