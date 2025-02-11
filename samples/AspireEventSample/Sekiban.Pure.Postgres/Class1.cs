using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ResultBoxes;
using Sekiban.Pure.Events;
using Sekiban.Pure.Serialize;

namespace Sekiban.Pure.Postgres;

public class Class1
{
}

public class PostgresDbFactory(
    SekibanPostgresDbOption dbOption,
    IPostgresMemoryCacheAccessor memoryCache,
    IServiceProvider serviceProvider)
{
    private static string GetMemoryCacheDbContextKey()
    {
        return "dbContext.Postgres";
    }

    private string GetConnectionString()
    {
        return dbOption.ConnectionString ?? string.Empty;
    }

    private bool GetMigrationFinished()
    {
        return dbOption.MigrationFinished;
    }

    private void SetMigrationFinished()
    {
        dbOption.MigrationFinished = true;
    }

    private async Task<SekibanDbContext> GetDbContextAsync()
    {
        // var dbContextFromCache = (SekibanDbContext?)memoryCache.Cache.Get(GetMemoryCacheDbContextKey(SekibanContextIdentifier()));
        //
        // if (dbContextFromCache is not null)
        // {
        //     return dbContextFromCache;
        // }
        //
        var connectionString = GetConnectionString();
        var dbContext = new SekibanDbContext(new DbContextOptions<SekibanDbContext>())
            { ConnectionString = connectionString };
        if (!GetMigrationFinished())
        {
            await dbContext.Database.MigrateAsync();
            SetMigrationFinished();
        }

        // memoryCache.Cache.Set(GetMemoryCacheDbContextKey(SekibanContextIdentifier()), dbContext, new MemoryCacheEntryOptions());
        await Task.CompletedTask;
        return dbContext;
    }

    public async Task DeleteAllFromAggregateFromContainerIncludes()
    {
        await DbActionAsync(
            async dbContext =>
            {
                dbContext.Events.RemoveRange(dbContext.Events);
                await dbContext.SaveChangesAsync();
            });
    }

    public async Task DeleteAllFromEventContainer()
    {
        await DeleteAllFromAggregateFromContainerIncludes();
    }

    private void ResetMemoryCache()
    {
        // There may be a network error, so initialize the container.
        // This allows reconnection when recovered next time.
        memoryCache.Cache.Remove(GetMemoryCacheDbContextKey());
    }

    public async Task<T> DbActionAsync<T>(Func<SekibanDbContext, Task<T>> dbAction)
    {
        try
        {
            await using var dbContext = await GetDbContextAsync();
            var result = await dbAction(dbContext);
            return result;
        }
        catch
        {
            ResetMemoryCache();
            throw;
        }
    }

    public async Task DbActionAsync(Func<SekibanDbContext, Task> dbAction)
    {
        try
        {
            await using var dbContext = await GetDbContextAsync();
            await dbAction(dbContext);
        }
        catch
        {
            // There may be a network error, so initialize the container.
            // This allows reconnection when recovered next time.
            ResetMemoryCache();
            throw;
        }
    }
}

public class SekibanDbContext(DbContextOptions<SekibanDbContext> options) : DbContext(options)
{
    public DbSet<DbEvent> Events { get; set; } = default!;
    public string ConnectionString { get; init; } = string.Empty;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(ConnectionString);
    }
}

public interface IDbEvent
{
    public Guid Id { get; init; }
    public string Payload { get; init; }
    public int Version { get; init; }
    public string CallHistories { get; init; }
    public Guid AggregateId { get; init; }
    public string PartitionKey { get; init; }
    public string DocumentTypeName { get; init; }
    public DateTime TimeStamp { get; init; }
    public string SortableUniqueId { get; init; }
    public string AggregateType { get; init; }
    public string RootPartitionKey { get; init; }
}

public record DbEvent : IDbEvent
{
    public string AggregateGroup { get; init; } = string.Empty;
    public string PayloadTypeName { get; init; } = string.Empty;
    public string CausationId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public string ExecutedUser { get; init; } = string.Empty;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; init; }

    [Column(TypeName = "json")] public string Payload { get; init; } = string.Empty;
    public string SortableUniqueId { get; init; } = string.Empty;

    public int Version { get; init; }
    public Guid AggregateId { get; init; }
    public string RootPartitionKey { get; init; } = string.Empty;
    public DateTime TimeStamp { get; init; } = DateTime.MinValue;
    public string PartitionKey { get; init; } = string.Empty;

    public static DbEvent FromEvent(IEvent ev, ISekibanSerializer serializer, IEventTypes eventTypes)
    {
        var document = eventTypes.ConvertToEventDocument(ev).UnwrapBox();

        return new DbEvent
        {
            Version = document.Version,
            Payload = serializer.Serialize(document.Payload) // need to serialize by type.
            CallHistories = SekibanJsonHelper.Serialize(ev.CallHistories) ?? string.Empty,
            Id = ev.Id,
            AggregateId = ev.PartitionKeys.AggregateId,
            PartitionKey = ev.PartitionKeys.ToPrimaryKeysString(),
            DocumentTypeName = ev.GetPayload().GetType().Name,
            TimeStamp = ev,
            SortableUniqueId = ev.SortableUniqueId,
            AggregateType = ev.AggregateType,
            RootPartitionKey = ev.RootPartitionKey
        };
    }
}