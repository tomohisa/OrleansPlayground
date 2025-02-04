using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using ResultBoxes;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Exception;
using Sekiban.Pure.OrleansEventSourcing;

namespace Sekiban.Pure.CosmosDb;

public class Class1
{
}

public class CosmosDbEventWriter(CosmosDbFactory dbFactory) : IEventWriter
{
    
    public Task SaveEvents<TEvent>(IEnumerable<TEvent> events) where TEvent : IEvent=> dbFactory.CosmosActionAsync(
        DocumentType.Event,
        async container =>
        {
            var taskList = events
                // .Select(ev => ev.)
                .Select(ev => container.UpsertItemAsync<dynamic>(ev, CosmosPartitionGenerator.ForEvent(ev.PartitionKeys)))
                .ToList();
            await Task.WhenAll(taskList);
        });
}

public class CosmosPartitionGenerator
{
    public static PartitionKey ForEvent(PartitionKeys partitionKeys) =>
        new PartitionKeyBuilder()
            .Add(partitionKeys.RootPartitionKey)
            .Add(partitionKeys.Group)
            .Add(PartitionKeyGenerator.ForEvent(partitionKeys))
            .Build();
}




/// <summary>
///     Use this to access memory cache instance.
///     In should share same instance over threads.
/// </summary>
public interface ICosmosMemoryCacheAccessor
{
    /// <summary>
    ///     Get shared memory cache instance.
    /// </summary>
    IMemoryCache Cache { get; }
}
/// <summary>
///     Memory cache accessor
///     Note: This class is for internal use only
/// </summary>
public class CosmosMemoryCacheAccessor : ICosmosMemoryCacheAccessor
{
    private static IMemoryCache? staticMemoryCache;
    public CosmosMemoryCacheAccessor(IMemoryCache memoryCache) => Cache = staticMemoryCache ??= memoryCache;
    public IMemoryCache Cache { get; }
}


public class SekibanCosmosSerializer() : CosmosSerializer
{
    private readonly JsonObjectSerializer _jsonObjectSerializer = new(new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNameCaseInsensitive = true });
// can use source generator serialization.
    public override T FromStream<T>(Stream stream)
    {
        if (typeof(Stream).IsAssignableFrom(typeof(T)))
        {
            return (T)(object)stream;
        }

        using (stream)
        {
            return (T)_jsonObjectSerializer.Deserialize(stream, typeof(T), default)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var streamPayload = new MemoryStream();
        _jsonObjectSerializer.Serialize(streamPayload, input, typeof(T), default);
        streamPayload.Position = 0;
        return streamPayload;
    }
}

public record SekibanCosmosClientOptions
{
    /// <summary>
    ///     Cosmos Db Options.
    /// </summary>
    public CosmosClientOptions ClientOptions { get; init; } = new()
    {
        Serializer = new SekibanCosmosSerializer(),
        AllowBulkExecution = true,
        MaxRetryAttemptsOnRateLimitedRequests = 200,
        ConnectionMode = ConnectionMode.Gateway,
        GatewayModeMaxConnectionLimit = 200
    };
}


