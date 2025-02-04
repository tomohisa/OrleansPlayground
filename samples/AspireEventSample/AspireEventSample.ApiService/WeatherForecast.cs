using System.Text.Json.Serialization;
using AspireEventSample.ApiService.Aggregates.Branches;
using ResultBoxes;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Exception;

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchCreated>))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchNameChanged>))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartCreated>))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartItemAdded>))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartPaymentProcessed>))]
public partial class AspireEventSampleApiServiceEventsJsonContext : JsonSerializerContext
{
}