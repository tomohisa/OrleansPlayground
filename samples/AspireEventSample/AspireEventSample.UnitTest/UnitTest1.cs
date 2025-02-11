using AspireEventSample.ApiService.Projections;
using Sekiban.Pure.Query;

namespace AspireEventSample.UnitTest;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var query = new BranchExistsQuery("Test");
        var queryResult = new QueryResult<bool>(true);
        var general = queryResult.ToGeneral(query);
        Assert.True((bool)general.GetValue());
    }
}