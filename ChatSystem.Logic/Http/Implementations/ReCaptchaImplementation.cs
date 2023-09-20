using ChatSystem.Logic.Abstractions;
using ChatSystem.Logic.Constants.Configs;
using ChatSystem.Logic.Helpers;
using ChatSystem.Logic.Models;
using Microsoft.Extensions.Options;
using Serilog;

namespace ChatSystem.Logic.Http.Implementations;

public class ReCaptchaImplementation : IRecaptchaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeysConfig _keysConfig;
    private readonly ILogger _logger; 
    
    public ReCaptchaImplementation(IHttpClientFactory httpClientFactory, ILogger logger, IOptions<KeysConfig> keysConfig)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _keysConfig = keysConfig.Value;
    }

    public async Task<bool> IsRecaptchaValid(string recaptchaResponse)
    {
        using var httpClient = _httpClientFactory.CreateClient("ReCaptcha");

        string response = await httpClient.GetStringAsync($"recaptcha/api/siteverify?secret={_keysConfig.RecaptchaSecret}&response={recaptchaResponse}");
        JsonHelper.TryDeserialize<ReCaptchaResponse>(response, out var reCaptchaResponse);
        if(reCaptchaResponse is null)
            _logger.Error("ReCaptchaImplementation:IsRecaptchaValid Failed to deserialize {message} to <{class}>", recaptchaResponse, typeof(ReCaptchaResponse));
        
        return reCaptchaResponse is not null && reCaptchaResponse.Success;
    }
}