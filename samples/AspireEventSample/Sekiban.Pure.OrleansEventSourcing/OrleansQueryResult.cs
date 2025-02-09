namespace Sekiban.Pure.OrleansEventSourcing;

[GenerateSerializer]
public record OrleansQueryResult<T>([property: Id(0)] T Result, string QueryName) : IOrleansQueryResult
{
}