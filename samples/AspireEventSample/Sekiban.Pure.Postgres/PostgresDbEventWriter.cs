using ResultBoxes;
using Sekiban.Pure.Events;
using Sekiban.Pure.OrleansEventSourcing;
using Sekiban.Pure.Serialize;

namespace Sekiban.Pure.Postgres;

public class PostgresDbEventWriter : IEventWriter
{
    private readonly PostgresDbFactory _dbFactory;
    private readonly IEventTypes _eventTypes;
    private readonly ISekibanSerializer _serializer;

    public PostgresDbEventWriter(PostgresDbFactory dbFactory, IEventTypes eventTypes, ISekibanSerializer serializer)
    {
        _dbFactory = dbFactory;
        _eventTypes = eventTypes;
        _serializer = serializer;
    }

    public async Task SaveEvents<TEvent>(IEnumerable<TEvent> events) where TEvent : IEvent
    {
        await _dbFactory.DbActionAsync(async dbContext =>
        {
            var dbEvents = events
                .Select(ev => DbEvent.FromEvent(ev, _serializer, _eventTypes))
                .ToList();
            
            await dbContext.Events.AddRangeAsync(dbEvents);
            await dbContext.SaveChangesAsync();
        });
    }
}
