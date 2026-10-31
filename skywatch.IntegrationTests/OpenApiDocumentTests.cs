namespace SkyWatch.IntegrationTests;

// Swagger UI renders and validates the form from this document, so its shape matters.
[Collection(ApiCollection.Name)]
public class OpenApiDocumentTests(SkyWatchFactory api)
{
    private async Task<JsonElement> ParameterSchema(string path, string name)
    {
        var document = await api.Anonymous.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var parameters = document.GetProperty("paths").GetProperty(path).GetProperty("get").GetProperty("parameters");
        return parameters.EnumerateArray().First(p => p.GetProperty("name").GetString() == name).GetProperty("schema");
    }

    [Theory]
    [InlineData("/aircraft", "lat")]
    [InlineData("/aircraft", "lon")]
    [InlineData("/aircraft", "radiusKm")]
    [InlineData("/airports/nearby", "lat")]
    public async Task Numbers_are_documented_as_plain_numbers_not_number_or_string(string path, string name)
    {
        var type = (await ParameterSchema(path, name)).GetProperty("type");
        Assert.Equal(JsonValueKind.String, type.ValueKind); // an array here means "number | string"
        Assert.Equal("number", type.GetString());
    }

    // Swagger UI only offers a pick-list when the enum is inline in the array's items, not behind a $ref.
    [Fact]
    public async Task Kinds_is_an_array_whose_items_list_the_named_values_inline()
    {
        var kinds = await ParameterSchema("/aircraft", "kinds");
        Assert.Equal("array", kinds.GetProperty("type").GetString());

        var items = kinds.GetProperty("items");
        Assert.False(items.TryGetProperty("$ref", out _));
        Assert.Equal("string", items.GetProperty("type").GetString());
        Assert.Equal(["commercial", "private", "military", "unknown"], items.GetProperty("enum").EnumerateArray().Select(v => v.GetString()));
    }
}
