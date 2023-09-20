namespace ChatSystem.Logic.Models;

public record GenericResponse(string ErrorMessage);
public record GenericResponse<T>(T? Model = default, string? ErrorMessage = null);