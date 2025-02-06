using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;

namespace Sekiban.Pure.OrleansEventSourcing;

public interface IEventWriter
{
    Task SaveEvents<TEvent>(IEnumerable<TEvent> events) where TEvent : IEvent;
}

public interface IAggregatesStream
{
    public List<string> GetStreamNames();
    public ResultBox<string> GetSingleStreamName() =>
        ResultBox
            .UnitValue
            .Verify(
                () => GetStreamNames() is { Count : 1 } streamNames
                    ? ExceptionOrNone.None
                    : new ApplicationException("Stream Names is not set"))
            .Conveyor(_ => GetStreamNames()[0].ToResultBox());
}

public record SortableIdConditionNone : ISortableIdCondition
{
    public bool OutsideOfRange(SortableUniqueIdValue toCompare) => false;
    public static SortableIdConditionNone None => new();
}
public record SinceSortableIdCondition(SortableUniqueIdValue SortableUniqueId) : ISortableIdCondition
{
    public bool OutsideOfRange(SortableUniqueIdValue toCompare) => SortableUniqueId.IsLaterThan(toCompare);
}
public record BetweenSortableIdCondition(SortableUniqueIdValue Start, SortableUniqueIdValue End) : ISortableIdCondition
{
    public bool OutsideOfRange(SortableUniqueIdValue toCompare) =>
        Start.IsLaterThan(toCompare) || End.IsEarlierThan(toCompare);
}

public interface ISortableIdCondition
{
    public static ISortableIdCondition None => new SortableIdConditionNone();
    public bool OutsideOfRange(SortableUniqueIdValue toCompare);
    public static ISortableIdCondition Since(SortableUniqueIdValue sinceSortableId) =>
        new SinceSortableIdCondition(sinceSortableId);
    public static ISortableIdCondition Between(SortableUniqueIdValue start, SortableUniqueIdValue end) =>
        start.IsEarlierThan(end)
            ? new BetweenSortableIdCondition(start, end)
            : new BetweenSortableIdCondition(end, start);
    public static ISortableIdCondition FromState(IAggregate? state) =>
        state?.LastSortableUniqueId is { } lastSortableId ? Since(lastSortableId) : None;
}
public record AggregateGroupStream(
    string AggregateGroup) : IAggregatesStream
{
    public List<string> GetStreamNames() => [AggregateGroup];
}


public record EventRetrievalInfo(
    OptionalValue<string> RootPartitionKey,
    OptionalValue<IAggregatesStream> AggregateStream,
    OptionalValue<Guid> AggregateId,
    ISortableIdCondition SortableIdCondition)
{
    public OptionalValue<int> MaxCount { get; init; } = OptionalValue<int>.Empty;

    public static EventRetrievalInfo FromNullableValues(
        string? rootPartitionKey,
        IAggregatesStream aggregatesStream,
        Guid? aggregateId,
        ISortableIdCondition sortableIdCondition,
        int? MaxCount = null) => new(
        string.IsNullOrWhiteSpace(rootPartitionKey)
            ? (aggregateId.HasValue ? OptionalValue.FromValue(IDocument.DefaultRootPartitionKey) : OptionalValue<string>.Empty) 
            : OptionalValue.FromNullableValue(rootPartitionKey),
        OptionalValue<IAggregatesStream>.FromValue(aggregatesStream),
        OptionalValue.FromNullableValue(aggregateId),
        sortableIdCondition)
    {
        MaxCount = OptionalValue.FromNullableValue(MaxCount)
    };
    public static EventRetrievalInfo All => new(
        OptionalValue<string>.Empty,
        OptionalValue<IAggregatesStream>.Empty,
        OptionalValue<Guid>.Empty,
        SortableIdConditionNone.None);

    public bool GetIsPartition() => AggregateId.HasValue;
    public bool HasAggregateStream() =>
        AggregateStream.HasValue && AggregateStream.GetValue().GetStreamNames().Count > 0;
    public bool HasRootPartitionKey() => RootPartitionKey.HasValue;

    public ResultBox<string> GetPartitionKey() =>
        ResultBox
            .UnitValue
            .Verify(
                () => GetIsPartition() ? ExceptionOrNone.None : new ApplicationException("Partition Key is not set"))
            .Verify(
                () => HasAggregateStream()
                    ? ExceptionOrNone.None
                    : new ApplicationException("Aggregate Stream is not set"))
            .Conveyor(() => AggregateStream.GetValue().GetSingleStreamName())
            .Verify(
                () => HasRootPartitionKey()
                    ? ExceptionOrNone.None
                    : new ApplicationException("Root Partition Key is not set"))
            .Remap(
                aggregateName => PartitionKeys.Existing(AggregateId.GetValue(),aggregateName, RootPartitionKey.GetValue()).ToPrimaryKeysString());
    public static EventRetrievalInfo FromPartitionKeys(PartitionKeys partitionKeys) =>
        new(
            OptionalValue.FromValue(partitionKeys.RootPartitionKey),
            OptionalValue<IAggregatesStream>.FromValue(new AggregateGroupStream(partitionKeys.Group)),
            OptionalValue.FromValue(partitionKeys.AggregateId),
            SortableIdConditionNone.None);
}

public interface IEventReader
{
    Task<ResultBox<IReadOnlyList<IEvent>>> GetEvents(EventRetrievalInfo eventRetrievalInfo);
}