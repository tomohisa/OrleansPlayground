using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Projections;
using ResultBoxes;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
public class AspireEventSampleApiServiceMultiProjectorType2 : IMultiProjectorsType
{
    public ResultBox<IMultiProjectorCommon> Project(IMultiProjectorCommon multiProjector, IEvent ev)
        => multiProjector switch
        {
            BranchMultiProjector branchMultiProjector => branchMultiProjector
                .Project(branchMultiProjector, ev)
                .Remap(mp => (IMultiProjectorCommon)mp),
            _ => new ApplicationException(multiProjector.GetType().Name)
        };

    public ResultBox<IMultiProjectorCommon>
        Project(IMultiProjectorCommon multiProjector, IReadOnlyList<IEvent> events) => ResultBox
        .FromValue(events.ToList())
        .ReduceEach(multiProjector, (ev, common) => Project(common, ev));

    public IMultiProjectorStateCommon ToTypedState(MultiProjectionState state)
        => state.ProjectorCommon switch
        {
            BranchMultiProjector projector => new MultiProjectionState<BranchMultiProjector>(
                projector,
                state.LastEventId,
                state.LastSortableUniqueId,
                state.Version,
                state.AppliedSnapshotVersion,
                state.RootPartitionKey),
            _ => throw new ArgumentException(
                $"No state type found for projector type: {state.ProjectorCommon.GetType().Name}")
        };

    public IMultiProjectorCommon GetProjectorFromMultiProjectorName(string grainName)
        => grainName switch
        {
            not null when BranchMultiProjector.GetMultiProjectorName() == grainName =>
                BranchMultiProjector.GenerateInitialPayload(),
            not null when AggregateListProjector<BranchProjector>.GetMultiProjectorName() == grainName =>
                AggregateListProjector<BranchProjector>.GenerateInitialPayload(),
            _ => throw new ArgumentException($"No projector found for grain name: {grainName}")
        };
}