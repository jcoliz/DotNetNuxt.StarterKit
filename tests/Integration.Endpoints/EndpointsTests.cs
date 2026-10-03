using System.Net;
using System.Text.Json;
using DotNetNuxt.Tests.Integration.Endpoints.Helpers;

namespace DotNetNuxt.Tests.Integration.Endpoints;

[TestFixture]
[NonParallelizable]
public class EndpointsTests : EndpointTestFixture
{
    [Test]
    public async Task Version_ReturnsOk()
    {
        var response = await Client.GetAsync("/version");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.Not.Empty);
    }

    [Test]
    public async Task Weather_ReturnsForecasts()
    {
        var response = await Client.GetAsync("/api/weather?offset=0&count=5");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetArrayLength(), Is.EqualTo(5));
    }

    [Test]
    public async Task Weather_OffsetOutOfRange_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/api/weather?offset=31");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("/api/weather?offset=31", Description = "model validation")]
    [TestCase("/api/weather?offset=0&count=60", Description = "feature validation")]
    public async Task Weather_BadRequest_NamesTheOperation(string url)
    {
        var response = await Client.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("detail").GetString(), Is.EqualTo("Failed to fetch weather forecasts"));
    }

}
