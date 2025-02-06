namespace Sekiban.Pure.OrleansEventSourcing;

public interface IMultiProjectorGrain : IGrainWithStringKey
{
    Task RebuildStateAsync();
    Task BuildStateAsync();
    Task<OrleansMultiProjectorState> GetStateAsync();
}
