using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.Carts;
using AspireEventSample.ApiService.Generated;
using AspireEventSample.ApiService.Projections;
using Orleans.Serialization;
using ResultBoxes;
using Sekiban.Pure;
using Sekiban.Pure.Orleans.NUnit;
namespace AspireEventSample.NUnitTest;

public class OrleansTest : SekibanOrleansTestBase<OrleansTest>
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

    [Test]
    public void TestCreateShoppingCartThrows()
    {
        Assert.ThrowsAsync<CodecNotFoundException>(
            async () =>
            {
                await WhenCommand(new CreateShoppingCart(Guid.CreateVersion7())).UnwrapBox();
            });
    }

    public override SekibanDomainTypes GetDomainTypes() =>
        AspireEventSampleApiServiceDomainTypes.Generate(AspireEventSampleApiServiceEventsJsonContext.Default.Options);

    [Test]
    public Task RegisterBranchAndListQueryTest()
        => GivenCommand(new RegisterBranch("DDD"))
            .Conveyor(response => GivenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Conveyor(_ => ThenQuery(new BranchExistsQuery("ES")))
            .Do(queryResult => Assert.That(queryResult, Is.True))
            .Conveyor(_ => ThenQuery(new SimpleBranchListQuery("DDD")))
            .Do(queryResult => Assert.That(queryResult.Items, Is.Empty))
            .Conveyor(_ => ThenQuery(new SimpleBranchListQuery("ES")))
            .Do(queryResult => Assert.That(queryResult.Items.Count(), Is.EqualTo(1)))
            .UnwrapBox();
}