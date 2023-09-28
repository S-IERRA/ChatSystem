using System.Text.Json.Serialization;
using AspNetCore.ClaimsValueProvider;
using ChatSystem.Authorization.Extensions;
using ChatSystem.Data;
using ChatSystem.Data.Extensions;
using ChatSystem.Logic.Extensions;
using ChatSystem.Rest.Extensions;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .MinimumLevel.Warning()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);

builder.Services.AddMemoryCache();

builder.Services.RegisterRateLimits();
builder.Services.RegisterHttpClients();
builder.Services.RegisterDataServices();
builder.Services.RegisterAuthorization(builder.Configuration);
builder.Services.RegisterLogicServices(builder.Configuration);

builder.Services.AddSwaggerGen();

builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMvc(options =>
{
    options.AddClaimsValueProvider();
});

WebApplication app = builder.Build();
app.UseCors("CORSPolicy");
app.UseRouting();
app.MapControllers();

//app.UseHttpsRedirection();
app.UseAuthorization();

/*using (var serviceScope = app.Services.CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<EntityFrameworkContext>();
    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
}*/

// app.RegisterAuthorizationMiddlewares();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.SerializeAsV2 = true;
    });
    
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat System");
    });
}

app.Run();