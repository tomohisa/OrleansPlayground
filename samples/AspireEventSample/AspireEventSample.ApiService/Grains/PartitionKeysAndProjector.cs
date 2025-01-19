using ResultBoxes;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Projectors;

namespace AspireEventSample.ApiService.Grains;

public record PartitionKeysAndProjector(PartitionKeys PartitionKeys, IAggregateProjector Projector)
{
    public static ResultBox<PartitionKeysAndProjector> FromGrainKey(string grainKey)
    {
        var splitted = grainKey.Split("=");
        if (splitted.Length != 2)
        {
            throw new ResultsInvalidOperationException("invalid grain key");
        }
        var partitionKeys = PartitionKeys.FromPrimaryKeysString(splitted[0]).UnwrapBox();
        var projectorSpecifier = new MyAggregateProjectorSpecifier();
        return projectorSpecifier.GetProjector(splitted[1]).Remap(projector => new PartitionKeysAndProjector(partitionKeys, projector));
    }
}