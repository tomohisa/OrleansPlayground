namespace Sekiban.Pure.Query;

public record QueryResult<T>(T Value) : IQueryResult;