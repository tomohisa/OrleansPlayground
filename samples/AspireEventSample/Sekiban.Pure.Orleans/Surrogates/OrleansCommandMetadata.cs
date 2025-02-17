using Sekiban.Pure.Command.Handlers;
namespace Sekiban.Pure.Orleans.Surrogates;

[GenerateSerializer]
public record OrleansCommandMetadata([property:Id(0)]Guid CommandId, [property:Id(1)]string CausationId,
    [property:Id(2)]string CorrelationId, [property:Id(3)]string ExecutedUser)
{
}