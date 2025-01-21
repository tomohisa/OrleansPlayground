using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Grains;

public record ProcessPayment(Guid UserId, string PaymentMethod) : ICommandWithHandlerAsync<ProcessPayment, ShoppingCartProjector>
{
    public PartitionKeys SpecifyPartitionKeys(ProcessPayment command) =>
        PartitionKeys<ShoppingCartProjector>.Generate();
    public Task<ResultBox<EventOrNone>> HandleAsync(
        ProcessPayment command,
        ICommandContext<IAggregatePayload> context) =>
        EventOrNone.Event(new PaymentProcessedShoppingCart(command.PaymentMethod)).ToTask();
}