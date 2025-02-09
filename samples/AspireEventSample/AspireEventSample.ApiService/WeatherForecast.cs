using System.Text.Json;
using System.Text.Json.Serialization;
using AspireEventSample.ApiService.Aggregates.Carts;
using AspireEventSample.ApiService.Projections;
using ResultBoxes;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Exceptions;
using Sekiban.Pure.Extensions;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Query;

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EventDocumentCommon))]
[JsonSerializable(typeof(EventDocumentCommon[]))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchCreated>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Branches.BranchCreated))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchNameChanged>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Branches.BranchNameChanged))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartCreated>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartCreated))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartItemAdded>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartItemAdded))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartPaymentProcessed>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartPaymentProcessed))]
public partial class AspireEventSampleApiServiceEventsJsonContext : JsonSerializerContext
{
}
