using ChatSystem.Data.Extensions;
using ChatSystem.Websocket.Logic;
using Microsoft.Extensions.DependencyInjection;

IServiceCollection serviceCollection = new ServiceCollection();
serviceCollection.RegisterDataServices();
serviceCollection.AddSingleton<WebSocketServer>();

using var serviceProvider = serviceCollection.BuildServiceProvider();
serviceProvider.GetRequiredService<WebSocketServer>();

Thread.Sleep(-1);