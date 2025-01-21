using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.Carts;
using ResultBoxes;
using Sekiban.Pure.Projectors;

namespace AspireEventSample.ApiService.Grains;

public class MyAggregateProjectorSpecifier : IAggregateProjectorSpecifier
{
    public ResultBox<IAggregateProjector> GetProjector(string projectorName)
    {
        return projectorName switch
        {
            nameof(BranchProjector) => new BranchProjector(),
            nameof(ShoppingCartProjector) => new ShoppingCartProjector(),
            _ => new ResultsInvalidOperationException("unknown projector")
        };
    }
}