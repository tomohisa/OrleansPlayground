using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Generated;
using AspireEventSample.ApiService.Projections;
using ResultBoxes;
using Sekiban.Pure;
using Sekiban.Pure.xUnit;
namespace AspireEventSample.UnitTest;

public class AspireEventSampleUnitTest2 : SekibanInMemoryTestBase
{
    protected override SekibanDomainTypes GetDomainTypes() => AspireEventSampleApiServiceDomainTypes.Generate(
        AspireEventSampleApiServiceEventsJsonContext.Default.Options);

    [Fact]
    public Task RegisterBranchTest()
        => GivenCommand(new RegisterBranch("DDD"))
            .Do(response => Assert.Equal(1, response.Version))
            .Conveyor(response => WhenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Do(response => Assert.Equal(2, response.Version))
            .Conveyor(response => ThenGetAggregate<BranchProjector>(response.PartitionKeys))
            .Conveyor(aggregate => aggregate.Payload.ToResultBox().Cast<Branch>())
            .Do(payload => Assert.Equal("ES", payload.Name))
            .UnwrapBox();

    [Fact]
    public Task RegisterTwoBranchTest()
        => GivenCommand(new RegisterBranch("DDD"))
            .Do(_ => Assert.Single(Repository.Events))
            .Conveyor(_ => GivenCommand(new RegisterBranch("DDD2")))
            .Do(_ => Assert.Equal(2, Repository.Events.Count))
            .Do(response => Assert.Equal(1, response.Version))
            .Conveyor(response => WhenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Do(response => Assert.Equal(2, response.Version))
            .Conveyor(response => ThenGetAggregate<BranchProjector>(response.PartitionKeys))
            .Conveyor(aggregate => aggregate.Payload.ToResultBox().Cast<Branch>())
            .Do(payload => Assert.Equal("ES", payload.Name))
            .UnwrapBox();

    [Fact]
    public Task RegisterBranchAndQueryTest()
        => GivenCommand(new RegisterBranch("DDD"))
            .Conveyor(response => GivenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Conveyor(_ => ThenQuery(new BranchExistsQuery("DDD")))
            .Do(Assert.False)
            .Conveyor(_ => ThenQuery(new BranchExistsQuery("ES")))
            .Do(Assert.True)
            .UnwrapBox();

    [Fact]
    public Task RegisterBranchAndListQuery()
        => GivenCommand(new RegisterBranch("DDD"))
            .Conveyor(response => GivenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Conveyor(_ => ThenQuery(new SimpleBranchListQuery("DDD")))
            .Do(queryResult => Assert.Empty(queryResult.Items))
            .Conveyor(_ => ThenQuery(new SimpleBranchListQuery("ES")))
            .Do(queryResult => Assert.Single(queryResult.Items))
            .UnwrapBox();
}