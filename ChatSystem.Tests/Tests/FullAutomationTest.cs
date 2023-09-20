using ChatSystem.ApiWrapper;
using ChatSystem.ApiWrapper.Models;

namespace ChatSystem.Tests.Tests;

public class FullAutomationTest
{
    private static readonly AccountApiClient AccountApiClient = new AccountApiClient();

    public async Task RunTest()
    {
        var createAccountRequest = new CreateAccountRequest("S-IERRA", "test-email1@email.com", "Password01!");
        await AccountApiClient.CreateUserTest(createAccountRequest);
        
        await AccountApiClient.LoginUser(createAccountRequest);
    }
}