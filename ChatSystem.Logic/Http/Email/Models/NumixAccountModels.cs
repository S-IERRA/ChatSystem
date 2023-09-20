namespace ChatSystem.Logic.Http.Email.Models;

public record RequestAccountCreation(string Username, string RestUrl);
public record SuccessfullyCreatedAccountTemplate(string Username);

public record PasswordResetTemplate(string ResetCode);
public record SuccessfullyResetTemplate(string Username);