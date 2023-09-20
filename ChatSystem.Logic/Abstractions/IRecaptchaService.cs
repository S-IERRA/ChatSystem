namespace ChatSystem.Logic.Abstractions;

public interface IRecaptchaService
{
    Task<bool> IsRecaptchaValid(string recaptchaResponse);
}