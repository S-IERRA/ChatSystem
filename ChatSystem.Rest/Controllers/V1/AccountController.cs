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
}