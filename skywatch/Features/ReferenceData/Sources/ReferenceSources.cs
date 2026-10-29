namespace SkyWatch.Features.ReferenceData.Sources;

public static class ReferenceSources
{
    public const string Airports = "airports";
    public const string Airlines = "airlines";
    public const string Countries = "countries";

    public static readonly string[] All = [Countries, Airports, Airlines];
}
