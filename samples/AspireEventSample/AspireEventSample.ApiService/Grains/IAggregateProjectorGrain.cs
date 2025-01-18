using System.Text.Json.Serialization;
using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Documents;

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

public class AggregateProjectorGrain : Grain, IAggregateProjectorGrain
{
    public async Task<IAggregate> GetStateAsync()
    {
        throw new System.NotImplementedException();
    }

    public async Task<CommandResponse> ExecuteCommandAsync(ICommandWithHandlerSerializable command)
    {
        var projector = command.GetProjector();
        var handler = command.GetHandler();
        var partitionKeysSpecifier = command.GetPartitionKeysSpecifier();
        var commandExecutor = new CommandExecutor();
        var aggregateType = command.GetAggregatePayloadType();
        var result = await commandExecutor.ExecuteGeneralNonGeneric(command, projector, partitionKeysSpecifier, NoInjection.Empty, handler, aggregateType);
        return result.ToOrleansResultBox();
    }

    public async Task<IAggregatePayload> RebuildStateAsync()
    {
        throw new System.NotImplementedException();
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
public record UserAggregatePayload(string Name, int Age) : IAggregatePayload;

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

public class pr
{
    public async Task main()
    {
        new OrleansAggregate<UserAggregatePayload>(new UserAggregatePayload("Alice", 20), PartitionKeys.Generate(), 0, string.Empty);
    }
}
    