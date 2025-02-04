namespace Sekiban.Pure.Command.Handlers;

public record CommandMetadata(Guid CommandId, string CausationId, string CorrelationId, string ExecutedUser)
{
    
}