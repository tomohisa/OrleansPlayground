using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Generated;
using AspireEventSample.ApiService.Projections;
using ResultBoxes;
using Sekiban.Pure;
using Sekiban.Pure.NUnit;
namespace AspireEventSample.NUnitTest;

public class AspireEventSampleUnitTest2 : SekibanInMemoryTestBase
{
    protected override SekibanDomainTypes GetDomainTypes() => AspireEventSampleApiServiceDomainTypes.Generate(
        AspireEventSampleApiServiceEventsJsonContext.Default.Options);

    [Test]
    public Task RegisterBranchTest()
        => GivenCommand(new RegisterBranch("DDD"))
            .Do(response => Assert.That(response.Version, Is.EqualTo(1)))
            .Conveyor(response => WhenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Do(response => Assert.That(response.Version, Is.EqualTo(2)))
            .Conveyor(response => ThenGetAggregate<BranchProjector>(response.PartitionKeys))
            .Conveyor(aggregate => aggregate.Payload.ToResultBox().Cast<Branch>())
            .Do(payload => Assert.That(payload.Name, Is.EqualTo("ES")))
            .UnwrapBox();

    [Test]
    public Task RegisterBranchAndQueryTest()
        => GivenCommand(new RegisterBranch("DDD"))
            .Conveyor(response => GivenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Conveyor(_ => ThenQuery(new BranchExistsQuery("DDD")))
            .Do(queryResult => Assert.That(queryResult, Is.False))
            .Conveyor(_ => ThenQuery(new BranchExistsQuery("ES")))
            .Do(queryResult => Assert.That(queryResult, Is.True))
            .UnwrapBox();
    [Test]
    public Task RegisterBranchAndListQueryTest()
        => GivenCommand(new RegisterBranch("DDD"))
            .Conveyor(response => GivenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Conveyor(_ => ThenQuery(new SimpleBranchListQuery("DDD")))
            .Do(queryResult => Assert.That(queryResult.Items.Count(), Is.EqualTo(0)))
            .Conveyor(_ => ThenQuery(new SimpleBranchListQuery("ES")))
            .Do(queryResult => Assert.That(queryResult.Items.Count(), Is.EqualTo(1)))
            .UnwrapBox();

    [Test]
    public Task RegisterTwoBranchTest()
        => GivenCommand(new RegisterBranch("DDD"))
            .Do(_ => Assert.That(Repository.Events, Has.Count.EqualTo(1)))
            .Conveyor(_ => GivenCommand(new RegisterBranch("DDD2")))
            .Do(_ => Assert.That(Repository.Events, Has.Count.EqualTo(2)))
            .Do(response => Assert.That(response.Version, Is.EqualTo(1)))
            .Conveyor(response => WhenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Do(response => Assert.That(response.Version, Is.EqualTo(2)))
            .Conveyor(response => ThenGetAggregate<BranchProjector>(response.PartitionKeys))
            .Conveyor(aggregate => aggregate.Payload.ToResultBox().Cast<Branch>())
            .Do(payload => Assert.That(payload.Name, Is.EqualTo("ES")))
            .UnwrapBox();
}