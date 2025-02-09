using ResultBoxes;
using Sekiban.Core.Shared;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Exceptions;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Repositories;
using Sekiban.Pure.Validations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
namespace Sekiban.Pure.Command.Executor;

public class CommandExecutor : ICommandExecutor
{
    public IEventTypes EventTypes { get; init; } = new EmptyEventTypes();

    public Task<ResultBox<CommandResponse>> ExecuteWithResource<TCommand>(
        TCommand command,
        ICommandResource<TCommand> resource, CommandMetadata metadata) where TCommand : ICommand, IEquatable<TCommand> =>
        ExecuteGeneralNonGeneric(
            command,
            resource.GetProjector(),
            resource.GetSpecifyPartitionKeysFunc(),
            resource.GetInjection(),
            resource.GetHandler(),
            resource.GetAggregatePayloadType(), metadata);

    #region Private Methods
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(CommandExecutor))]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "<Pending>")]
    public async Task<ResultBox<CommandResponse>> ExecuteGeneralNonGeneric(
        ICommand command,
        IAggregateProjector projector,
        Delegate specifyPartitionKeys,
        object? inject,
        Delegate handler,
        OptionalValue<Type> aggregatePayloadType, CommandMetadata metadata)
    {
        var commandType = command.GetType();
        var injectType = inject is not null ? inject.GetType() : typeof(NoInjection);
        var optionalType = typeof(OptionalValue<>).MakeGenericType(injectType);
        var optionalMethod = optionalType?.GetMethod(
            nameof(OptionalValue.FromValue),
            BindingFlags.Static | BindingFlags.Public);
        var injectValue = inject is not null
            ? optionalMethod?.Invoke(null, new[] { inject })
            : OptionalValue<NoInjection>.Empty;
        var aggregatePayloadTypeValue = aggregatePayloadType.HasValue
            ? aggregatePayloadType.GetValue()
            : typeof(IAggregatePayload);
        var method = GetType().GetMethod(nameof(ExecuteGeneral), BindingFlags.Public | BindingFlags.Instance);
        if (method is null) { return new SekibanCommandHandlerNotMatchException("Method not found"); }
        var genericMethod = method.MakeGenericMethod(commandType, injectType, aggregatePayloadTypeValue);
        var result = genericMethod.Invoke(
            this,
            new[] { command, projector, specifyPartitionKeys, injectValue, handler, metadata });
        if (result is Task<ResultBox<CommandResponse>> task) { return await task; }
        return new SekibanCommandHandlerNotMatchException("Result is not Task<ResultBox<CommandResponse>>");
    }
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(CommandExecutor))]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "<Pending>")]
    public async Task<ResultBox<CommandResponse>> ExecuteGeneralNonGeneric(
        ICommand command,
        IAggregateProjector projector,
        PartitionKeys partitionKeys,
        object? inject,
        Delegate handler,
        OptionalValue<Type> aggregatePayloadType,
        CommandMetadata commandMetadata,
        Func<PartitionKeys, IAggregateProjector,Task<ResultBox<Aggregate>>> loader, Func<string, List<IEvent>,Task<ResultBox<List<IEvent>>>> saver)
    {
        var commandType = command.GetType();
        var injectType = inject is not null ? inject.GetType() : typeof(NoInjection);
        var optionalType = typeof(OptionalValue<>).MakeGenericType(injectType);
        var optionalMethod = optionalType?.GetMethod(
            nameof(OptionalValue.FromValue),
            BindingFlags.Static | BindingFlags.Public);
        var injectValue = inject is not null
            ? optionalMethod?.Invoke(null, new[] { inject })
            : OptionalValue<NoInjection>.Empty;
        var aggregatePayloadTypeValue = aggregatePayloadType.HasValue
            ? aggregatePayloadType.GetValue()
            : typeof(IAggregatePayload);
        var method = GetType()
            .GetMethod(nameof(ExecuteGeneralWithPartitionKeys), BindingFlags.Public | BindingFlags.Instance);
        if (method is null) { return new SekibanCommandHandlerNotMatchException("Method not found"); }
        var genericMethod = method.MakeGenericMethod(commandType, injectType, aggregatePayloadTypeValue);
        var result = genericMethod.Invoke(this, new[] { command, projector, partitionKeys, injectValue, handler, commandMetadata, loader, saver });
        if (result is Task<ResultBox<CommandResponse>> task) { return await task; }
        return new SekibanCommandHandlerNotMatchException("Result is not Task<ResultBox<CommandResponse>>");
    }


    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(CommandExecutor))]
    public Task<ResultBox<CommandResponse>> ExecuteGeneral<TCommand, TInject, TAggregatePayload>(
        TCommand command,
        IAggregateProjector projector,
        Func<TCommand, PartitionKeys> specifyPartitionKeys,
        OptionalValue<TInject> inject,
        Delegate handler, CommandMetadata commandMetadata) where TCommand : ICommand where TAggregatePayload : IAggregatePayload =>
        specifyPartitionKeys(command)
            .ToResultBox()
            .Conveyor(
                partitionKeys => ExecuteGeneralWithPartitionKeys<TCommand, TInject, TAggregatePayload>(
                    command,
                    projector,
                    partitionKeys,
                    inject,
                    handler, commandMetadata, (pk, pj) => Repository.Load(pk, pj).ToTask(), (_, events) => Repository.Save(events).ToTask()));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(CommandExecutor))]
    public Task<ResultBox<CommandResponse>> ExecuteGeneralWithPartitionKeys<TCommand, TInject, TAggregatePayload>(
        TCommand command,
        IAggregateProjector projector,
        PartitionKeys partitionKeys,
        OptionalValue<TInject> inject,
        Delegate handler,
        CommandMetadata commandMetadata,
        Func<PartitionKeys, IAggregateProjector,Task<ResultBox<Aggregate>>> loader, Func<string, List<IEvent>,Task<ResultBox<List<IEvent>>>> saver) where TCommand : ICommand where TAggregatePayload : IAggregatePayload =>
        ResultBox
            .Start
            .Conveyor(_ => command.ValidateProperties().ToList().ToResultBox()).Verify(errors => errors.Count == 0 ? ExceptionOrNone.None : new SekibanValidationErrorsException(errors))
            .Conveyor(
                _ => CreateCommandContextWithoutState<TCommand, TAggregatePayload, TInject>(
                    command,
                    partitionKeys,
                    projector,
                    handler, loader, commandMetadata))
            .Combine(
                context => RunHandler<TCommand, TInject, TAggregatePayload>(command, context, inject, handler)
                    .Conveyor(eventOrNone => EventToCommandExecuted(context, eventOrNone)))
            .Conveyor(values => saver(values.Value1.OriginalSortableUniqueId, values.Value2.ProducedEvents).Remap(savedEvent =>TwoValues.FromValues(values.Value1, savedEvent)))
            .Conveyor(
                (context, savedEvents) => ResultBox.FromValue(
                    new CommandResponse(
                        partitionKeys,
                        savedEvents,
                        savedEvents.Count > 0
                            ? savedEvents.Last().Version
                            : context.GetCurrentVersion())));


    private Task<ResultBox<ICommandContextWithoutState>>
        CreateCommandContextWithoutState<TCommand, TAggregatePayload, TInject>(
            TCommand command,
            PartitionKeys partitionKeys,
            IAggregateProjector projector,
            Delegate handler,
            Func<PartitionKeys, IAggregateProjector,Task<ResultBox<Aggregate>>> loader,
            CommandMetadata commandMetadata
            ) where TCommand : ICommand where TAggregatePayload : IAggregatePayload =>
        handler switch
        {
            Func<TCommand, ICommandContextWithoutState, ResultBox<EventOrNone>> handler1 =>
                ResultBox<ICommandContextWithoutState>.FromValue(
                    new CommandContextWithoutState(partitionKeys, EventTypes, commandMetadata)).ToTask(),
            Func<TCommand, ICommandContextWithoutState, Task<ResultBox<EventOrNone>>> handler1 =>
                ResultBox<ICommandContextWithoutState>.FromValue(
                    new CommandContextWithoutState(partitionKeys, EventTypes, commandMetadata)).ToTask(),
            Func<TCommand, TInject, ICommandContextWithoutState, ResultBox<EventOrNone>> handler1 =>
                ResultBox<ICommandContextWithoutState>.FromValue(
                    new CommandContextWithoutState(partitionKeys, EventTypes, commandMetadata)).ToTask(),
            Func<TCommand, TInject, ICommandContextWithoutState, Task<ResultBox<EventOrNone>>> handler1 =>
                ResultBox<ICommandContextWithoutState>.FromValue(
                    new CommandContextWithoutState(partitionKeys, EventTypes, commandMetadata)).ToTask(),
            _ => ResultBox
                .Start
                .Conveyor(keys => loader(partitionKeys, projector))
                .Verify(aggregate => VerifyAggregateType<TCommand, TAggregatePayload>(command, aggregate))
                .Conveyor(
                    aggregate => ResultBox<ICommandContextWithoutState>.FromValue(
                        new CommandContext<TAggregatePayload>(aggregate, projector, EventTypes, commandMetadata)))
        };

    private ExceptionOrNone VerifyAggregateType<TCommand, TAggregatePayload>(TCommand command, Aggregate aggregate)
        where TCommand : ICommand where TAggregatePayload : IAggregatePayload =>
        aggregate.GetPayload() is not TAggregatePayload
            ? ExceptionOrNone.FromException(
                new SekibanAggregateTypeRestrictionException(
                    $"To execute command {command.GetType().Name}, " +
                    $"Aggregate must be {aggregate.GetPayload().GetType().Name}," +
                    $" but currently aggregate type is {aggregate.GetPayload().GetType().Name}"))
            : ExceptionOrNone.None;

    private Task<ResultBox<EventOrNone>> RunHandler<TCommand, TInject, TAggregatePayload>(
        TCommand command,
        ICommandContextWithoutState context,
        OptionalValue<TInject> inject,
        Delegate handler) where TCommand : ICommand where TAggregatePayload : IAggregatePayload =>
        (handler, inject.HasValue, context) switch
        {
            (Func<TCommand, ICommandContextWithoutState, ResultBox<EventOrNone>> handler1, _, { } contextWithout) =>
                Task.FromResult(handler1(command, contextWithout)),
            (Func<TCommand, ICommandContextWithoutState, Task<ResultBox<EventOrNone>>> handler1, _, { } contextWithout)
                => handler1(command, contextWithout),
            (Func<TCommand, TInject, ICommandContextWithoutState, ResultBox<EventOrNone>> handler1, true, {
            } contextWithout) => Task.FromResult(handler1(command, inject.GetValue(), contextWithout)),
            (Func<TCommand, TInject, ICommandContextWithoutState, Task<ResultBox<EventOrNone>>> handler1, true, {
            } contextWithout) => handler1(command, inject.GetValue(), contextWithout),
            (Func<TCommand, ICommandContext<TAggregatePayload>, ResultBox<EventOrNone>> handler1, _,
                ICommandContext<TAggregatePayload> stateContext) => handler1(command, stateContext).ToTask(),
            (Func<TCommand, TInject, ICommandContext<TAggregatePayload>, ResultBox<EventOrNone>> handler1, true,
                ICommandContext<TAggregatePayload> stateContext) => handler1(command, inject.GetValue(), stateContext)
                    .ToTask(),
            (Func<TCommand, ICommandContext<TAggregatePayload>, Task<ResultBox<EventOrNone>>> handler1, _,
                ICommandContext<TAggregatePayload> stateContext) => handler1(command, stateContext),
            (Func<TCommand, TInject, ICommandContext<TAggregatePayload>, Task<ResultBox<EventOrNone>>> handler1, true,
                ICommandContext<TAggregatePayload> stateContext) => handler1(command, inject.GetValue(), stateContext),
            _ => ResultBox<EventOrNone>
                .FromException(
                    new SekibanCommandHandlerNotMatchException(
                        $"{handler.GetType().Name} does not match as command handler"))
                .ToTask()
        };
    private ResultBox<CommandExecuted> EventToCommandExecuted(
        ICommandContextWithoutState commandContext,
        EventOrNone eventOrNone) =>
        (eventOrNone.HasEvent
            ? EventTypes
                .GenerateTypedEvent(
                    eventOrNone.GetValue(),
                    commandContext.GetPartitionKeys(),
                    SortableUniqueIdValue.Generate(SekibanDateProducer.GetRegistered().UtcNow, Guid.NewGuid()),
                    commandContext.GetNextVersion(), commandContext.EventMetadata)
                .Remap(ev => commandContext.Events.Append(ev).ToList())
            : ResultBox.FromValue(commandContext.Events)).Remap(commandContext.GetCommandExecuted);
    #endregion
}
