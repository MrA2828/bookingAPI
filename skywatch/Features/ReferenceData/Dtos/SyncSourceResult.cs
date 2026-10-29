namespace SkyWatch.Features.ReferenceData.Dtos;

public record SyncSourceResult(string Source, bool Success, int Rows, int Added, int Updated, int Deactivated, string? Error);
