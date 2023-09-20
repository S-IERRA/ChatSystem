using System.Net;
using System.Text;
using System.Text.Json;
using ChatSystem.ApiWrapper.Helpers;
using ChatSystem.ApiWrapper.Models;
using ChatSystem.ApiWrapper.Models.Response;

namespace ChatSystem.ApiWrapper;

public class AccountApiClient
{
    private readonly HttpClient _httpClient;

    public AccountApiClient()
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
    
    public async Task<HttpResponseMessage> FetchUser(Guid userId)
    {
        var response = await _httpClient.GetAsync($"api/v1/user/{userId}");
        return response;
    }

    public async Task<BasicChatUser?> CreateUserTest(CreateAccountRequest createAccountRequest)
    {
        var content = new StringContent(JsonSerializer.Serialize(createAccountRequest), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/v1/user/test-create", content);

        string responseContent = await response.Content.ReadAsStringAsync();
        JsonHelper.TryDeserialize<BasicChatUser>(responseContent, out var deserialized);
        
        return deserialized;
    }

    public async Task<HttpResponseMessage> CreateUser(CreateAccountRequest createAccountRequest)
    {
        var content = new StringContent(JsonSerializer.Serialize(createAccountRequest), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/v1/user/create", content);
        return response;
    }
    
    public async Task<BasicChatUser?> ConfirmCreate(Guid registrationToken)
    {
        var response = await _httpClient.PostAsync($"api/v1/user/{registrationToken}/confirm-create", null);
        
        string responseContent = await response.Content.ReadAsStringAsync();
        JsonHelper.TryDeserialize<BasicChatUser>(responseContent, out var deserialized);
        
        return deserialized;
    }

    public async Task<HttpResponseMessage> LoginUser(CreateAccountRequest createAccountRequest)
    {
        var content = new StringContent(JsonSerializer.Serialize(createAccountRequest), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/v1/user/login-user", content);
        return response;
    }

    public async Task<HttpResponseMessage> LogoutUser()
    {
        var response = await _httpClient.PostAsync("api/v1/user/logout-user", null);
        return response;
    }

 public async Task<HttpResponseMessage> ResumeSession(Guid userId)
    {
        var response = await _httpClient.PostAsync($"api/v1/user/resume-session?userId={userId}", null);
        return response;
    }

    public async Task<HttpResponseMessage> RequestPasswordReset(string email)
    {
        var requestUri = $"api/v1/user/request-reset-password?email={Uri.EscapeDataString(email)}";
        var response = await _httpClient.PostAsync(requestUri, null);
        return response;
    }

    public async Task<HttpResponseMessage> ResetPassword(Guid passwordResetToken, string password)
    {
        var requestUri = $"api/v1/user/{passwordResetToken}/reset-password";
        var content = new StringContent(JsonSerializer.Serialize(new { password }), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(requestUri, content);
        return response;
    }

    public async Task<HttpResponseMessage> FetchRelationships(Guid userId)
    {
        var response = await _httpClient.GetAsync($"api/v1/user/relationships?userId={userId}");
        return response;
    }

    public async Task<HttpResponseMessage> SendFriendRequest(Guid userId, string targetUsername)
    {
        var requestUri = $"api/v1/user/{targetUsername}/send-friend-request";
        var response = await _httpClient.PostAsync(requestUri, null);
        return response;
    }

    public async Task<HttpResponseMessage> CancelFriendRequest(Guid requestId)
    {
        var requestUri = $"api/v1/user/{requestId}/cancel-request";
        var response = await _httpClient.DeleteAsync(requestUri);
        return response;
    }

    public async Task<HttpResponseMessage> FriendRequest(Guid requestId, bool isAccepted)
    {
        var requestUri = $"api/v1/user/{requestId}/reply-friend-request?isAccepted={isAccepted}";
        var response = await _httpClient.PostAsync(requestUri, null);
        return response;
    }

    public async Task<HttpResponseMessage> Unfriend(Guid targetUserId)
    {
        var requestUri = $"api/v1/user/{targetUserId}/unfriend";
        var response = await _httpClient.PostAsync(requestUri, null);
        return response;
    }

    public async Task<HttpResponseMessage> BlockUser(Guid targetUser)
    {
        var requestUri = $"api/v1/user/{targetUser}/block";
        var response = await _httpClient.PostAsync(requestUri, null);
        return response;
    }

    public async Task<HttpResponseMessage> UnblockUser(Guid targetUserId)
    {
        var requestUri = $"api/v1/user/{targetUserId}/unblock";
        var response = await _httpClient.PostAsync(requestUri, null);
        return response;
    }
}