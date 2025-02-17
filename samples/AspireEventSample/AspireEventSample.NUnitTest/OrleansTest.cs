using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.Carts;
using AspireEventSample.ApiService.Generated;
using AspireEventSample.ApiService.Projections;
using Orleans.Serialization;
using ResultBoxes;
using Sekiban.Pure;
using Sekiban.Pure.Orleans.NUnit;
using Sekiban.Pure.Projectors;
namespace AspireEventSample.NUnitTest;

public class OrleansTest : SekibanOrleansTestBase<OrleansTest>
{
    [Test]
    public Task Test1()
        => GivenCommand(new RegisterBranch("DDD"))
            .Do(response => Assert.That(response.Version, Is.EqualTo(1)))
            .Conveyor(response => WhenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Do(response => Assert.That(response.Version, Is.EqualTo(2)))
            .Do( // wait 10 second for the event to be processed
                async _ =>
                {
                    await Task.Delay(10000);
                })
            .Conveyor(response => ThenGetAggregate<BranchProjector>(response.PartitionKeys))
            .Conveyor(aggregate => aggregate.Payload.ToResultBox().Cast<Branch>())
            .Do(payload => Assert.That(payload.Name, Is.EqualTo("ES")))
            .Conveyor(_ => ThenGetMultiProjector<BranchMultiProjector>())
            .Do(
                projector =>
                {
                    Assert.That(projector.Branches.Count, Is.EqualTo(1));
                    Assert.That(projector.Branches.Values.First().BranchName, Is.EqualTo("ES"));
                })
            .Conveyor(_ => ThenGetMultiProjector<AggregateListProjector<BranchProjector>>())
            .Do(
                projector =>
                {
                    Assert.That(projector.Aggregates.Values.Count, Is.EqualTo(1));
                    Assert.That(projector.Aggregates.Values.First().Payload, Is.TypeOf<Branch>());
                    Assert.That(((Branch)projector.Aggregates.Values.First().Payload).Name, Is.EqualTo("ES"));
                })
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