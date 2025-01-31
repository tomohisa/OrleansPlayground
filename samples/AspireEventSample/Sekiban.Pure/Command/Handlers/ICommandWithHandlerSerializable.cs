using ResultBoxes;
using Sekiban.Pure.Events;
using Sekiban.Pure.Extensions;

namespace Sekiban.Pure.Command.Handlers;

public interface ICommandWithHandlerSerializable: ICommandGetProjector,ICommand
{
    public Delegate GetHandler();
    public Delegate GetPartitionKeysSpecifier();
    public OptionalValue<Type> GetAggregatePayloadType();
}

public record CommandMetadata(Guid CommandId, string CausationId, string CorrelationId, string ExecutedUser)
{
    
}
public interface ICommandGetExecutingUser
{
    public string GetExecutingUser();
}
public record CommandMetadataProvider(Func<string> GetExecutingUser)
{
    public CommandMetadata GetMetadata()
    {
        var commandId = GuidExtensions.CreateVersion7();
        return new CommandMetadata(commandId, "", commandId.ToString(), GetExecutingUser());
    }

    public CommandMetadata GetMetadataWithSubscribedEvent(IEvent ev)
    {
        return new CommandMetadata(GuidExtensions.CreateVersion7(), ev.Id.ToString(), ev.Metadata.CorrelationId, ev.Metadata.ExecutedUser);
    }
}