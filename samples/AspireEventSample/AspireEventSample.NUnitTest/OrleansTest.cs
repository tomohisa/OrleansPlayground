using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.Carts;
using AspireEventSample.ApiService.Generated;
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

    // [Test]
    // public void TestCreateShoppingCart()
    //     => Assert.ThrowsAsync<CodecNotFoundException>(
    //         () => WhenCommand(new CreateShoppingCart(Guid.CreateVersion7())));

    [Test]
    public Task TestCreateShoppingCart()
        => WhenCommand(new CreateShoppingCart(Guid.CreateVersion7())).UnwrapBox();

    // [Test]
    // public void SerializationOrleansTest1()
    // {
    //     var original = new CreateShoppingCart(Guid.CreateVersion7());
    //
    //     // シリアライズ→デシリアライズのラウンドトリップ
    //     var copy = Orleans.Serialization.SerializationManager.RoundTripSerializationForTesting(original);
    //
    //     // オブジェクトの内容が一致しているか検証
    //     Assert.Equal(original, copy);
    // }
    public override SekibanDomainTypes GetDomainTypes() =>
        AspireEventSampleApiServiceDomainTypes.Generate(AspireEventSampleApiServiceEventsJsonContext.Default.Options);
}