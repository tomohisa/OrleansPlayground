using ResultBoxes;
using Sekiban.Pure.Projectors;

namespace AspireEventSample.ApiService.Grains;

public interface IAggregateProjectorSpecifier
{
    ResultBox<IAggregateProjector> GetProjector(string projectorName);
}