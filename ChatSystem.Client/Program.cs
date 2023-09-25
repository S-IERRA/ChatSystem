using System.Text.Json;
using ChatSystem.Client.Logic;
using ChatSystem.Logic.Models.Websocket;

var user2Object = new CreateAccountRequest("Account2", "email2@gmail.com", "string");
Console.WriteLine(JsonSerializer.Serialize(user2Object));

var webSocketClient = new WebSocketClient();

await webSocketClient.Send(WebSocketOpcodes.Login, user2Object);

Thread.Sleep(-1);

public record CreateAccountRequest(string Username, string Email, string Password);