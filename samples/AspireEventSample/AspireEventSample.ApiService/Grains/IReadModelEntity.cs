namespace Sekiban.Pure.Documents
{
    public interface IReadModelEntity
    {
        Guid Id { get; }
        Guid TargetId { get; }
        string LastSortableUniqueId { get; }
        DateTime TimeStamp { get; }
    }
}
