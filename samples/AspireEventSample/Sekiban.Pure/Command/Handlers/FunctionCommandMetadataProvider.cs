using Sekiban.Pure.Events;
using Sekiban.Pure.Extensions;
namespace Sekiban.Pure.Command.Handlers;

public interface ICommandMetadataProvider
{
    CommandMetadata GetMetadata();
    CommandMetadata GetMetadataWithSubscribedEvent(IEvent ev);
}
public record FunctionCommandMetadataProvider(Func<string> GetExecutingUser) : ICommandMetadataProvider
{
    public CommandMetadata GetMetadata()
    {
        var commandId = GuidExtensions.CreateVersion7();
        return new CommandMetadata(commandId, "", commandId.ToString(), GetExecutingUser());
    }

    public CommandMetadata GetMetadataWithSubscribedEvent(IEvent ev) => new(
        GuidExtensions.CreateVersion7(),
        ev.Id.ToString(),
        ev.Metadata.CorrelationId,
        ev.Metadata.ExecutedUser);
}
public class CommandMetadataProvider(IExecutingUserProvider executingUserProvider) : ICommandMetadataProvider
{
    public CommandMetadata GetMetadata()
    {
        var commandId = GuidExtensions.CreateVersion7();
        return new CommandMetadata(commandId, "", commandId.ToString(), executingUserProvider.GetExecutingUser());
    }

    public CommandMetadata GetMetadataWithSubscribedEvent(IEvent ev) => new(
        GuidExtensions.CreateVersion7(),
        ev.Id.ToString(),
        ev.Metadata.CorrelationId,
        ev.Metadata.ExecutedUser);
}
public interface IExecutingUserProvider
{
    string GetExecutingUser();
}