namespace SkyWatch.Entities;

// One row per imported source: when it last succeeded and what the last run changed.
public class SyncState
{
    public string Source { get; set; } = "";
    public string Status { get; set; } = "";        // "ok" | "failed"
    public DateTime? LastSyncedAt { get; set; }     // last successful run
    public DateTime? LastAttemptAt { get; set; }    // last run, successful or not
    public int Rows { get; set; }
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Deactivated { get; set; }
    public string? Error { get; set; }
}
