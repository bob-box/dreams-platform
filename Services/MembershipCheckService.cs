using System.Text.Json;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Services;

public class MembershipCheckService(IHttpClientFactory factory) : IMembershipCheckService
{
    private const string ApiUrl = "https://api.example.com/membership"; // Replace with actual API URL

    public async Task<bool> IsActiveMember(string email)
    {
        return true;
        var httpClient = factory.CreateClient();
        var response = await httpClient.GetAsync($"{ApiUrl}?email={Uri.EscapeDataString(email)}");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MembershipResponse>(content);
            return result?.IsMember ?? false;
        }
        return false;
    }

    private class MembershipResponse
    {
        public bool IsMember { get; set; }
    }
}