public class CosmosDbFactory(
    ICosmosMemoryCacheAccessor cosmosMemoryCache,
    SekibanCosmosClientOptions options, SekibanAzureCosmosDbOption sekibanAzureCosmosDbOptions)
{
    public Func<Task<CosmosClient?>> SearchCosmosClientAsync { get; set; } = async () =>
    {
        await Task.CompletedTask;
        return null;
    };

    public async Task DeleteAllFromEventContainer()
    {
        await DeleteAllFromAggregateFromContainerIncludes(DocumentType.Event);
    }

    public async Task<T> CosmosActionAsync<T>(
        DocumentType documentType,Func<Container, Task<T>> cosmosAction)
    {
        try
        {
            var result = await cosmosAction(await GetContainerAsync(documentType));
            return result;
        }
        catch
        {
            ResetMemoryCache(documentType);
            throw;
        }
    }

    public async Task CosmosActionAsync(DocumentType documentType, Func<Container, Task> cosmosAction)
    {
        try
        {
            await cosmosAction(await GetContainerAsync(documentType));
        }
        catch
        {
            // There may be a network error, so initialize the container.
            // This allows reconnection when recovered next time.
            ResetMemoryCache(documentType);
            throw;
        }
    }
    public string GetContainerId(DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.Event => sekibanAzureCosmosDbOptions.CosmosEventsContainer ?? string.Empty,
            _ => sekibanAzureCosmosDbOptions.CosmosItemsContainer ?? string.Empty
        };
    }
    private static string GetMemoryCacheContainerKey(
        DocumentType documentType,
        string databaseId,
        string containerId) =>
        $"{(documentType == DocumentType.Event ? "event." : "")}cosmosdb.container.{databaseId}.{containerId}";

    private static string GetMemoryCacheClientKey(DocumentType documentType) =>
        $"{(documentType == DocumentType.Event ? "event." : "")}cosmosdb.client";

    private static string GetMemoryCacheDatabaseKey(DocumentType documentType, string databaseId) =>
        $"{(documentType == DocumentType.Event ? "event." : "")}cosmosdb.container.{databaseId}";

    private string GetUri()
    {
        return sekibanAzureCosmosDbOptions.CosmosEndPointUrl ?? string.Empty;
    }

    private string GetSecurityKey()
    {
        return sekibanAzureCosmosDbOptions.CosmosAuthorizationKey ?? string.Empty;
    }

    private ResultBox<string> GetConnectionString() =>
        ResultBox<SekibanAzureCosmosDbOption>.FromValue(sekibanAzureCosmosDbOptions)
            .Conveyor(
                azureOptions => azureOptions.CosmosConnectionString switch
                {
                    { } v when !string.IsNullOrWhiteSpace(v) => ResultBox<string>.FromValue(v),
                    _ => new SekibanConfigurationException("CosmosConnectionString is not set.")
                });
    public string GetDatabaseId()
    {
        return sekibanAzureCosmosDbOptions.CosmosDatabase ?? string.Empty;
    }

    public Container? GetContainerFromCache(DocumentType documentType)
    {
        var databaseId = GetDatabaseId();
        var containerId = GetContainerId(documentType);
        return (Container?)cosmosMemoryCache.Cache.Get(GetMemoryCacheContainerKey(documentType, databaseId, containerId));
    }
    public void SetContainerToCache(DocumentType documentType, Container container)
    {
        var databaseId = GetDatabaseId();
        var containerId = GetContainerId(documentType);
        cosmosMemoryCache.Cache.Set(
            GetMemoryCacheContainerKey(documentType, databaseId, containerId),
            container,
            new MemoryCacheEntryOptions());
    }
    public async Task<Database> GetDatabaseAsync(DocumentType documentType, CosmosClient client)
    {
        var database = cosmosMemoryCache.Cache.Get<Database?>(GetMemoryCacheDatabaseKey(documentType, GetDatabaseId()));
        if (database is not null)
        {
            return database;
        }
        database = await client.CreateDatabaseIfNotExistsAsync(GetDatabaseId());
        cosmosMemoryCache.Cache.Set(
            GetMemoryCacheDatabaseKey(documentType, GetDatabaseId()),
            database,
            new MemoryCacheEntryOptions());
        return database;
    }
    public async Task<Container> GetContainerFromDatabaseAsync(DocumentType documentType, Database database)
    {
        var containerId = GetContainerId(documentType);
        var containerProperties = new ContainerProperties(containerId, GetPartitionKeyPaths());
        var container = await database.CreateContainerIfNotExistsAsync(containerProperties, 400);

        SetContainerToCache(documentType, container);
        return container;
    }

    public async Task<CosmosClient> GetCosmosClientAsync(DocumentType documentType)
    {
        await Task.CompletedTask;
        var client = cosmosMemoryCache.Cache.Get<CosmosClient?>(GetMemoryCacheClientKey(documentType));
        if (client is not null)
        {
            return client;
        }
        var clientOptions = options.ClientOptions;
        client = await SearchCosmosClientAsync() ??
            GetConnectionString() switch
            {
                { IsSuccess: true } value => new CosmosClient(value.GetValue(), clientOptions),
                _ => GetCosmosClientFromUriAndKey()
            };
        cosmosMemoryCache.Cache.Set(GetMemoryCacheClientKey(documentType), client, new MemoryCacheEntryOptions());
        return client;
    }
    private CosmosClient GetCosmosClientFromUriAndKey()
    {
        var uri = GetUri();
        var securityKey = GetSecurityKey();
        var clientOptions = options.ClientOptions;
        return new CosmosClient(uri, securityKey, clientOptions);
    }

    public async Task<Container> GetContainerAsync(DocumentType documentType)
    {
        var container = GetContainerFromCache(documentType);
        if (container is not null)
        {
            return container;
        }
        var client = await GetCosmosClientAsync(documentType);

        var database = await GetDatabaseAsync(documentType, client);
        return await GetContainerFromDatabaseAsync(documentType, database);
    }

    public async Task DeleteAllFromAggregateFromContainerIncludes(DocumentType documentType)
    {
        await CosmosActionAsync<IEnumerable<IEvent>?>(
            documentType,
            async container =>
            {
                var query = container.GetItemLinqQueryable<IDocument>().Where(b => true);
                var feedIterator = container.GetItemQueryIterator<CosmosEventInfo>(query.ToQueryDefinition());

                var deleteItemIds = new List<(Guid id, PartitionKey partitionKey)>();
                while (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        if (item is null)
                        {
                            continue;
                        }
                        var id = item.Id;
                        var partitionKey = item.PartitionKey;
                        var rootPartitionKey = item.RootPartitionKey;
                        var aggregateType = item.AggregateType;

                        deleteItemIds.Add((id, new PartitionKeyBuilder().Add(rootPartitionKey).Add(aggregateType).Add(partitionKey).Build()));
                    }
                }

                var concurrencyTasks = new List<Task>();
                foreach (var (id, partitionKey) in deleteItemIds)
                {
                    concurrencyTasks.Add(container.DeleteItemAsync<IDocument>(id.ToString(), partitionKey));
                }

                await Task.WhenAll(concurrencyTasks);
                return null;
            });
    }

    public void ResetMemoryCache(DocumentType documentType)
    {
        var containerId = GetContainerId(documentType);
        var databaseId = GetDatabaseId();
        // There may be a network error, so initialize the container.
        // This allows reconnection when recovered next time.
        cosmosMemoryCache.Cache.Remove(GetMemoryCacheClientKey(documentType));
        cosmosMemoryCache.Cache.Remove(GetMemoryCacheDatabaseKey(documentType, databaseId));
        cosmosMemoryCache.Cache.Remove(GetMemoryCacheContainerKey(documentType, databaseId, containerId));
    }

    private static IReadOnlyList<string> GetPartitionKeyPaths() => ["/RootPartitionKey", "/AggregateType", "/PartitionKey"];
}

