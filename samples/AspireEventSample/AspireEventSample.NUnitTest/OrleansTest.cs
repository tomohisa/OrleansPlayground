using AspireEventSample.ApiService.Aggregates.Branches;
using ResultBoxes;
using Sekiban.Pure.Orleans.NUnit;
namespace AspireEventSample.NUnitTest;

public class OrleansTest : SekibanOrleansTestBase<MyDomainGetter>
{
    [Test]
    public Task Test1()
        => GivenCommand(new RegisterBranch("DDD"))
            .Do(response => Assert.That(response.Version, Is.EqualTo(1)))
            .Conveyor(response => WhenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Do(response => Assert.That(response.Version, Is.EqualTo(2)))
            .Conveyor(response => ThenGetAggregate<BranchProjector>(response.PartitionKeys))
            .Conveyor(aggregate => aggregate.Payload.ToResultBox().Cast<Branch>())
            .Do(payload => Assert.That(payload.Name, Is.EqualTo("ES")))
            .UnwrapBox();
}