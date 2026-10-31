namespace SkyWatch.IntegrationTests;

// One host and one database shared by every test class in the collection.
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<SkyWatchFactory>
{
    public const string Name = "api";
}