public record SekibanAzureCosmosDbOption
{
    public const string CosmosEventsContainerDefaultValue = "events";
    public const string CosmosItemsContainerDefaultValue = "items";
    public const string CosmosConnectionStringNameDefaultValue = "SekibanCosmos";
    public const string CosmosDatabaseDefaultValue = "SekibanDb";
    public const bool LegacyPartitionDefaultValue = false;

    public string CosmosEventsContainer { get; init; } = CosmosEventsContainerDefaultValue;
    public string CosmosItemsContainer { get; init; } = CosmosItemsContainerDefaultValue;
    public string? CosmosEndPointUrl { get; init; }
    public string? CosmosAuthorizationKey { get; init; }
    public string CosmosConnectionStringName { get; init; } = CosmosConnectionStringNameDefaultValue;
    public string? CosmosConnectionString { get; init; }
    public string CosmosDatabase { get; init; } = CosmosDatabaseDefaultValue;

    public bool LegacyPartitions { get; init; }

    public static SekibanAzureCosmosDbOption FromConfiguration(
        IConfigurationSection section,
        IConfigurationRoot configurationRoot)
    {
        var azureSection = section.GetSection("Azure");

        var eventsContainer = azureSection.GetValue<string>(nameof(CosmosEventsContainer)) ??
            azureSection.GetValue<string>("CosmosDbEventsContainer") ??
            azureSection.GetValue<string>("AggregateEventCosmosDbContainer") ?? CosmosEventsContainerDefaultValue;
        var itemsContainer = azureSection.GetValue<string>(nameof(CosmosItemsContainer)) ??
            azureSection.GetValue<string>("CosmosDbItemsContainer") ??
            azureSection.GetValue<string>("CosmosDbContainer") ??
            azureSection.GetValue<string>("CosmosDbCommandsContainer") ??
            azureSection.GetValue<string>("AggregateCommandCosmosDbContainer") ?? CosmosItemsContainerDefaultValue;

        var cosmosEndPointUrl = azureSection.GetValue<string>(nameof(CosmosEndPointUrl)) ?? azureSection.GetValue<string>("CosmosDbEndPointUrl");
        var cosmosAuthorizationKey = azureSection.GetValue<string>(nameof(CosmosAuthorizationKey)) ??
            azureSection.GetValue<string>("CosmosDbAuthorizationKey");
        var cosmosConnectionStringName = azureSection.GetValue<string>(nameof(CosmosConnectionStringName)) ?? CosmosConnectionStringNameDefaultValue;
        var cosmosConnectionString = configurationRoot.GetConnectionString(cosmosConnectionStringName) ??
            section.GetValue<string>(nameof(CosmosConnectionString)) ?? section.GetValue<string>("CosmosDbConnectionString");
        var cosmosDatabase = azureSection.GetValue<string>(nameof(CosmosDatabase)) ??
            azureSection.GetValue<string>("CosmosDbDatabase") ?? CosmosDatabaseDefaultValue;


        var legacyPartition = azureSection.GetValue<bool?>(nameof(LegacyPartitions)) ?? LegacyPartitionDefaultValue;

        return new SekibanAzureCosmosDbOption
        {
            CosmosEventsContainer = eventsContainer,
            CosmosItemsContainer = itemsContainer,
            CosmosConnectionString = cosmosConnectionString,
            CosmosConnectionStringName = cosmosConnectionStringName,
            CosmosEndPointUrl = cosmosEndPointUrl,
            CosmosAuthorizationKey = cosmosAuthorizationKey,
            CosmosDatabase = cosmosDatabase,
            LegacyPartitions = legacyPartition
        };
    }
}

public record CosmosEventInfo
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }
    public string PartitionKey { get; init; } = string.Empty;
    public string RootPartitionKey { get; init; } = string.Empty;
    public string AggregateType { get; init; } = string.Empty;
}


