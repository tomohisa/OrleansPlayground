namespace Sekiban.Pure.OrleansEventSourcing;

public interface IMultiProjectorGrain : IGrainWithStringKey
{
    Task RebuildStateAsync();
    Task BuildStateAsync();
    Task<IMultiProjectionOrleansResult> GetStateAsync();
}

public interface IMultiProjectionOrleansResult;



public class MultiProjectorGrain : Grain, IMultiProjectorGrain
{
    public Task RebuildStateAsync()
    {
        throw new NotImplementedException();
    }

    public Task BuildStateAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IMultiProjectionOrleansResult> GetStateAsync()
    {
        throw new NotImplementedException();
    }
}