using Microsoft.Extensions.DependencyInjection;
using ResultBoxes;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Query;
namespace Sekiban.Pure.Orleans.Grains;

public class MultiProjectorGrain(
    [PersistentState("multiProjector", "Default")]
    IPersistentState<MultiProjectionState> safeState,
    IEventReader eventReader,
    SekibanDomainTypes sekibanDomainTypes) : Grain, IMultiProjectorGrain
{
    private static readonly TimeSpan SafeStateTime = TimeSpan.FromSeconds(5);
    private MultiProjectionState? UnsafeState { get; set; }

    public async Task RebuildStateAsync()
    {
        var projector = GetProjectorFromMultiProjectorName();
        var info = EventRetrievalInfo.All;
        var events = (await eventReader.GetEvents(info)).UnwrapBox();
        var currentTime = DateTime.UtcNow;
        var safeTimeThreshold = currentTime.Subtract(SafeStateTime);
        safeState.State = new MultiProjectionState(
            projector,
            Guid.Empty,
            string.Empty,
            0,
            0,
            "default");

        if (events.Count == 0) return;

        // Split events into safe and unsafe based on time
        var lastEvent = events[^1];
        var lastEventSortableId = new SortableUniqueIdValue(lastEvent.SortableUniqueId);
        var safeTimeIdValue = new SortableUniqueIdValue(safeTimeThreshold.ToString("O"));

        if (lastEventSortableId.IsEarlierThan(safeTimeIdValue))
        {
            var projectedState = sekibanDomainTypes.MultiProjectorsType.Project(projector, events).UnwrapBox();
            // All events are safe to persist
            safeState.State = new MultiProjectionState(
                projectedState,
                lastEvent.Id,
                lastEvent.SortableUniqueId,
                safeState.State?.Version + 1 ?? 1,
                0,
                safeState.State?.RootPartitionKey ?? "default");
            await safeState.WriteStateAsync();
            UnsafeState = null;
        } else
        {
            // Find split point between safe and unsafe events
            var splitIndex = events
                .ToList()
                .FindLastIndex(
                    e =>
                        new SortableUniqueIdValue(e.SortableUniqueId).IsEarlierThan(safeTimeIdValue));

            if (splitIndex >= 0)
            {
                var safeEvents = events.Take(splitIndex + 1).ToList();
                var lastSafeEvent = safeEvents[^1];
                var safeProjectedState
                    = sekibanDomainTypes.MultiProjectorsType.Project(projector, safeEvents).UnwrapBox();
                safeState.State = new MultiProjectionState(
                    safeProjectedState,
                    lastSafeEvent.Id,
                    lastSafeEvent.SortableUniqueId,
                    safeState.State?.Version + 1 ?? 1,
                    0,
                    safeState.State?.RootPartitionKey ?? "default");
                await safeState.WriteStateAsync();
            }

            // Set unsafe state with full projection
            var unsafeEvents = events.Skip(splitIndex + 1).ToList();
            var unsafeProjectedState =
                sekibanDomainTypes
                    .MultiProjectorsType
                    .Project(safeState.State.ProjectorCommon, unsafeEvents)
                    .UnwrapBox();
            // Set unsafe state with full projection
            UnsafeState = new MultiProjectionState(
                unsafeProjectedState,
                lastEvent.Id,
                lastEvent.SortableUniqueId,
                safeState.State.Version + 1,
                0,
                safeState.State.RootPartitionKey);
        }
    }

    public async Task BuildStateAsync()
    {
        if (safeState.RecordExists == false)
        {
            await RebuildStateAsync();
            return;
        }

        var info = EventRetrievalInfo.All with
        {
            SortableIdCondition =
            ISortableIdCondition.Since(new SortableUniqueIdValue(safeState.State.LastSortableUniqueId))
        };

        var events = (await eventReader.GetEvents(info)).UnwrapBox();
        if (!events.Any()) return;
        var currentTime = DateTime.UtcNow;
        var safeTimeThreshold = currentTime.Subtract(SafeStateTime);


        var lastEvent = events[^1];
        var lastEventSortableId = new SortableUniqueIdValue(lastEvent.SortableUniqueId);
        var safeTimeIdValue = new SortableUniqueIdValue(safeTimeThreshold.ToString("O"));

        if (lastEventSortableId.IsEarlierThan(safeTimeIdValue))
        {
            var projectedState = sekibanDomainTypes
                .MultiProjectorsType
                .Project(safeState.State.ProjectorCommon, events)
                .UnwrapBox();

            // All new events are safe to persist
            safeState.State = new MultiProjectionState(
                projectedState,
                lastEvent.Id,
                lastEvent.SortableUniqueId,
                safeState.State.Version + 1,
                0,
                safeState.State.RootPartitionKey);
            await safeState.WriteStateAsync();
            UnsafeState = null;
        } else
        {
            // Find split point between safe and unsafe events
            var splitIndex = events
                .ToList()
                .FindLastIndex(
                    e =>
                        new SortableUniqueIdValue(e.SortableUniqueId).IsEarlierThan(safeTimeIdValue));
            if (splitIndex >= 0)
            {
                var safeEvents = events.Take(splitIndex + 1).ToList();
                var lastSafeEvent = safeEvents[^1];
                var safeProjectedState =
                    sekibanDomainTypes
                        .MultiProjectorsType
                        .Project(safeState.State.ProjectorCommon, safeEvents)
                        .UnwrapBox();
                safeState.State = new MultiProjectionState(
                    safeProjectedState,
                    lastSafeEvent.Id,
                    lastSafeEvent.SortableUniqueId,
                    safeState.State.Version + 1,
                    0,
                    safeState.State.RootPartitionKey);
                await safeState.WriteStateAsync();
            }
            var unsafeEvents = events.Skip(splitIndex + 1).ToList();
            var unsafeProjectedState =
                sekibanDomainTypes
                    .MultiProjectorsType
                    .Project(safeState.State.ProjectorCommon, unsafeEvents)
                    .UnwrapBox();
            // Set unsafe state with full projection
            UnsafeState = new MultiProjectionState(
                unsafeProjectedState,
                lastEvent.Id,
                lastEvent.SortableUniqueId,
                safeState.State.Version + 1,
                0,
                safeState.State.RootPartitionKey);
        }
    }

    public async Task<MultiProjectionState> GetStateAsync()
    {
        await BuildStateAsync();
        return UnsafeState ?? safeState.State;
    }

    public async Task<QueryResultGeneral> QueryAsync(IQueryCommon query)
    {
        var result = await sekibanDomainTypes.QueryTypes.ExecuteAsQueryResult(
                query,
                GetProjectorForQuery,
                new ServiceCollection().BuildServiceProvider()) ??
            throw new ApplicationException("Query not found");
        return result
            .Remap(value => value.ToGeneral(query))
            .UnwrapBox();
    }

    public async Task<IListQueryResult> QueryAsync(IListQueryCommon query)
    {
        var result = await sekibanDomainTypes.QueryTypes.ExecuteAsQueryResult(
                query,
                GetProjectorForQuery,
                new ServiceCollection().BuildServiceProvider()) ??
            throw new ApplicationException("Query not found");
        return result.UnwrapBox();
    }

    public async Task<ResultBox<IMultiProjectorStateCommon>> GetProjectorForQuery(
        IMultiProjectionEventSelector multiProjectionEventSelector)
    {
        await BuildStateAsync();
        return UnsafeState?.ToResultBox<IMultiProjectorStateCommon>() ??
            safeState?.State.ToResultBox<IMultiProjectorStateCommon>() ??
            new ApplicationException("No state found");
    }


    public IMultiProjectorCommon GetProjectorFromMultiProjectorName()
    {
        var grainName = this.GetPrimaryKeyString();
        return sekibanDomainTypes.MultiProjectorsType.GetProjectorFromMultiProjectorName(grainName);
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
}