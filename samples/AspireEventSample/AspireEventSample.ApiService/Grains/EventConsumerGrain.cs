using Orleans.Streams;
using Sekiban.Pure.OrleansEventSourcing;

namespace AspireEventSample.ApiService.Grains;

// public class OrderStreamNamespacePredicateProvider : IStreamNamespacePredicateProvider
// {
//
//     public bool TryGetPredicate(string predicatePattern, out IStreamNamespacePredicate predicate)
//     {
//         // ここで任意のPredicateインスタンスを返す
//         predicate = new OrderStreamNamespacePredicate();
//         return true;
//     }
// }
//
// public class OrderStreamNamespacePredicate : IStreamNamespacePredicate
// {
//     public bool IsMatch(string streamNamespace)
//     {
//         // ここでNamespaceのマッチング処理を行う
//         return streamNamespace.StartsWith("AllEvents");
//     }
//
//     public string PredicatePattern => "AllEvents";
// }

[ImplicitStreamSubscription("AllEvents")]
public class EventConsumerGrain : Grain
{
    // private readonly ILogger<IEventConsumerGrain> _logger;
    private IAsyncStream<OrleansEvent> _stream;

    private StreamSubscriptionHandle<OrleansEvent> _subscriptionHandle;
    // public EventConsumerGrain(ILogger<IEventConsumerGrain> logger)
    // {
    //     _logger = logger;
    // }

    // public Task ConsumeEventsAsync(IReadOnlyList<OrleansEvent> events)
    // {
    //     // ここでイベントを消費する処理を書く
    //     return Task.CompletedTask;
    // }

    // イベント受信時のハンドラ
    // public Task OnNextAsync(OrleansEvent evt, StreamSequenceToken token, StreamId streamId)
    // {
    // //     // ここでイベントを受信したときの処理を書く
    // //     return Task.CompletedTask;
    // // }
    // // {
    //     Console.WriteLine($"[MyGrain] Received event: {evt}");
    //     // ここで処理を行う
    //     return Task.CompletedTask;
    // }

    // ストリーム購読中に例外が発生した場合のハンドラ
    public Task OnErrorAsync(Exception ex)
    {
        Console.WriteLine($"[MyGrain] Stream error: {ex.Message}");
        return Task.CompletedTask;
    }

    // ストリームが完了した場合のハンドラ
    public Task OnNextAsync(OrleansEvent item, StreamSequenceToken? token)
    {
        Console.WriteLine($"[MyGrain] Received event: {item}");
        // var ev = item
        return Task.CompletedTask;
    }

    public Task OnCompletedAsync()
    {
        Console.WriteLine("[MyGrain] Stream completed.");
        return Task.CompletedTask;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // _logger.LogInformation("OnActivateAsync");
        
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

    public Task ConsumeEventsAsync(IReadOnlyList<OrleansEvent> events)
    {
        throw new NotImplementedException();
    }
}
