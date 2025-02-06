using Sekiban.Pure.Projectors;

namespace Sekiban.Pure.OrleansEventSourcing;

public class MultiProjectorGrain(IMultiProjectorsType multiProjectorsType, [PersistentState(stateName: "multiProjector", storageName: "Default")] IPersistentState<OrleansMultiProjectorState> safeState, IEventReader eventReader) : Grain, IMultiProjectorGrain
{
    private static readonly TimeSpan SafeStateTime = TimeSpan.FromSeconds(10);
    private OrleansMultiProjectorState? UnsafeState { get; set; } = null;

    public IMultiProjectorCommon GetProjectorFromGrainName()
    {
        var grainName = this.GetPrimaryKeyString();
        return multiProjectorsType.GetProjectorFromGrainName(grainName);
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        
        await base.OnActivateAsync(cancellationToken);
        // if state is not yet saved create new empty state using GetProjectorFromGrainName and save it.
        // save state
    }
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task RebuildStateAsync()
    {
        // get all state and project events from initial state. Save only SafeStateTime past events only to safe state.
        // and last SafeStateTime projection should save to UnsafeState.
    }

    public async Task BuildStateAsync()
    {
        // get state and project events after the last saved state. Save only SafeStateTime past events only to safe state.
        // and last SafeStateTime projection should save to UnsafeState.
        
    }

    public async Task<OrleansMultiProjectorState> GetStateAsync()
    {
        await BuildStateAsync();
        return UnsafeState ?? safeState.State;
    }
}