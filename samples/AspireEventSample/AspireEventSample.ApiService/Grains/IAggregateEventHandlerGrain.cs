using Orleans.Streams;
using Orleans.Streams.Core;
using Sekiban.Pure.OrleansEventSourcing;

namespace AspireEventSample.ApiService.Grains;

public interface IAggregateEventHandlerGrain : IGrainWithStringKey
{
    /// <summary>
    ///     指定された lastSortableUniqueId を前提に、新しいイベントを保存する。
    ///     楽観的排他を行い、手元の LastSortableUniqueId と異なる場合はエラーや差分を返す。
    ///     成功時には新たな LastSortableUniqueId を返却する。
    /// </summary>
    /// <param name="expectedLastSortableUniqueId">Projector側が認識している最後の SortableUniqueId</param>
    /// <param name="newEvents">作成されたイベント一覧</param>
    /// <returns>
    ///     (成功時) 新たな LastSortableUniqueId
    ///     (失敗時) 例外をスローする or もしくは差分イベントを返す別パターンなど
    /// </returns>
    Task<IReadOnlyList<OrleansEvent>> AppendEventsAsync(
        string expectedLastSortableUniqueId,
        IReadOnlyList<OrleansEvent> newEvents
    );

    /// <summary>
    ///     イベントの差分を取得する。
    /// </summary>
    /// <param name="fromSortableUniqueId">差分の取得開始点となる SortableUniqueId</param>
    /// <param name="limit">取得する最大件数(必要なら)</param>
    /// <returns>該当するイベント一覧</returns>
    Task<IReadOnlyList<OrleansEvent>> GetDeltaEventsAsync(
        string fromSortableUniqueId,
        int? limit = null
    );

    /// <summary>
    ///     全イベントを最初から取得する。
    ///     プロジェクタのバージョン変更などで State を作り直す際に利用。
    ///     取得件数が大きい場合はページングなども検討。
    /// </summary>
    /// <returns>全イベント一覧</returns>
    Task<IReadOnlyList<OrleansEvent>> GetAllEventsAsync();

    /// <summary>
    ///     現在の管理している最後の SortableUniqueId を返す。
    /// </summary>
    /// <returns>最後の SortableUniqueId</returns>
    Task<string> GetLastSortableUniqueIdAsync();

    /// <summary>
    ///     指定のプロジェクターを登録しておく(任意)。
    ///     複数 Projector がある場合、差分取得の最終取得位置を記録する仕組みなども考えられる。
    /// </summary>
    /// <param name="projectorKey">プロジェクターの固有キー</param>
    /// <returns></returns>
    Task RegisterProjectorAsync(string projectorKey);
}

public interface IEventConsumerGrain : IGrainWithGuidKey
{
    Task ConsumeEventsAsync(IReadOnlyList<OrleansEvent> events);
}

[ImplicitStreamSubscription("AllEvents")]
public class EventConsumerGrain : Grain, IEventConsumerGrain
{
    private readonly ILogger<IEventConsumerGrain> _logger;
    private IAsyncStream<OrleansEvent> _stream;

    private StreamSubscriptionHandle<OrleansEvent> _subscriptionHandle;
    public EventConsumerGrain(ILogger<IEventConsumerGrain> logger)
    {
        _logger = logger;
    }

    public Task ConsumeEventsAsync(IReadOnlyList<OrleansEvent> events)
    {
        // ここでイベントを消費する処理を書く
        return Task.CompletedTask;
    }

    // イベント受信時のハンドラ
    private Task OnNextAsync(OrleansEvent evt, StreamSequenceToken token)
    {
        Console.WriteLine($"[MyGrain] Received event: {evt}");
        // ここで処理を行う
        return Task.CompletedTask;
    }

    // ストリーム購読中に例外が発生した場合のハンドラ
    private Task OnErrorAsync(Exception ex)
    {
        Console.WriteLine($"[MyGrain] Stream error: {ex.Message}");
        return Task.CompletedTask;
    }

    // ストリームが完了した場合のハンドラ
    private Task OnCompletedAsync()
    {
        Console.WriteLine("[MyGrain] Stream completed.");
        return Task.CompletedTask;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OnActivateAsync");
        
        var streamProvider = this.GetStreamProvider("EventStreamProvider");

        _stream = streamProvider.GetStream<OrleansEvent>(StreamId.Create("AllEvents", Guid.Empty));

        // このGrainがアクティブ化されたタイミングで、該当Streamの購読を開始
        _subscriptionHandle = await _stream.SubscribeAsync(
            (evt, token) => OnNextAsync(evt, token),    // イベントを受信したとき
            OnErrorAsync,                               // 例外が起きたとき
            OnCompletedAsync                            // ストリームが完了したとき
        );

        await base.OnActivateAsync(cancellationToken);
    }
}