using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AspNetCore.ClaimsValueProvider;
using ChatSystem.Authorization.Abstractions;
using ChatSystem.Authorization.Models;
using ChatSystem.Data;
using ChatSystem.Data.Dtos;
using ChatSystem.Data.Models;
using ChatSystem.Logic.Abstractions;
using ChatSystem.Logic.ChatSystem_Logic;
using ChatSystem.Logic.ChatSystem_Logic.Algorithms;
using ChatSystem.Logic.Constants;
using ChatSystem.Logic.Helpers;
using ChatSystem.Logic.Http.Email.Models;
using ChatSystem.Logic.Models;
using ChatSystem.Logic.Models.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ILogger = Serilog.ILogger;

namespace ChatSystem.Rest.Controllers.V1;

[Authorize]
[ApiController]
[Route("api/v1/user")]
[EnableRateLimiting("Api")]
public class AccountController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IEmailService _emailService;
    private readonly IAuthenticatorService _authenticatorService;
    private readonly IDbContextFactory<EntityFrameworkContext> _dbContext;

    public AccountController(ILogger logger, IEmailService emailService, IAuthenticatorService authenticatorService, IMemoryCache memoryCache, IDbContextFactory<EntityFrameworkContext> dbContext)
    {
        _logger = logger;
        _emailService = emailService;
        _authenticatorService = authenticatorService;
        _memoryCache = memoryCache;
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> FetchUser(Guid userId)
    {
        //ToDo: if user is self (jwt) then return a less basic type
        await using var context = await _dbContext.CreateDbContextAsync();
            
            
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("test-create")]
    public async Task<IActionResult> CreateUserTest(CreateAccountRequest createAccountRequest)
    {
        await using var context = await _dbContext.CreateDbContextAsync();
        if (context.Users.Any(x => x.Email == createAccountRequest.Email))
        {
            _logger.Debug("AuthController:RequestRegisterUser - Email already exists: {email}", createAccountRequest.Email);

            return Ok(new GenericResponse(RestErrors.EmailAlreadyExists));
        }

        ChatUser chatUser = await context.CreateUserAccount(createAccountRequest);
        chatUser.RegistrationToken = Guid.Empty;
        
        await context.SaveChangesAsync();

        return Ok(JsonSerializer.Serialize((BasicChatUser)chatUser, JsonHelper.JsonSerializerOptions));
    }

    [AllowAnonymous]
    [HttpPost("test-method1")]
    public async Task<IActionResult> TestMethod1()
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        var user1Object = new CreateAccountRequest("Account1", "email1@gmail.com", "string");
        ChatUser chatUser1 = await context.CreateUserAccount(user1Object);
        chatUser1.RegistrationToken = Guid.Empty;
        
        var user2Object = new CreateAccountRequest("Account2", "email2@gmail.com", "string");
        ChatUser chatUser2 = await context.CreateUserAccount(user2Object);
        chatUser2.RegistrationToken = Guid.Empty;
        
        await context.SaveChangesAsync();

        await SendFriendRequest(chatUser1.Id, "Account2");
        await FriendRequest(chatUser1.Id, chatUser2.Id, true);

        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("create")]
    public async Task<IActionResult> CreateUser(CreateAccountRequest createAccountRequest)
    {
        /*bool isRecaptchaValid = await _recaptchaService.IsRecaptchaValid(recaptchaResponse);
        if (!isRecaptchaValid)
            return BadRequest(RestErrors.InvalidRecaptchaResponse);
        
        var validator = new RegisterUserValidator();
        ValidationResult result = await validator.ValidateAsync(authorizationRequest);
        
        if (!result.IsValid)
             return BadRequest(result.Errors);*/

        await using var context = await _dbContext.CreateDbContextAsync();
        if (context.Users.Any(x => x.Email == createAccountRequest.Email))
        {
            _logger.Debug("AuthController:RequestRegisterUser - Email already exists: {email}", createAccountRequest.Email);

            return Ok(new GenericResponse(RestErrors.EmailAlreadyExists));
        }

        ChatUser chatUser = await context.CreateUserAccount(createAccountRequest);

        var passwordResetTemplate = new RequestAccountCreation(createAccountRequest.Email, $"https://numix.cc/api/authorization/{chatUser.RegistrationToken}/confirm");
        await _emailService.FetchEmailAndSend(EmailTemplateType.SignUp, passwordResetTemplate, createAccountRequest.Email);

        return Ok();
    }
    
    [AllowAnonymous]
    [HttpPost("{registrationToken:guid}/confirm-create")]
    public async Task<IActionResult> ConfirmCreate(string recaptchaResponse, Guid registrationToken)
    {
        _logger.Debug("AuthController:RegisterUser called with recaptchaResponse: {recaptchaResponse}, registrationToken: {registrationToken}", recaptchaResponse, registrationToken);

        /*bool isRecaptchaValid = await _recaptchaService.IsRecaptchaValid(recaptchaResponse);
        if (!isRecaptchaValid)
            return BadRequest(new GenericResponse(NumixErrors.InvalidRecaptchaResponse));*/
        
        await using var context = await _dbContext.CreateDbContextAsync();
        if (await context.Users.FirstOrDefaultAsync(x => x.RegistrationToken == registrationToken) is not { } user)
        {
            _logger.Debug("AuthController:RegisterUser Failed - registrationToken not found: {registrationToken}", registrationToken);

            return NotFound(new GenericResponse(RestErrors.InvalidRegistrationToken));
        }

        user.RegistrationToken = Guid.Empty;
        await context.SaveChangesAsync();
        
        var passwordResetTemplate = new SuccessfullyCreatedAccountTemplate(user.Email);
        await _emailService.FetchEmailAndSend(EmailTemplateType.SuccessfullySignedUp, passwordResetTemplate, user.Email);

        return Ok(JsonSerializer.Serialize((BasicChatUser)user, JsonHelper.JsonSerializerOptions));
    }

    [AllowAnonymous]
    [HttpPost("login-user")]
    public async Task<IActionResult> LoginUser(CreateAccountRequest createAccountRequest)
    {
        _logger.Debug("AuthController:LoginUser called with authorizationRequest: {@authorizationRequest}", createAccountRequest);

        /*bool isRecaptchaValid = await _recaptchaService.IsRecaptchaValid(recaptchaResponse);
        if (!isRecaptchaValid)
            return BadRequest(new GenericResponse(NumixErrors.InvalidRecaptchaResponse));*/
        
        await using var context = await _dbContext.CreateDbContextAsync();
        GenericResponse<ChatUser> genericResponse = await context.LogUserIn(createAccountRequest);
        if (genericResponse.ErrorMessage is not null)
        {
            _logger.Debug("AuthController:LoginUser Failed: {@genericResponse}", genericResponse.ErrorMessage);

            return Unauthorized(new GenericResponse(genericResponse.ErrorMessage));
        }

        ChatUser chatUser = genericResponse.Model!;

        if (chatUser.RegistrationToken != Guid.Empty)
        {
            _logger.Debug("AuthController:LoginUser Failed, Account requires verification: {@Guid}", chatUser.Id);

            return Unauthorized(new GenericResponse(RestErrors.AccountNotConfirmed));
        }

        chatUser.SessionId = Guid.NewGuid();

        var jwtUser = (JwtUser)chatUser;
        jwtUser.IssuingIpAddress = HttpContext.Connection.RemoteIpAddress;
        
        ChatSystemAuthenticated jwtToken = _authenticatorService.GenerateToken(jwtUser);
        string jwtTokenJson = JsonSerializer.Serialize(jwtToken, JsonHelper.JsonSerializerOptions);
        
        var options = new CookieOptions
        {
            HttpOnly = true, 
        };
        
        Response.Cookies.Delete("JwtToken");
        Response.Cookies.Delete("RefreshToken");
    
        Response.Cookies.Append("JwtToken", jwtToken.JwtToken, options);
        
        if(jwtToken.RefreshToken is not null)
            Response.Cookies.Append("RefreshToken", jwtToken.RefreshToken, options);

        return Ok(jwtTokenJson);
    }

    [HttpPost("logout-user")]
    public async Task<IActionResult> LogoutUser([FromClaim(ChatClaims.SessionId)] Guid sessionId)
    {     
        string? refreshToken = Request.Cookies["RefreshToken"];

        if (refreshToken is null)
            return Unauthorized();
        
        _logger.Debug("AuthController:Logout called");

        await using var context = await _dbContext.CreateDbContextAsync();
        if (await context.Users.FirstOrDefaultAsync(x => x.SessionId == sessionId) is not { } currentUser)
            return Unauthorized();

        currentUser.SessionId = null;
        await context.SaveChangesAsync();

        Response.Cookies.Delete("JwtToken");
        Response.Cookies.Delete("RefreshToken");

        _memoryCache.Set(Request.Cookies["JwtToken"]!, true, TimeSpan.FromMinutes(15));

        return Ok();
    }

    [HttpPost("resume-session")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ResumeSession([FromClaim(ChatClaims.UserId)] Guid userId)
    {
        string? refreshToken = Request.Cookies["RefreshToken"];

        if (refreshToken is null || userId == Guid.Empty)
            return Unauthorized();

        _logger.Debug("AuthController:ResumeSession called with userId: {userId}", userId);

        await using var context = await _dbContext.CreateDbContextAsync();
        if (await context.Users.FirstOrDefaultAsync(x => x.Id == userId) is not { } chatUser)
            return BadRequest();
        
        var jwtUser = (JwtUser)chatUser;
        jwtUser.IssuingIpAddress = HttpContext.Connection.RemoteIpAddress;
        
        ChatSystemAuthenticated jwtToken = _authenticatorService.GenerateToken(jwtUser);
        string jwtTokenJson = JsonSerializer.Serialize(jwtToken, JsonHelper.JsonSerializerOptions);
        
        var options = new CookieOptions
        {
            HttpOnly = true,
        };
    
        Response.Cookies.Delete("JwtToken");
        Response.Cookies.Delete("RefreshToken");

        _memoryCache.Set(Request.Cookies["JwtToken"]!, true, TimeSpan.FromMinutes(15));
        Response.Cookies.Append("JwtToken", jwtToken.JwtToken, options);
        
        if(jwtToken.RefreshToken is not null)
            Response.Cookies.Append("RefreshToken", jwtToken.RefreshToken, options);
        
        return Ok(JsonSerializer.Serialize(jwtToken, JsonHelper.JsonSerializerOptions));
    }

    [AllowAnonymous]
    [HttpPost("request-reset-password")]
    public async Task<IActionResult> RequestPasswordReset([EmailAddress] string email)
    {
        _logger.Debug("AuthController:RequestPasswordReset called with email: {@email}", email);

        await using var context = await _dbContext.CreateDbContextAsync();
        if (await context.Users.FirstOrDefaultAsync(x => x.Email == email) is not { } chatUser)
            return Ok(new GenericResponse(RestErrors.InvalidEmail));
        
        if (chatUser.RegistrationToken != Guid.Empty)
        {
            _logger.Debug("AuthController:LoginUser Failed, Account requires verification: {@Guid}", chatUser.Id);

            return Unauthorized(new GenericResponse(RestErrors.AccountNotConfirmed));
        }

        chatUser.PasswordResetToken = Guid.NewGuid();
        await context.SaveChangesAsync();

        var passwordResetTemplate = new PasswordResetTemplate($"https://numix.cc/api/authorization/{chatUser.PasswordResetToken}/set-password");
        await _emailService.FetchEmailAndSend(EmailTemplateType.PasswordReset, passwordResetTemplate, email);
        
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("{passwordResetToken:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid passwordResetToken, string password)
    {
        _logger.Debug("AuthController:SetPassword called with passwordResetToken: {passwordResetToken}",
            passwordResetToken);

        await using var context = await _dbContext.CreateDbContextAsync();
        if (await context.Users.FirstOrDefaultAsync(x =>
                x.PasswordResetToken != Guid.Empty && x.PasswordResetToken == passwordResetToken) is not { } chatUser)
            return NotFound(new GenericResponse(RestErrors.InvalidResetToken));

        chatUser.HashedPassword = Pbkdf2.CreateHash(password);
        chatUser.PasswordResetToken = Guid.Empty;

        await context.SaveChangesAsync();

        var passwordResetTemplate = new SuccessfullyResetTemplate(chatUser.Email);
        await _emailService.FetchEmailAndSend(EmailTemplateType.SuccessfullyReset, passwordResetTemplate,
            chatUser.Email);

        return Ok();
    }

    //ToDo: fetch from cache
    [HttpGet("relationships")]
    public async Task<IActionResult> FetchRelationships([FromClaim(ChatClaims.UserId)] Guid userId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Users.Where(x => x.Id == userId).Include(x=>x.Relationships).ThenInclude(x=>x.Users).FirstOrDefaultAsync() is not { } user)
            return NotFound();

        return Ok(user);
    }

    //ToDo: is user already has a relationship of any sort , decline the request
    [HttpPost("{targetUsername}/send-friend-request")]
    public async Task<IActionResult> SendFriendRequest([FromClaim(ChatClaims.UserId)] Guid userId, string targetUsername)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Users.FirstOrDefaultAsync(x => x.Id == userId) is not { } user)
            return NotFound();

        if (await context.Users.Where(x => x.Username == targetUsername)
                .Include(x=>x.Relationships)
                .ThenInclude(x=>x.Users)
                .FirstOrDefaultAsync() is not { } targetUser)
            return NotFound();

        var hasRelationShip = targetUser.Relationships.FirstOrDefault(r => r.Users.Any(x=>x.Id == userId));
        if (hasRelationShip is not null)
            return BadRequest();
        
        context.Relationships.Add(new ChatRelationship()
            {
                CreatorId = userId,
                Type = ChatRelationShipType.Outgoing,
                Users = new List<ChatUser>()
                {
                    user, targetUser
                }
            }
        );
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{requestId:guid}/cancel-request")]
    public async Task<IActionResult> CancelFriendRequest([FromClaim(ChatClaims.UserId)] Guid userId, Guid requestId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Relationships.FirstOrDefaultAsync(x => x.Id == requestId) is not { } relationship)
            return NotFound();

        if (relationship.CreatorId != userId)
            return BadRequest();

        context.Relationships.Remove(relationship);
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    [HttpPost("{requestId:guid}/reply-friend-request")]
    public async Task<IActionResult> FriendRequest([FromClaim(ChatClaims.UserId)] Guid userId, Guid requestId, bool isAccepted)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Relationships.FirstOrDefaultAsync(x => x.Id == requestId) is not { } relationship)
            return NotFound();

        if (relationship.CreatorId == userId)
            return BadRequest();

        if (isAccepted)
            relationship.Type = ChatRelationShipType.Friends;
        else
            context.Relationships.Remove(relationship);
        
        await context.SaveChangesAsync();
        
        return Ok();
    }

    [HttpDelete("{targetUserId:guid}/unfriend")]
    public async Task<IActionResult> Unfriend([FromClaim(ChatClaims.UserId)] Guid userId, Guid targetUserId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        var currentUser = await context.Users
            .Include(u => u.Relationships)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (currentUser is null)
            return NotFound("User not found");

        var targetUser = await context.Users
            .Include(u => u.Relationships)
            .FirstOrDefaultAsync(u => u.Id == targetUserId);

        if (targetUser is null)
            return NotFound("Target user not found");

        var friendship = currentUser.Relationships.FirstOrDefault(r =>
            r.Type is ChatRelationShipType.Friends && r.Users.Any(x=>x.Id == targetUserId));

        if (friendship is null)
            return BadRequest("You are not friends with the target user");

        context.Relationships.Remove(friendship);
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    //ToDo: if user is friends, remove that friendship
    [HttpPost("{targetUserId:guid}/block")]
    public async Task<IActionResult> BlockUser([FromClaim(ChatClaims.UserId)] Guid userId, Guid targetUserId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Users.FirstOrDefaultAsync(x => x.Id == userId) is not { } currentUser)
            return NotFound();

        if (await context.Users.FirstOrDefaultAsync(x => x.Id == targetUserId) is not { } targetUser)
            return NotFound();
        
        var friendship = currentUser.Relationships.FirstOrDefault(r => r.Type is ChatRelationShipType.Friends && r.Users.Any(x=>x.Id == targetUserId));
        if (friendship is not null)
            context.Relationships.Remove(friendship);

        context.Relationships.Add(new ChatRelationship()
            {
                CreatorId = userId,
                Type = ChatRelationShipType.Blocked,
                Users = new List<ChatUser>()
                {
                    currentUser, targetUser
                }
            }
        );
        await context.SaveChangesAsync();

        return Ok();
    }
    
    [HttpPost("{targetUserId:guid}/unblock")]
    public async Task<IActionResult> UnblockUser([FromClaim(ChatClaims.UserId)] Guid userId, Guid targetUserId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Users.Where(x => x.Id == userId)
                .Include(x => x.Relationships)
                .ThenInclude(x=>x.Users)
                .FirstOrDefaultAsync() is not { } 
                currentUser)
            return NotFound();

        var friendship = currentUser.Relationships.FirstOrDefault(r => r.Type is ChatRelationShipType.Blocked && r.Users.Any(x=>x.Id == targetUserId));
        if (friendship is null)
            return BadRequest("You are not blocking the target user");

        context.Relationships.Remove(friendship);
        await context.SaveChangesAsync();
        
        return Ok();
    }
}