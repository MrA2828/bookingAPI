namespace SkyWatch.Features.ReferenceData.Dtos;

public record SyncStateReturnDto(string Source, string Status, DateTime? LastSyncedAt, DateTime? LastAttemptAt,
    int Rows, int Added, int Updated, int Deactivated, string? Error);
