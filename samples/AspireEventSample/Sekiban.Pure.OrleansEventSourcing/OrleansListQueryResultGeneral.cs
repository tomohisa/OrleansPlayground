using Sekiban.Pure.Query;
namespace Sekiban.Pure.OrleansEventSourcing;

[GenerateSerializer]
public record OrleansListQueryResultGeneral(
    [property: Id(0)] int? TotalCount,
    [property: Id(1)] int? TotalPages,
    [property: Id(2)] int? CurrentPage,
    [property: Id(3)] int? PageSize,
    [property: Id(4)] IEnumerable<object> Items,
    [property: Id(5)] string RecordType,
    [property: Id(6)] IListQueryCommon Query)
{
    public static OrleansListQueryResultGeneral FromListQueryResultGeneral(ListQueryResultGeneral queryResultGeneral)
    {
        return new OrleansListQueryResultGeneral(queryResultGeneral.TotalCount, queryResultGeneral.TotalPages,
            queryResultGeneral.CurrentPage, queryResultGeneral.PageSize, queryResultGeneral.Items,
            queryResultGeneral.RecordType, queryResultGeneral.Query);
    }

    public ListQueryResultGeneral ToListQueryResultGeneral()
    {
        return new ListQueryResultGeneral(TotalCount, TotalPages, CurrentPage, PageSize, Items, RecordType, Query);
    }
}