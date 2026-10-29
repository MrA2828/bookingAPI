namespace SkyWatch.Features.Aircraft.Sources;

public class AircraftFeedUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
