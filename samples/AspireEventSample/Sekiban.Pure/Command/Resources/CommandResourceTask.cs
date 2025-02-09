using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
namespace Sekiban.Pure.Command.Handlers;

public record CommandResourceTask<TCommand, TProjector>(
    Func<TCommand, PartitionKeys> SpecifyPartitionKeys,
    Func<TCommand, ICommandContext<IAggregatePayload>, Task<ResultBox<EventOrNone>>> Handler)
    : ICommandResource<TCommand> where TCommand : ICommand, IEquatable<TCommand>
    where TProjector : IAggregateProjector, new()
{
    public Func<TCommand, PartitionKeys> GetSpecifyPartitionKeysFunc() => SpecifyPartitionKeys;
    public Type GetCommandType() => typeof(TCommand);
    public IAggregateProjector GetProjector() => new TProjector();
    public object GetInjection() => NoInjection.Empty;
    public Delegate GetHandler() => Handler;
    public OptionalValue<Type> GetAggregatePayloadType() => OptionalValue<Type>.Empty;
}
public record CommandResourceTask<TCommand, TProjector, TAggregatePayload>(
    Func<TCommand, PartitionKeys> SpecifyPartitionKeys,
    Func<TCommand, ICommandContext<TAggregatePayload>, Task<ResultBox<EventOrNone>>> Handler)
    : ICommandResource<TCommand> where TCommand : ICommand, IEquatable<TCommand>
    where TAggregatePayload : IAggregatePayload
    where TProjector : IAggregateProjector, new()
{
    public Func<TCommand, PartitionKeys> GetSpecifyPartitionKeysFunc() => SpecifyPartitionKeys;
    public Type GetCommandType() => typeof(TCommand);
    public IAggregateProjector GetProjector() => new TProjector();
    public object? GetInjection() => NoInjection.Empty;
    public Delegate GetHandler() => Handler;
    public OptionalValue<Type> GetAggregatePayloadType() => typeof(TAggregatePayload);
}
