using System.Text.Json.Serialization;
using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Repositories;

namespace AspireEventSample.ApiService.Grains;

public interface IAggregateProjectorGrain : IGrainWithStringKey
{
    /// <summary>
    /// 現在の状態を取得する。
    /// Stateが未作成の場合や、Projectorのバージョンが変わっている場合などは、
    /// イベントを一括取得して再構築することを考慮。
    /// </summary>
    /// <returns>現在の集約状態</returns>
    Task<IAggregate> GetStateAsync();

    /// <summary>
    /// コマンドを実行するエントリポイント。
    /// 現在の状態をベースに CommandHandler を使ってイベントを生成し、AggregateEventHandler へ送る。
    /// </summary>
    /// <param name="command">実行するコマンド</param>
    /// <returns>実行後の状態や生成イベントなど、必要に応じて返す</returns>
    Task<CommandResponse> ExecuteCommandAsync(ICommandWithHandlerSerializable command);

    /// <summary>
    /// State を一から再構築する(バージョンアップ時や State 破損時など)。
    /// すべてのイベントを AggregateEventHandler から受け取り、Projector ロジックを通して再構成。
    /// </summary>
    /// <returns>再構築後の新しい状態</returns>
    Task<IAggregatePayload> RebuildStateAsync();
}

public class AggregateProjectorGrain(
    [PersistentState(stateName: "aggregate", storageName: "projected")] IPersistentState<Aggregate> state) : Grain, IAggregateProjectorGrain
{
    public async Task<IAggregate> GetStateAsync()
    {
        var partitionKeysAndProjector = PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString()).UnwrapBox();
        return Repository.Load(partitionKeysAndProjector.PartitionKeys, partitionKeysAndProjector.Projector).UnwrapBox();
    }
    public async Task<CommandResponse> ExecuteCommandAsync(ICommandWithHandlerSerializable command)
    {
        var partitionKeysAndProjector = PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString()).UnwrapBox();
        this.GetPrimaryKeyString();
        var commandExecutor = new CommandExecutor();
        var result = await commandExecutor.ExecuteGeneralNonGeneric(command, partitionKeysAndProjector.Projector, partitionKeysAndProjector.PartitionKeys, NoInjection.Empty, command.GetHandler(), command.GetAggregatePayloadType());
        var aggregate = Repository.Load(partitionKeysAndProjector.PartitionKeys, partitionKeysAndProjector.Projector).UnwrapBox();
        state.State = aggregate;
        await state.WriteStateAsync();
        return result.UnwrapBox();
    }

    public async Task<IAggregatePayload> RebuildStateAsync()
    {
        throw new System.NotImplementedException();
    }
}

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

[GenerateSerializer]
public record OrleansResultBox<TValue>(Exception? Exception,TValue? Value) where TValue : notnull
{
    [JsonIgnore] public bool IsSuccess => Exception is null && Value is not null;
    public Exception GetException() =>
        Exception ?? throw new ResultsInvalidOperationException("no exception");

    public TValue GetValue() =>
        (IsSuccess ? Value : throw new ResultsInvalidOperationException("no value")) ??
        throw new ResultsInvalidOperationException();
}

public static class OrleansResultBoxExtensions
{
    public static OrleansResultBox<TValue> ToOrleansResultBox<TValue>(this ResultBox<TValue> resultBox) where TValue : notnull
    {
        return resultBox.IsSuccess ? new OrleansResultBox<TValue>(null, resultBox.GetValue()) : new OrleansResultBox<TValue>(resultBox.GetException(), default);
    }
}

[GenerateSerializer]
public record Branch(string Name) : IAggregatePayload;

public record BranchCreated(string Name) : IEventPayload;
public record BranchNameChanged(string Name) : IEventPayload;
[GenerateSerializer]
public record RegisterBranch(string Name) : ICommandWithHandler<RegisterBranch, BranchProjector>
{
    public PartitionKeys SpecifyPartitionKeys(RegisterBranch command) => PartitionKeys<BranchProjector>.Generate();
    public ResultBox<EventOrNone> Handle(RegisterBranch command, ICommandContext<IAggregatePayload> context) =>
        EventOrNone.Event(new BranchCreated(command.Name));
}

public interface IAggregateProjectorSpecifier
{
    ResultBox<IAggregateProjector> GetProjector(string projectorName);
}
public class MyAggregateProjectorSpecifier : IAggregateProjectorSpecifier
{
    public ResultBox<IAggregateProjector> GetProjector(string projectorName)
    {
        return projectorName switch
        {
            nameof(BranchProjector) => new BranchProjector(),
            _ => new ResultsInvalidOperationException("unknown projector")
        };
    }
}
public class BranchProjector : IAggregateProjector
{
    public IAggregatePayload Project(IAggregatePayload payload, IEvent ev) =>
        (payload, ev.GetPayload()) switch
        {
            (EmptyAggregatePayload, BranchCreated created) => new Branch(created.Name),
            (Branch branch, BranchNameChanged changed) => new Branch(changed.Name),
            _ => payload
        };
}

[GenerateSerializer]
record OrleansAggregate<TAggregatePayload>(
    TAggregatePayload Payload,
    PartitionKeys PartitionKeys,
    int Version,
    string LastSortableUniqueId) : IAggregate where TAggregatePayload : IAggregatePayload
{
    public IAggregatePayload GetPayload()
    {
        return Payload;
    }
}
