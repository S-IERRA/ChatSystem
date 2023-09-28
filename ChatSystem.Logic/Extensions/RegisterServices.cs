
using ChatSystem.Logic.Abstractions;
using ChatSystem.Logic.Caching.Rest;
using ChatSystem.Logic.Constants.Configs;
using ChatSystem.Logic.Email.Implementations;
using ChatSystem.Logic.Http.Implementations;
using ChatSystem.Logic.Websocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ChatSystem.Logic.Extensions;

public static class RegisterServices
{
    public static void RegisterLogicServices(this IServiceCollection serviceCollection, IConfiguration configuration)
    { 
        serviceCollection.Configure<SmtpSettings>(configuration.GetSection("MailSettings"));
        serviceCollection.Configure<KeysConfig>(configuration.GetSection("Keys"));
        
        serviceCollection.AddSingleton(typeof(IRedisCommunicationService), typeof(RedisCommunicationImplementation));
        serviceCollection.AddSingleton(typeof(IGenericCacheService), typeof(GenericCacheImplementation));

        /*using var serviceProvider = serviceCollection.BuildServiceProvider(); 
        serviceProvider.GetRequiredService<IRedisCommunicationService>();*/

        //ToDo: below won't be needed
        serviceCollection.AddScoped(typeof(IRecaptchaService), typeof(ReCaptchaImplementation));
        serviceCollection.AddScoped(typeof(IEmailService), typeof(EmailServiceImplementation));
    }

    
    public static void RegisterHttpClients(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient("ReCaptcha", client => 
        {
            client.BaseAddress = new Uri("https://www.google.com");
        });
    }
}

