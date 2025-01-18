using System.Text.Json.Serialization;
using ResultBoxes;

namespace AspireEventSample.ApiService.Grains;

[GenerateSerializer]
public record OrleansResultBox<TValue>(Exception? Exception,TValue? Value) where TValue : notnull
{
    [JsonIgnore] public bool IsSuccess => Exception is null && Value is not null;
    public Exception GetException() =>
        Exception ?? throw new ResultsInvalidOperationException("no exception");

    public TValue GetValue() =>
        (IsSuccess ? Value : throw new ResultsInvalidOperationException("no value")) ??
        throw new ResultsInvalidOperationException();
}