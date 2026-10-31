namespace SkyWatch.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthorizationTests(SkyWatchFactory api)
{
    [Theory]
    [InlineData("/airports?search=lhr")]
    [InlineData("/airports/LHR")]
    [InlineData("/airports/nearby?lat=51&lon=0")]
    [InlineData("/airlines?search=ba")]
    [InlineData("/countries")]
    [InlineData("/aircraft?lat=51&lon=0")]
    [InlineData("/aircraft/406f72")]
    public async Task Endpoints_reject_requests_without_a_token(string url) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Anonymous.GetAsync(url)).StatusCode);

    [Fact]
    public async Task Login_with_a_wrong_password_is_rejected()
    {
        var response = await api.Anonymous.PostAsJsonAsync("/auth/login", new { email = SkyWatchFactory.UserEmail, password = "wrong-Pass1!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_document_is_public() =>
        Assert.Equal(HttpStatusCode.OK, (await api.Anonymous.GetAsync("/openapi/v1.json")).StatusCode);
}
