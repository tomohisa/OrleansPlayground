using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Generated;
using AspireEventSample.ApiService.Projections;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Executors;
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

    [Fact]
    public async Task InMemorySetupTest()
    {
        var executor = new InMemorySekibanExecutor(
            new AspireEventSampleApiServiceEventTypes(),
            new FunctionCommandMetadataProvider(() => "test"),
            new AspireEventSampleApiServiceQueryTypes(),
            new AspireEventSampleApiServiceMultiProjectorType());

        var result1 = await executor.ExecuteCommandAsync(new RegisterBranch("DDD"));
        Assert.True(result1.IsSuccess);
        var value = result1.GetValue();
        Assert.NotNull(value);
        Assert.Equal(1, value.Version);
        var aggregateId = value.PartitionKeys.AggregateId;
        Assert.NotEqual(Guid.Empty, aggregateId);
    }
}