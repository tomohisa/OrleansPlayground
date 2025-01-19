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
    // Task<IAggregate> GetStateAsync();

    /// <summary>
    /// コマンドを実行するエントリポイント。
    /// 現在の状態をベースに CommandHandler を使ってイベントを生成し、AggregateEventHandler へ送る。
    /// </summary>
    /// <param name="command">実行するコマンド</param>
    /// <returns>実行後の状態や生成イベントなど、必要に応じて返す</returns>
    Task<OrleansCommandResponse> ExecuteCommandAsync(OrleansCommand command);

    /// <summary>
    /// State を一から再構築する(バージョンアップ時や State 破損時など)。
    /// すべてのイベントを AggregateEventHandler から受け取り、Projector ロジックを通して再構成。
    /// </summary>
    /// <returns>再構築後の新しい状態</returns>
    // Task<IAggregatePayload> RebuildStateAsync();
}

[GenerateSerializer]
public record OrleansCommandResponse([property:Id(0)]OrleansPartitionKeys PartitionKeys, [property:Id(1)]List<string> Events, [property:Id(2)]int Version)
{
}

[GenerateSerializer]
public record OrleansPartitionKeys(
    [property: Id(0)] Guid AggregateId,
    [property: Id(1)] string Group,
    [property: Id(2)] string RootPartitionKey);


[GenerateSerializer]
public record OrleansCommand([property:Id(0)]string payload);

public static class PartitionKeysExtensions
{
    public static OrleansPartitionKeys ToOrleansPartitionKeys(this PartitionKeys partitionKeys) =>
        new(partitionKeys.AggregateId, partitionKeys.Group, partitionKeys.RootPartitionKey);
}

public static class CommandResponseExtensions
{
    public static OrleansCommandResponse ToOrleansCommandResponse(this CommandResponse response) =>
        new(response.PartitionKeys.ToOrleansPartitionKeys(), response.Events.Select(e => e.ToString() ?? String.Empty).ToList(), response.Version);
}
