using ResultBoxes;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
using System.Linq;
using Orleans.Runtime;
using Sekiban.Pure.Query;

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
        await safeState.ReadStateAsync();
    }
    
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task RebuildStateAsync()
    {
        var projector = GetProjectorFromGrainName();
        var info = EventRetrievalInfo.All;  
        var events = (await eventReader.GetEvents(info)).UnwrapBox();
        var currentTime = DateTime.UtcNow;
        var safeTimeThreshold = currentTime.Subtract(SafeStateTime);
        
        if (events.Count == 0)
        {
            return;
        }
        var projectedState = multiProjectorsType.Project(projector, events).UnwrapBox();

        // Split events into safe and unsafe based on time
        var lastEvent = events[^1];
        var lastEventSortableId = new SortableUniqueIdValue(lastEvent.SortableUniqueId);
        var safeTimeIdValue = new SortableUniqueIdValue(safeTimeThreshold.ToString("O"));
        
        if (lastEventSortableId.IsEarlierThan(safeTimeIdValue))
        {
            // All events are safe to persist
            safeState.State = new OrleansMultiProjectorState(
                projectedState,
                lastEvent.Id,
                lastEvent.SortableUniqueId,
                safeState.State?.Version + 1 ?? 1,
                0,
                safeState.State?.RootPartitionKey ?? "default");
            await safeState.WriteStateAsync();
            UnsafeState = null;
        }
        else
        {
            // Find split point between safe and unsafe events
            var splitIndex = events.ToList().FindLastIndex(e => 
                new SortableUniqueIdValue(e.SortableUniqueId).IsEarlierThan(safeTimeIdValue));
            
            if (splitIndex >= 0)
            {
                var safeEvents = events.Take(splitIndex + 1).ToList();
                var lastSafeEvent = safeEvents[^1];
                var safeProjectedState = multiProjectorsType.Project(projector, safeEvents).UnwrapBox();
                safeState.State = new OrleansMultiProjectorState(
                    safeProjectedState,
                    lastSafeEvent.Id,
                    lastSafeEvent.SortableUniqueId,
                    safeState.State?.Version + 1 ?? 1,
                    0,
                    safeState.State?.RootPartitionKey ?? "default");
                await safeState.WriteStateAsync();
            }
            
            // Set unsafe state with full projection
            UnsafeState = new OrleansMultiProjectorState(
                projectedState,
                lastEvent.Id,
                lastEvent.SortableUniqueId,
                safeState.State?.Version + 1 ?? 1,
                0,
                safeState.State?.RootPartitionKey ?? "default");
        }
    }

    public async Task BuildStateAsync()
    {
        if (safeState.RecordExists == false)
        {
            await RebuildStateAsync();
            return;
        }

        var projector = GetProjectorFromGrainName();
        var info = EventRetrievalInfo.All with { SortableIdCondition = ISortableIdCondition.Since(new SortableUniqueIdValue(safeState.State.LastSortableUniqueId)) };
        
        var events = (await eventReader.GetEvents(info)).UnwrapBox();
        if (!events.Any())
        {
            return;
        }
        var currentTime = DateTime.UtcNow;
        var safeTimeThreshold = currentTime.Subtract(SafeStateTime);
        
        var projectedState = multiProjectorsType.Project(safeState.State.ProjectorCommon, events).UnwrapBox();

        var lastEvent = events[^1];
        var lastEventSortableId = new SortableUniqueIdValue(lastEvent.SortableUniqueId);
        var safeTimeIdValue = new SortableUniqueIdValue(safeTimeThreshold.ToString("O"));
        
        if (lastEventSortableId.IsEarlierThan(safeTimeIdValue))
        {
            // All new events are safe to persist
            safeState.State = new OrleansMultiProjectorState(
                projectedState,
                lastEvent.Id,
                lastEvent.SortableUniqueId,
                safeState.State.Version + 1,
                0,
                safeState.State.RootPartitionKey);
            await safeState.WriteStateAsync();
            UnsafeState = null;
        }
        else
        {
            // Find split point between safe and unsafe events
            var splitIndex = events.ToList().FindLastIndex(e => 
                new SortableUniqueIdValue(e.SortableUniqueId).IsEarlierThan(safeTimeIdValue));
            
            if (splitIndex >= 0)
            {
                var safeEvents = events.Take(splitIndex + 1).ToList();
                var lastSafeEvent = safeEvents[^1];
                var safeProjectedState = multiProjectorsType.Project(safeState.State.ProjectorCommon, safeEvents).UnwrapBox();
                safeState.State = new OrleansMultiProjectorState(
                    safeProjectedState,
                    lastSafeEvent.Id,
                    lastSafeEvent.SortableUniqueId,
                    safeState.State.Version + 1,
                    0, 
                    safeState.State.RootPartitionKey);
                await safeState.WriteStateAsync();
            }

            // Set unsafe state with full projection
            UnsafeState = new OrleansMultiProjectorState(
                projectedState,
                lastEvent.Id,
                lastEvent.SortableUniqueId,
                safeState.State.Version + 1,
                0,
                safeState.State.RootPartitionKey);
        }
    }

    public async Task<OrleansMultiProjectorState> GetStateAsync()
    {
        await BuildStateAsync();
        return UnsafeState ?? safeState.State;
    }
    public Task<IOrleansQueryResult> QueryAsync(IQueryCommon query)
    {
        throw new NotImplementedException();

    }
}
