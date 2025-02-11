using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.Carts;
using ResultBoxes;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Exceptions;
using Sekiban.Pure.Extensions;
using Sekiban.Pure.Serialize;
using System.Text.Json;
internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class AspireEventSampleApiServiceEventTypes : IEventTypes
{
    public ResultBox<IEvent> GenerateTypedEvent(
        IEventPayload payload,
        PartitionKeys partitionKeys,
        string sortableUniqueId,
        int version,
        EventMetadata metadata) => payload switch
    {
        BranchCreated branchcreated => new Event<BranchCreated>(
            GuidExtensions.CreateVersion7(),
            branchcreated,
            partitionKeys,
            sortableUniqueId,
            version,
            metadata),
        BranchNameChanged branchnamechanged => new Event<BranchNameChanged>(
            GuidExtensions.CreateVersion7(),
            branchnamechanged,
            partitionKeys,
            sortableUniqueId,
            version,
            metadata),
        ShoppingCartCreated shoppingcartcreated => new Event<ShoppingCartCreated>(
            GuidExtensions.CreateVersion7(),
            shoppingcartcreated,
            partitionKeys,
            sortableUniqueId,
            version,
            metadata),
        ShoppingCartItemAdded shoppingcartitemadded => new Event<ShoppingCartItemAdded>(
            GuidExtensions.CreateVersion7(),
            shoppingcartitemadded,
            partitionKeys,
            sortableUniqueId,
            version,
            metadata),
        ShoppingCartPaymentProcessed shoppingcartpaymentprocessed => new Event<ShoppingCartPaymentProcessed>(
            GuidExtensions.CreateVersion7(),
            shoppingcartpaymentprocessed,
            partitionKeys,
            sortableUniqueId,
            version,
            metadata),
        _ => ResultBox<IEvent>.FromException(
            new SekibanEventTypeNotFoundException($"Event Type {payload.GetType().Name} Not Found"))
    };

    public ResultBox<IEventDocument> ConvertToEventDocument(
        IEvent ev) => ev switch
    {
        Event<BranchCreated> BranchCreatedEvent => EventDocument<BranchCreated>.FromEvent(
            BranchCreatedEvent),
        Event<BranchNameChanged> BranchNameChangedEvent => EventDocument<BranchNameChanged>.FromEvent(
            BranchNameChangedEvent),
        Event<ShoppingCartCreated> ShoppingCartCreatedEvent => EventDocument<ShoppingCartCreated>.FromEvent(
            ShoppingCartCreatedEvent),
        Event<ShoppingCartItemAdded> ShoppingCartItemAddedEvent => EventDocument<ShoppingCartItemAdded>.FromEvent(
            ShoppingCartItemAddedEvent),
        Event<ShoppingCartPaymentProcessed> ShoppingCartPaymentProcessedEvent =>
            EventDocument<ShoppingCartPaymentProcessed>.FromEvent(
                ShoppingCartPaymentProcessedEvent),
        _ => ResultBox<IEventDocument>.FromException(
            new SekibanEventTypeNotFoundException($"Event Type {ev.GetPayload().GetType().Name} Not Found"))
    };
    public ResultBox<IEvent> DeserializeToTyped(
        EventDocumentCommon common,
        JsonSerializerOptions serializeOptions) => common.PayloadTypeName switch
    {
        nameof(BranchCreated) => common.ToEvent<BranchCreated>(serializeOptions),
        nameof(BranchNameChanged) => common.ToEvent<BranchNameChanged>(serializeOptions),
        nameof(ShoppingCartCreated) => common.ToEvent<ShoppingCartCreated>(serializeOptions),
        nameof(ShoppingCartItemAdded) => common.ToEvent<ShoppingCartItemAdded>(serializeOptions),
        nameof(ShoppingCartPaymentProcessed) => common.ToEvent<ShoppingCartPaymentProcessed>(serializeOptions),
        _ => ResultBox<IEvent>.FromException(
            new SekibanEventTypeNotFoundException($"Event Type {common.PayloadTypeName} Not Found"))
    };
    public ResultBox<string> SerializePayloadToJson(ISekibanSerializer serializer, IEvent ev) =>
        ev.GetPayload() switch
        {
            BranchCreated branchcreated =>
                ResultBox.CheckNullWrapTry(() => serializer.Serialize(branchcreated)),
            BranchNameChanged branchnamechanged =>
                ResultBox.CheckNullWrapTry(() => serializer.Serialize(branchnamechanged)),
            ShoppingCartCreated shoppingcartcreated =>
                ResultBox.CheckNullWrapTry(() => serializer.Serialize(shoppingcartcreated)),
            ShoppingCartItemAdded shoppingcartitemadded =>
                ResultBox.CheckNullWrapTry(() => serializer.Serialize(shoppingcartitemadded)),
            ShoppingCartPaymentProcessed shoppingcartpaymentprocessed
                => ResultBox.CheckNullWrapTry(() => serializer.Serialize(shoppingcartpaymentprocessed)),
            _ => ResultBox<string>.FromException(
                new SekibanEventTypeNotFoundException($"Event Type {ev.GetPayload().GetType().Name} Not Found"))
        };

    public void CheckEventJsonContextOption(JsonSerializerOptions options)
    {
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(EventDocumentCommon), options) == null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"EventDocumentCommon not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocumentCommon))] ");
        }
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(EventDocumentCommon[]), options) == null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"EventDocumentCommon[] not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocumentCommon[]))] ");
        }
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(EventDocument<BranchCreated>), options) == null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchCreated> not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchCreated>))]");
        }
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(BranchCreated), options) == null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"AspireEventSample.ApiService.Aggregates.Branches.BranchCreated not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchCreated>))]");
        }
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(EventDocument<BranchNameChanged>), options) == null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchNameChanged> not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchNameChanged>))]");
        }
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(BranchNameChanged), options) == null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"AspireEventSample.ApiService.Aggregates.Branches.BranchNameChanged not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchNameChanged>))]");
        }
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(EventDocument<ShoppingCartCreated>), options) == null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartCreated> not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartCreated>))]");
        }
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(ShoppingCartCreated), options) == null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartCreated not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartCreated>))]");
        }
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(EventDocument<ShoppingCartItemAdded>), options) == null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartItemAdded> not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartItemAdded>))]");
        }
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(ShoppingCartItemAdded), options) == null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartItemAdded not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartItemAdded>))]");
        }
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(EventDocument<ShoppingCartPaymentProcessed>), options) ==
            null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartPaymentProcessed> not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartPaymentProcessed>))]");
        }
        if (options?.TypeInfoResolver?.GetTypeInfo(typeof(ShoppingCartPaymentProcessed), options) == null)
        {
            throw new SekibanEventTypeNotFoundException(
                $"AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartPaymentProcessed not found in {options?.TypeInfoResolver?.GetType().Name ?? string.Empty}, put attribute [JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartPaymentProcessed>))]");
        }
    }
}