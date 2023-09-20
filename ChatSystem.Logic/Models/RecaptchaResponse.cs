using System.Text.Json.Serialization;

namespace ChatSystem.Logic.Models;

public record ReCaptchaResponse(
    [property: JsonPropertyName("success")] bool Success
);
