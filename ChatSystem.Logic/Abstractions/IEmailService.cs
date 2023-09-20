using ChatSystem.Logic.Constants;

namespace ChatSystem.Logic.Abstractions;

public interface IEmailService
{
    Task FetchEmailAndSend<T>(EmailTemplateType templateType, T template, string recipient);
}