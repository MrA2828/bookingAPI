namespace SkyWatch.Entities;

// Reference rows imported from an external dataset. Rows that vanish from the source are
// flagged IsActive = false instead of being deleted, so history and references stay intact.
public interface ISyncedEntity
{
    int Id { get; }
    bool IsActive { get; set; }
    DateTime UpdatedAt { get; set; }
}
