using System.Net;
using System.Text;
using System.Text.Json;
using ChatSystem.ApiWrapper.Models;

namespace ChatSystem.ApiWrapper;

public class ServerApiClient
{
    private readonly HttpClient _httpClient;

    public ServerApiClient()
    {
        var httpClientHandler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
        };

        _httpClient = new HttpClient(httpClientHandler)
        {
            BaseAddress = new Uri("https://localhost:44304"),
        };
    }
    
    public async Task<string> CreateServer(Guid userId, CreateServerRequest request)
    {
        var response = await _httpClient.PostAsync($"api/v1/server/create", SerializeRequest(request));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    // Method to delete a server
    public async Task DeleteServer(Guid userId, Guid serverId)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/server/{serverId}/delete");
        response.EnsureSuccessStatusCode();
    }

    // Method to view server logs
    public async Task<string> ViewLogs(Guid userId, Guid serverId, uint page = 1)
    {
        var response = await _httpClient.PostAsync($"api/v1/server/{serverId}/view-logs?page={page}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    // Method to create an invite
    public async Task<string> CreateInvite(Guid userId, Guid serverId)
    {
        var response = await _httpClient.PostAsync($"api/v1/server/{serverId}/create-invite", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    // Method to delete an invite
    public async Task DeleteInvite(Guid userId, Guid serverId, Guid inviteId)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/server/{inviteId}");
        response.EnsureSuccessStatusCode();
    }

    // Method to join a server using an invite
    public async Task JoinServer(Guid userId, string invite)
    {
        var response = await _httpClient.PostAsync($"api/v1/server/{invite}/join", null);
        response.EnsureSuccessStatusCode();
    }

    // Method to leave a server
    public async Task LeaveServer(Guid userId, Guid serverId)
    {
        var response = await _httpClient.PostAsync($"api/v1/server/{serverId}/leave", null);
        response.EnsureSuccessStatusCode();
    }

    // Method to create a role
    public async Task CreateRole(Guid userId, Guid serverId, CreateRoleRequest request)
    {
        var response = await _httpClient.PutAsync($"api/v1/server/{serverId}/create-role", SerializeRequest(request));
        response.EnsureSuccessStatusCode();
    }

    // Method to delete a role
    public async Task DeleteRole(Guid userId, Guid serverId, Guid targetRoleId)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/server/{serverId}/delete-role?targetRoleId={targetRoleId}");
        response.EnsureSuccessStatusCode();
    }

    // Method to kick a member
    public async Task KickMember(Guid userId, Guid serverId, Guid targetUserId)
    {
        var response = await _httpClient.PostAsync($"api/v1/server/{serverId}/kick-member?targetUserId={targetUserId}", null);
        response.EnsureSuccessStatusCode();
    }

    // Method to ban a member
    public async Task BanMember(Guid userId, Guid serverId, Guid targetUserId)
    {
        var response = await _httpClient.PostAsync($"api/v1/server/{serverId}/ban-member?targetUserId={targetUserId}", null);
        response.EnsureSuccessStatusCode();
    }

    // Helper method to serialize request objects to JSON
    private HttpContent SerializeRequest(object request)
    {
        var json = JsonSerializer.Serialize(request);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}