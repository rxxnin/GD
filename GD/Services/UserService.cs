using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GD.Services;

public class UserService
{
    private readonly HttpClient _client;

    public UserService(HttpClient httpClientFactory)
    {
        _client = httpClientFactory;
    }

    public async Task<Response> GetInfo(string jwt)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/info");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<Response>())!;
    }
}

public class Response
{
    public Guid Id { get; set; }
    public double Balance { get; set; }
    public double PosLati { get; set; }
    public double PosLong { get; set; }
}
