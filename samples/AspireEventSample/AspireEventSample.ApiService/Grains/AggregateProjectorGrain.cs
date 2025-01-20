using AspireEventSample.ApiService.Generated;
using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.OrleansEventSourcing;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Repositories;

namespace AspireEventSample.ApiService.Grains;

public class AggregateProjectorGrain(
    [PersistentState(stateName: "aggregate", storageName: "Default")] IPersistentState<Aggregate> state) : Grain, IAggregateProjectorGrain
{
    public async Task<OrleansAggregate> GetStateAsync()
    {
        var partitionKeysAndProjector = PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString()).UnwrapBox();
        var state = Repository.Load(partitionKeysAndProjector.PartitionKeys, partitionKeysAndProjector.Projector).UnwrapBox();
        return state.ToOrleansAggregate();
    }

    public Task<IAggregateProjector> GetProjectorAsync()
    {
        return Task.FromResult(PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString()).UnwrapBox()
            .Projector);
    }

    public async Task<OrleansCommandResponse> ExecuteCommandAsync(ICommandWithHandlerSerializable orleansCommand)
    {
        ICommandWithHandlerSerializable command = orleansCommand as ICommandWithHandlerSerializable ?? throw new ArgumentException("Invalid command type");
        var partitionKeysAndProjector = PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString()).UnwrapBox();
        this.GetPrimaryKeyString();
        var commandExecutor = new CommandExecutor() {EventTypes = new AspireEventSampleApiServiceEventTypes()};
        var result = await commandExecutor.ExecuteGeneralNonGeneric(command, partitionKeysAndProjector.Projector, partitionKeysAndProjector.PartitionKeys, NoInjection.Empty, command.GetHandler(), command.GetAggregatePayloadType());
        var aggregate = Repository.Load(partitionKeysAndProjector.PartitionKeys, partitionKeysAndProjector.Projector).UnwrapBox();
        state.State = aggregate;
        await state.WriteStateAsync();
        return result.UnwrapBox().ToOrleansCommandResponse();
    }

    public async Task<IAggregatePayload> RebuildStateAsync()
    {
        throw new System.NotImplementedException();
    }
}