using AspireEventSample.ApiService.Generated;
using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.OrleansEventSourcing;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Repositories;
using Sekiban.Pure.Types;

namespace AspireEventSample.ApiService.Grains;

public class AggregateProjectorGrain(
    [PersistentState(stateName: "aggregate", storageName: "Default")] IPersistentState<Aggregate> state, SekibanTypeConverters typeConverters) : Grain, IAggregateProjectorGrain
{
    private OptionalValue<PartitionKeysAndProjector> _partitionKeysAndProjector = OptionalValue<PartitionKeysAndProjector>.Empty;

    private PartitionKeysAndProjector GetPartitionKeysAndProjector()
    {
        if (_partitionKeysAndProjector.HasValue) return _partitionKeysAndProjector.GetValue();
        _partitionKeysAndProjector = PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString(), typeConverters.AggregateProjectorSpecifier).UnwrapBox();
        return _partitionKeysAndProjector.GetValue();
    }
    public async Task<OrleansAggregate> GetStateAsync()
    {
        await state.ReadStateAsync();
        var read = state.State;
        if (read == null || GetPartitionKeysAndProjector().Projector.GetVersion() != read.ProjectorVersion)
        {
            return await RebuildStateAsync();
        }
        return read.ToOrleansAggregate();
    }

    public async Task<OrleansCommandResponse> ExecuteCommandAsync(ICommandWithHandlerSerializable orleansCommand)
    {
        var eventGrain = GrainFactory.GetGrain<IAggregateEventHandlerGrain>(GetPartitionKeysAndProjector().ToEventHandlerGrainKey());
        var orleansRepository = new OrleansRepository(eventGrain, GetPartitionKeysAndProjector().PartitionKeys, GetPartitionKeysAndProjector().Projector, typeConverters.EventTypes);
        var commandExecutor = new CommandExecutor() {EventTypes = typeConverters.EventTypes };
        var result = await commandExecutor.ExecuteGeneralNonGeneric(orleansCommand, 
            GetPartitionKeysAndProjector().Projector, GetPartitionKeysAndProjector().PartitionKeys, NoInjection.Empty, 
            orleansCommand.GetHandler(), orleansCommand.GetAggregatePayloadType(),(_, _) => orleansRepository.Load(), orleansRepository.Save );
        // best if just use events to update state
        var aggregate = await orleansRepository.Load().UnwrapBox();
        state.State = aggregate;
        await state.WriteStateAsync();
        return result.UnwrapBox().ToOrleansCommandResponse();
    }

    public async Task<OrleansAggregate> RebuildStateAsync()
    {
        var eventGrain = GrainFactory.GetGrain<IAggregateEventHandlerGrain>(GetPartitionKeysAndProjector().ToEventHandlerGrainKey());
        var orleansRepository = new OrleansRepository(eventGrain, GetPartitionKeysAndProjector().PartitionKeys, GetPartitionKeysAndProjector().Projector, typeConverters.EventTypes);
        var aggregate = await orleansRepository.Load().UnwrapBox();
        state.State = aggregate;
        await state.WriteStateAsync();
        return aggregate.ToOrleansAggregate();
    }
}