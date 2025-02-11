namespace Sekiban.Pure.Query;

public record QueryResult<T>(T Value) : IQueryResult
{
    public object GetValue()
    {
        return Value;
    }

    public QueryResultGeneral ToGeneral(IQueryCommon query)
    {
        return new QueryResultGeneral(Value, typeof(T).Name, query);
    }
}

public record QueryResultGeneral(object Value, string ResultType, IQueryCommon Query) : IQueryResult
{
    public object GetValue()
    {
        return Value;
    }

    public QueryResultGeneral ToGeneral(IQueryCommon queryCommon)
    {
        return this with { Query = queryCommon };
    }
}