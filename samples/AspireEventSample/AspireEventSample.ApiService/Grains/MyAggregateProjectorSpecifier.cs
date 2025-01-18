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
            _ => new ResultsInvalidOperationException("unknown projector")
        };
    }
}