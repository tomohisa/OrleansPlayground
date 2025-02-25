using Microsoft.AspNetCore.SignalR;
using Orleans.Streams;
using Sekiban.Pure.Events;

namespace MessageEachOther.ApiService;

public class NotificationHub : Hub
{
    // 固定のグループ名
    private const string FixedGroupName = "AllClients";

    // クライアント接続時に自動でグループに追加
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, FixedGroupName);
        await base.OnConnectedAsync();
    }

    // クライアントから呼ばれるメッセージ送信メソッド
    public async Task SendMessage(string user, string message)
    {
        // 固定グループ内の全クライアントにメッセージを送信
        await Clients.Group(FixedGroupName).SendAsync("ReceiveMessage", user, message);
    }
}

interface INotificationClient{
    Task ReceiveMessage(string request);
}

// public class NotificationMessage
// {
//     public string Message { get; set; }
// }
public class OrleansStreamBackgroundService : BackgroundService
    {
        private readonly IClusterClient _orleansClient;
        private readonly IHubContext<ChatHub> _hubContext;
        private Orleans.Streams.IAsyncStream<IEvent> _stream;
        private Orleans.Streams.StreamSubscriptionHandle<IEvent> _subscriptionHandle;

        public OrleansStreamBackgroundService(IClusterClient orleansClient, IHubContext<ChatHub> hubContext)
        {
            _orleansClient = orleansClient;
            _hubContext = hubContext;
        }

        public async Task OnNextAsync(IEvent item, StreamSequenceToken? token)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveMessage",item.GetPayload().GetType().Name, item.PartitionKeys.AggregateId);

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Orleansのストリームプロバイダ（例：SMSProvider）を取得
            var streamProvider = _orleansClient.GetStreamProvider("EventStreamProvider");

            // 固定のStreamId（ここではGuid.Empty）と名前空間（例："NotificationNamespace"）を指定してストリームを取得
            _stream = streamProvider.GetStream<IEvent>(StreamId.Create("AllEvents", Guid.Empty));

            // ストリームの購読開始
            _subscriptionHandle = await _stream.SubscribeAsync(OnNextAsync, async ex =>
            {
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", ex.GetType().Name,  ex.Message );
                await Task.CompletedTask;
            });
                
                // async (message, token) =>
                // {
                //     // 通知メッセージを受信したら、SignalRの全クライアントに送信
                //     await _hubContext.Clients.All.SendAsync("ReceiveNotification", message.Message);
                // },
                // ex =>
                // {
                //     Console.WriteLine($"Stream error: {ex.Message}");
                //     return Task.CompletedTask;
                // },
                // () =>
                // {
                //     Console.WriteLine("Stream completed");
                //     return Task.CompletedTask;
                // });

            // サービスがキャンセルされるまで待機
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_subscriptionHandle != null)
            {
                await _subscriptionHandle.UnsubscribeAsync();
            }
            await base.StopAsync(cancellationToken);
        }
    }