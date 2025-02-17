using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.Carts;
using AspireEventSample.ApiService.Generated;
using AspireEventSample.ApiService.Projections;
using Orleans.Serialization;
using ResultBoxes;
using Sekiban.Pure;
using Sekiban.Pure.Orleans.xUnit;
using Sekiban.Pure.Projectors;
namespace AspireEventSample.UnitTest;

public class OrleansTest : SekibanOrleansTestBase<OrleansTest>
{
    [Fact]
    public Task Test1() =>
        GivenCommand(new RegisterBranch("DDD"))
            .Do(response => Assert.Equal(1, response.Version))
            .Conveyor(response => WhenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            .Do(response => Assert.Equal(2, response.Version))
            // .Do(_ => Task.Delay(10000))
            .Conveyor(response => ThenGetAggregate<BranchProjector>(response.PartitionKeys))
            .Conveyor(aggregate => aggregate.Payload.ToResultBox().Cast<Branch>())
            .Do(payload => Assert.Equal("ES", payload.Name))
            .Conveyor(_ => ThenGetMultiProjector<BranchMultiProjector>())
            .Do(
                projector =>
                {
                    Assert.Equal(1, projector.Branches.Count);
                    Assert.Equal("ES", projector.Branches.Values.First().BranchName);
                })
            .Conveyor(_ => ThenGetMultiProjector<AggregateListProjector<BranchProjector>>())
            .Do(
                projector =>
                {
                    Assert.Equal(1, projector.Aggregates.Values.Count());
                    Assert.IsType<Branch>(projector.Aggregates.Values.First().Payload);
                    Assert.Equal("ES", ((Branch)projector.Aggregates.Values.First().Payload).Name);
                })
            .UnwrapBox();

    [Fact]
    public async Task TestCreateShoppingCartThrows()
    {
        await Assert.ThrowsAsync<CodecNotFoundException>(
            async () =>
            {
                await WhenCommand(new CreateShoppingCart(Guid.CreateVersion7())).UnwrapBox();
            });
    }

    public override SekibanDomainTypes GetDomainTypes() =>
        AspireEventSampleApiServiceDomainTypes.Generate(AspireEventSampleApiServiceEventsJsonContext.Default.Options);

    [Fact]
    public Task RegisterBranchAndListQueryTest() =>
        GivenCommand(new RegisterBranch("DDD"))
            .Conveyor(response => GivenCommand(new ChangeBranchName(response.PartitionKeys.AggregateId, "ES")))
            // .Do(_ => Task.Delay(10000))
            .Conveyor(_ => ThenQuery(new BranchExistsQuery("ES")))
            .Do(queryResult => Assert.True(queryResult))
            .Conveyor(_ => ThenQuery(new SimpleBranchListQuery("DDD")))
            .Do(queryResult => Assert.Empty(queryResult.Items))
            .Conveyor(_ => ThenQuery(new SimpleBranchListQuery("ES")))
            .Do(queryResult => Assert.Equal(1, queryResult.Items.Count()))
            .UnwrapBox();
}